using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitMerge
{
    /// <summary>
    /// The window that lets you perform merges on scenes and prefabs.
    /// </summary>
    public class GitMergeWindow : EditorWindow
    {
        //EditorPrefs keys for settings
        private const string epGitpath = "GitMerge_git";
        private const string epAutomerge = "GitMerge_automerge";
        private const string epAutofocus = "GitMerge_autofocus";
        
        //Settings
        private static string git = @"C:\Program Files (x86)\Git\bin\git.exe";
        public static bool automerge { private set; get; }
        public static bool autofocus { private set; get; }

        private static List<GameObjectMergeActions> allMergeActions;
        private static bool mergeInProgress
        {
            get
            {
                return allMergeActions != null;
            }
        }

        private static string fileName;
        private static string theirFilename;

        //Stuff needed for prefab merging
        public static GameObject ourPrefab { private set; get; }
        private static GameObject theirPrefab;
        private static string previouslyOpenedScene;
        public static GameObject ourPrefabInstance { private set; get; }

        //Are we merging a scene?
        //If we're merging and this yields false, we're merging a prefab.
        public static bool isMergingScene
        {
            get { return ourPrefab == null; }
        }

        private Vector2 scrollPosition = Vector2.zero;
        private int tab = 0;


        [MenuItem("Window/GitMerge")]
        static void OpenEditor()
        {
            var window = EditorWindow.GetWindow(typeof(GitMergeWindow), false, "GitMerge");
            //In case we're merging and the scene becomes edited,
            //the shown SerializedProperties should be repainted
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(500, 100);
        }

        void OnEnable()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            if(EditorPrefs.HasKey(epGitpath))
            {
                git = EditorPrefs.GetString(epGitpath);
            }
            if(EditorPrefs.HasKey(epAutomerge))
            {
                automerge = EditorPrefs.GetBool(epAutomerge);
            }
            else
            {
                automerge = true;
            }
            if(EditorPrefs.HasKey(epAutofocus))
            {
                autofocus = EditorPrefs.GetBool(epAutofocus);
            }
            else
            {
                autofocus = true;
            }
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
            DrawTabButtons();

            switch(tab)
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

        /// <summary>
        /// Tab that offers scene merging.
        /// </summary>
        private void OnGUISceneTab()
        {
            GUILayout.Label("Open Scene: " + EditorApplication.currentScene);
            if(EditorApplication.currentScene != ""
               && !mergeInProgress
               && GUILayout.Button("Start merging this scene", GUILayout.Height(80)))
            {
                InitializeSceneMerging();
            }

            DisplayMergeProcess();
        }

        /// <summary>
        /// Tab that offers prefab merging.
        /// </summary>
        private void OnGUIPrefabTab()
        {
            GameObject prefab;
            if(!mergeInProgress)
            {
                GUILayout.Label("Drag your prefab here to start merging:");
                if(prefab = EditorGUILayout.ObjectField(null, typeof(GameObject), false, GUILayout.Height(60)) as GameObject)
                {
                    InitializePrefabMerging(prefab);
                }
            }

            DisplayMergeProcess();
        }

        /// <summary>
        /// Tab that offers various settings for the tool.
        /// </summary>
        private void OnGUISettingsTab()
        {
            var gitNew = EditorGUILayout.TextField("Path to git.exe", git);
            if(git != gitNew)
            {
                git = gitNew;
                EditorPrefs.SetString(epGitpath, git);
            }

            var amNew = EditorGUILayout.Toggle("Automerge", automerge);
            if(automerge != amNew)
            {
                automerge = amNew;
                EditorPrefs.SetBool(epAutomerge, automerge);
            }
            GUILayout.Label("(Automerge new/deleted GameObjects/Components upon merge start)");

            var afNew = EditorGUILayout.Toggle("Auto Highlight", autofocus);
            if(autofocus != afNew)
            {
                autofocus = afNew;
                EditorPrefs.SetBool(epAutofocus, autofocus);
            }
            GUILayout.Label("(Highlight GameObjects when applying a MergeAction to it)");
        }

        /// <summary>
        /// Displays all MergeActions and the "apply merge" button if a merge is in progress.
        /// </summary>
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
                    if(isMergingScene)
                    {
                        CompleteSceneMerge();
                    }
                    else
                    {
                        CompletePrefabMerge();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// If no merge is in progress, draws the buttons to switch between tabs.
        /// Otherwise, draws the "abort merge" button.
        /// </summary>
        private void DrawTabButtons()
        {
            if(!mergeInProgress)
            {
                string[] tabs = { "Merge Scene", "Merge Prefab", "Settings" };
                tab = GUI.SelectionGrid(new Rect(72, 36, 300, 22), tab, tabs, 3);
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

        private void InitializeSceneMerging()
        {
            MergeAction.inMergePhase = false;

            //Do this just in case there is still a reference.
            //Otherwise isMergingScene will be false if something goes wrong.
            ourPrefab = null;

            ObjectDictionaries.Clear();

            //checkout "their" version
            GetTheirVersionOf(EditorApplication.currentScene);
            AssetDatabase.Refresh();

            //find all of "our" objects
            var ourObjects = GetAllSceneObjects();
            SetAsOurObjects(ourObjects);

            //add "their" objects
            EditorApplication.OpenSceneAdditive(theirFilename);

            //delete scene file
            AssetDatabase.DeleteAsset(theirFilename);

            //find all of "their" objects
            var addedObjects = GetAllNewSceneObjects(ourObjects);
            SetAsTheirObjects(addedObjects);

            //create list of differences that have to be merged
            BuildAllMergeActions(ourObjects, addedObjects);

            if(allMergeActions.Count == 0)
            {
                allMergeActions = null;
                ShowNotification(new GUIContent("No conflict found for this scene."));
            }
            else
            {
                MergeAction.inMergePhase = true;
            }
        }

        private void InitializePrefabMerging(GameObject prefab)
        {
            if(!EditorApplication.SaveCurrentSceneIfUserWantsTo())
            {
                return;
            }

            MergeAction.inMergePhase = false;

            ObjectDictionaries.Clear();

            //checkout "their" version
            GetTheirVersionOf(AssetDatabase.GetAssetOrScenePath(prefab));
            AssetDatabase.Refresh();
            
            ourPrefab = prefab;

            //Open a new Scene that will only display the prefab
            previouslyOpenedScene = EditorApplication.currentScene;
            EditorApplication.NewScene();

            //make the new scene empty
            DestroyImmediate(Camera.main.gameObject);

            //instantiate our object in order to view it while merging
            ourPrefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            //find all of "our" objects in the prefab
            var ourObjects = GetAllObjects(prefab);

            theirPrefab = AssetDatabase.LoadAssetAtPath(theirFilename, typeof(GameObject)) as GameObject;
            theirPrefab.hideFlags = HideFlags.HideAndDontSave;
            var theirObjects = GetAllObjects(theirPrefab);

            //create list of differences that have to be merged
            BuildAllMergeActions(ourObjects, theirObjects);

            if(allMergeActions.Count == 0)
            {
                allMergeActions = null;
                ourPrefab = null;
                AssetDatabase.DeleteAsset(theirFilename);
                OpenPreviousScene();
                ShowNotification(new GUIContent("No conflict found for this prefab."));
            }
            else
            {
                MergeAction.inMergePhase = true;
                ourPrefabInstance.Highlight();
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

        /// <summary>
        /// Recursively find all GameObjects that are part of the prefab
        /// </summary>
        /// <param name="prefab">The prefab to analyze</param>
        /// <param name="list">The list with all the objects already found. Pass null in the beginning.</param>
        /// <returns>The list with all the objects</returns>
        private static List<GameObject> GetAllObjects(GameObject prefab, List<GameObject> list = null)
        {
            if(list == null)
            {
                list = new List<GameObject>();
            }

            list.Add(prefab);
            foreach(Transform t in prefab.transform)
            {
                GetAllObjects(t.gameObject, list);
            }
            return list;
        }

        //TODO: Move to ObjectDictionaries
        private void SetAsOurObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                ObjectDictionaries.SetAsOurObject(obj);
            }
        }

        //TODO: Move to ObjectDictionaries
        private void SetAsTheirObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                ObjectDictionaries.SetAsTheirs(obj, false);
            }
        }

        /// <summary>
        /// Creates "their" version of the file at the given path,
        /// named filename--THEIRS.unity.
        /// </summary>
        /// <param name="path">The path of the file, relative to the project folder.</param>
        private static void GetTheirVersionOf(string path)
        {
            fileName = path;

            string basepath = Path.GetDirectoryName(path);
            string sname = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            string ours = Path.Combine(basepath, sname + "--OURS" + extension);
            theirFilename = Path.Combine(basepath, sname + "--THEIRS" + extension);

            File.Copy(path, ours);
            try
            {
                ExecuteGit("checkout --theirs \"" + path + "\"");
            }
            catch(GitException e)
            {
                File.Delete(ours);
                throw e;
            }
            File.Move(path, theirFilename);
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
        private void CompleteSceneMerge()
        {
            MergeAction.inMergePhase = false;

            ObjectDictionaries.DestroyTheirObjects();
            ObjectDictionaries.Clear();
            EditorApplication.SaveScene();

            allMergeActions = null;

            //Mark as merged for git
            ExecuteGit("add \"" + fileName + "\"");

            //directly committing here might not be that smart, since there might be more conflicts

            ShowNotification(new GUIContent("Scene successfully merged."));
        }

        /// <summary>
        /// Completes the merge process after solving all conflicts.
        /// Cleans up the scene by deleting "their" GameObjects, clears merge related data structures,
        /// executes git add scene_name.
        /// </summary>
        private void CompletePrefabMerge()
        {
            MergeAction.inMergePhase = false;

            //ObjectDictionaries.Clear();

            allMergeActions = null;

            //TODO: Could we explicitly just save the prefab?
            AssetDatabase.SaveAssets();

            //Mark as merged for git
            ExecuteGit("add \"" + fileName + "\"");

            //directly committing here might not be that smart, since there might be more conflicts

            ourPrefab = null;

            //delete their prefab file
            AssetDatabase.DeleteAsset(theirFilename);

            OpenPreviousScene();
            ShowNotification(new GUIContent("Prefab successfully merged."));
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

            //If we aborted merging a prefab...
            if(!isMergingScene)
            {
                //delete prefab file
                AssetDatabase.DeleteAsset(theirFilename);
                OpenPreviousScene();
                ourPrefab = null;
            }
        }

        /// <summary>
        /// Opens the previously opened scene, if there was any.
        /// </summary>
        private static void OpenPreviousScene()
        {
            if(!string.IsNullOrEmpty(previouslyOpenedScene))
            {
                EditorApplication.OpenScene(previouslyOpenedScene);
                previouslyOpenedScene = "";
            }
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

            try
            {
                process.Start();
            }
            catch(Win32Exception)
            {
                throw new GitException();
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        private class GitException : System.Exception
        {
            public override string Message
            {
                get { return "Could not find git.exe. Please enter a valid git.exe path in the settings."; }
            }
        }
    }
}