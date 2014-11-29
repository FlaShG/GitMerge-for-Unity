using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class GitMergeWindow : EditorWindow
{
    private static string git = @"C:\Program Files (x86)\Git\bin\git.exe";
    private static List<GitMergeActions> allMergeActions;

    private static string sceneName;
    private static string theirSceneName;

    private Vector2 scrollPosition = Vector2.zero;


    [MenuItem("Window/GitMerge")]
    static void OpenEditor()
    {
        var window = EditorWindow.GetWindow(typeof(GitMergeWindow), false, "GitMerge");
        //In case we're merging and the scene becomes edited,
        //the shown SerializedProperties should be repainted
        window.autoRepaintOnSceneChange = true;
        window.minSize = new Vector2(500, 100);
    }

    void OnHierarchyChange()
    {
        //Repaint if we changed the scene
        this.Repaint();
    }

    void Update()
    {
        if(GitMergeAction.inMergePhase
        &&(EditorApplication.isCompiling
        || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            ShowNotification(new GUIContent("Aborting merge due to editor state change."));
            AbortMerge();
        }
    }

    void OnGUI()
    {
        GitMergeResources.DrawLogo();

        GUILayout.Label("Open Scene: " + EditorApplication.currentScene);
        if(EditorApplication.currentScene != ""
           && allMergeActions == null
           && GUILayout.Button("Start merging this scene", GUILayout.Height(80)))
        {
            InitializeMerging();
        }


        if(allMergeActions != null)
        {
            var done = false;
            if(allMergeActions != null)
            {
                done = true;
                done = DisplayMergeActions(done);
            }
            GUILayout.BeginHorizontal();
            if(done && GUILayout.Button("Apply merge"))
            {
                CompleteMerge();
            }
            if(GUILayout.Button("Abort"))
            {
                AbortMerge();
            }
            GUILayout.EndHorizontal();
        }
    }

    private bool DisplayMergeActions(bool done)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
        GUILayout.BeginVertical(GUILayout.MinWidth(480));

        var textColor = GUI.skin.label.normal.textColor;
        GUI.skin.label.normal.textColor = Color.black;

        foreach(var actions in allMergeActions)
        {
            actions.OnGUI();
            done = done && actions.merged;
        }

        GUI.skin.label.normal.textColor = textColor;

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        return done;
    }

    private void InitializeMerging()
    {
        GitMergeAction.inMergePhase = false;

        GitMergeOriginalObjects.Clear();

        GetTheirVersionOf(EditorApplication.currentScene);
        AssetDatabase.Refresh();

        var ourObjects = GetAllSceneObjects();
        SetAsOriginalObjects(ourObjects);
        EditorApplication.OpenSceneAdditive(theirSceneName);
        AssetDatabase.DeleteAsset(theirSceneName);
        var addedObjects = GetAllNewSceneObjects(ourObjects);
        SetAsMergeObjects(addedObjects);

        BuildAllMergeActions(ourObjects, addedObjects);

        if(allMergeActions.Count == 0)
        {
            allMergeActions = null;
        }
        else
        {
            GitMergeAction.inMergePhase = true;
        }
    }

    private static List<GameObject> GetAllSceneObjects()
    {
        return new List<GameObject>((GameObject[])FindObjectsOfType(typeof(GameObject)));
    }

    private static List<GameObject> GetAllNewSceneObjects(List<GameObject> oldObjects)
    {
        var all = GetAllSceneObjects();
        var old = oldObjects;

        foreach(var obj in old)
        {
            all.Remove(obj);
        }

        return all;
    }

    private void SetAsOriginalObjects(List<GameObject> objects)
    {
        foreach(var obj in objects)
        {
            obj.SetAsOriginalObject();
        }
    }

    private void SetAsMergeObjects(List<GameObject> objects)
    {
        foreach(var obj in objects)
        {
            obj.SetAsMergeObject(false);
        }
    }

    private static void GetTheirVersionOf(string path)
    {
        sceneName = path;

        string basepath = Path.GetDirectoryName(path);
        string sname = Path.GetFileNameWithoutExtension(path);

        string ours = Path.Combine(basepath, sname + "--OURS.unity");
        theirSceneName = Path.Combine(basepath, sname + "--THEIRS.unity");

        File.Copy(path, ours);
        ExecuteGit("checkout --theirs " + path);
        File.Move(path, theirSceneName);
        File.Move(ours, path);
    }

    private void BuildAllMergeActions(List<GameObject> ourObjects, List<GameObject> theirObjects)
    {
        allMergeActions = new List<GitMergeActions>();

        var theirObjectsDict = new Dictionary<int, GameObject>();
        foreach(var theirs in theirObjects)
        {
            theirObjectsDict.Add(ObjectIDFinder.GetIdentifierFor(theirs), theirs);
        }

        foreach(var ours in ourObjects)
        {
            var id = ObjectIDFinder.GetIdentifierFor(ours);
            GameObject theirs;
            theirObjectsDict.TryGetValue(id, out theirs);

            var mergeActions = new GitMergeActions(ours, theirs);
            if(mergeActions.hasActions)
            {
                allMergeActions.Add(mergeActions);
            }
            theirObjectsDict.Remove(id);
        }

        foreach(var theirs in theirObjectsDict.Values)
        {
            //new GameObjects from them
            var mergeActions = new GitMergeActions(null, theirs);
            if(mergeActions.hasActions)
            {
                allMergeActions.Add(mergeActions);
            }
        }
    }

    private void CompleteMerge()
    {
        GitMergeAction.inMergePhase = false;

        GitMergeGameObjectExtensions.DestroyAllMergeObjects();
        GitMergeOriginalObjects.Clear();
        EditorApplication.SaveScene();

        allMergeActions = null;

        //Mark as merged for git
        ExecuteGit("add " + sceneName);

        //directly committing here might not be that smart, since there might be more conflicts

        ShowNotification(new GUIContent("Scene successfully merged."));
    }

    private static void AbortMerge()
    {
        GitMergeAction.inMergePhase = false;

        foreach(var actions in allMergeActions)
        {
            actions.UseOurs();
        }
        GitMergeGameObjectExtensions.DestroyAllMergeObjects();
        allMergeActions = null;
    }

    private static string ExecuteGit(string args)
    {
        var process = new Process();
        var startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = git;
        startInfo.Arguments = args;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        process.StartInfo = startInfo;

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }

    private static void print(string msg)
    {
        UnityEngine.Debug.Log(msg);
    }
}
