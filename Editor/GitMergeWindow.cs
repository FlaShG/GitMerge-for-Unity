using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace GitMerge
{
    /// <summary>
    /// The window that lets you perform merges on scenes and prefabs.
    /// </summary>
    public class GitMergeWindow : EditorWindow
    {
        private static string git = @"C:\Program Files (x86)\Git\bin\git.exe";
        private static List<GameObjectMergeActions> allMergeActions;
        private static bool mergeInProgress
        {
            get
            {
                return allMergeActions != null;
            }
        }

        private static string sceneName;
        private static string theirSceneName;

        private Vector2 scrollPosition = Vector2.zero;
        private int mode = 0;


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

        //Always check for editor state changes, and abort the active merge process if needed
        void Update()
        {
            if(MergeAction.inMergePhase
            && (EditorApplication.isCompiling
            || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                ShowNotification(new GUIContent("Aborting merge due to editor state change."));
                AbortMerge();
            }
        }

        void OnGUI()
        {
            Resources.DrawLogo();
            DrawModeButtons();

            switch(mode)
            {
                case 0:
                    OnGUISceneTab();
                    break;

                case 1:
                    OnGUIPrefabTab();
                    break;

                default:
                    OnGUISettingsTab();
                    break;
            }
        }

        private void OnGUISceneTab()
        {
            GUILayout.Label("Open Scene: " + EditorApplication.currentScene);
            if(EditorApplication.currentScene != ""
               && allMergeActions == null
               && GUILayout.Button("Start merging this scene", GUILayout.Height(80)))
            {
                InitializeMerging();
            }

            DisplayMergeProcess();
        }

        private void OnGUIPrefabTab()
        {

        }

        private void OnGUISettingsTab()
        {

        }

        private void DisplayMergeProcess()
        {
            if(mergeInProgress)
            {
                var done = false;
                if(allMergeActions != null)
                {
                    done = DisplayMergeActions();
                }
                GUILayout.BeginHorizontal();
                if(done && GUILayout.Button("Apply merge"))
                {
                    CompleteMerge();
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawModeButtons()
        {
            if(!mergeInProgress)
            {
                string[] modes = { "Merge Scene", "Merge Prefab", "Settings" };
                mode = GUI.SelectionGrid(new Rect(72, 36, 300, 22), mode, modes, 3);
            }
            else
            {
                GUI.backgroundColor = new Color(1,0.4f,0.4f,1);
                if(GUI.Button(new Rect(72, 36, 300, 22), "Abort merge"))
                {
                    AbortMerge();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        /// <summary>
        /// Displays all GameObjectMergeActions.
        /// </summary>
        /// <returns>True, if all MergeActions are flagged as "merged".</returns>
        private bool DisplayMergeActions()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            GUILayout.BeginVertical(GUILayout.MinWidth(480));

            var textColor = GUI.skin.label.normal.textColor;
            GUI.skin.label.normal.textColor = Color.black;

            var done = true;
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
            MergeAction.inMergePhase = false;

            ObjectDictionaries.Clear();

            //checkout "their" version
            GetTheirVersionOf(EditorApplication.currentScene);
            AssetDatabase.Refresh();

            //find all of "our" objects
            var ourObjects = GetAllSceneObjects();
            SetAsOurObjects(ourObjects);

            //add "their" objects
            EditorApplication.OpenSceneAdditive(theirSceneName);

            //delete scene file
            AssetDatabase.DeleteAsset(theirSceneName);

            //find all of "their" objects
            var addedObjects = GetAllNewSceneObjects(ourObjects);
            SetAsTheirObjects(addedObjects);

            //create list of differences that have to be merged
            BuildAllMergeActions(ourObjects, addedObjects);

            if(allMergeActions.Count == 0)
            {
                allMergeActions = null;
            }
            else
            {
                MergeAction.inMergePhase = true;
            }
        }

        private static List<GameObject> GetAllSceneObjects()
        {
            return new List<GameObject>((GameObject[])FindObjectsOfType(typeof(GameObject)));
        }

        /// <summary>
        /// Finds all GameObjects in the scene, minus the ones passed.
        /// </summary>
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

        private void SetAsOurObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                ObjectDictionaries.SetAsOurObject(obj);
            }
        }

        private void SetAsTheirObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                ObjectDictionaries.SetAsTheirs(obj, false);
            }
        }

        /// <summary>
        /// Creates "their" version of the scene at the given path,
        /// named scenename--THEIRS.unity.
        /// </summary>
        /// <param name="path">The path of the scene, relative to the project folder.</param>
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

        /// <summary>
        /// Finds all specific merge conflicts between two sets of GameObjects,
        /// representing "our" scene and "their" scene.
        /// </summary>
        /// <param name="ourObjects">The GameObjects of "our" version of the scene.</param>
        /// <param name="theirObjects">The GameObjects of "their" version of the scene.</param>
        private void BuildAllMergeActions(List<GameObject> ourObjects, List<GameObject> theirObjects)
        {
            allMergeActions = new List<GameObjectMergeActions>();

            //Map "their" GameObjects to their respective ids
            var theirObjectsDict = new Dictionary<int, GameObject>();
            foreach(var theirs in theirObjects)
            {
                theirObjectsDict.Add(ObjectIDFinder.GetIdentifierFor(theirs), theirs);
            }

            foreach(var ours in ourObjects)
            {
                //Try to find "their" equivalent to "our" GameObjects
                var id = ObjectIDFinder.GetIdentifierFor(ours);
                GameObject theirs;
                theirObjectsDict.TryGetValue(id, out theirs);

                //If theirs is null, mergeActions.hasActions will be false
                var mergeActions = new GameObjectMergeActions(ours, theirs);
                if(mergeActions.hasActions)
                {
                    allMergeActions.Add(mergeActions);
                }
                //Remove "their" GameObject from the dict to only keep those new to us
                theirObjectsDict.Remove(id);
            }

            //Every GameObject left in the dict is a...
            foreach(var theirs in theirObjectsDict.Values)
            {
                //...new GameObject from them
                var mergeActions = new GameObjectMergeActions(null, theirs);
                if(mergeActions.hasActions)
                {
                    allMergeActions.Add(mergeActions);
                }
            }
        }

        /// <summary>
        /// Completes the merge process after solving all conflicts.
        /// Cleans up the scene by deleting "their" GameObjects, clears merge related data structures,
        /// executes git add scene_name.
        /// </summary>
        private void CompleteMerge()
        {
            MergeAction.inMergePhase = false;

            ObjectDictionaries.DestroyTheirObjects();
            ObjectDictionaries.Clear();
            EditorApplication.SaveScene();

            allMergeActions = null;

            //Mark as merged for git
            ExecuteGit("add " + sceneName);

            //directly committing here might not be that smart, since there might be more conflicts

            ShowNotification(new GUIContent("Scene successfully merged."));
        }

        /// <summary>
        /// Aborts merge by using "our" version in all conflicts.
        /// Cleans up merge related data.
        /// </summary>
        private void AbortMerge()
        {
            MergeAction.inMergePhase = false;

            foreach(var actions in allMergeActions)
            {
                actions.UseOurs();
            }
            ObjectDictionaries.DestroyTheirObjects();
            ObjectDictionaries.Clear();
            allMergeActions = null;

            ShowNotification(new GUIContent("Merge aborted."));
        }

        /// <summary>
        /// Executes git as a subprocess.
        /// </summary>
        /// <param name="args">The git parameters. Examples: "status", "add filename".</param>
        /// <returns>Whatever git returns.</returns>
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
    }
}