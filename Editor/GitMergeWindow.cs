using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using GitMerge.Utilities;

namespace GitMerge
{
    /// <summary>
    /// The window that lets you perform merges on scenes and prefabs.
    /// </summary>
    public class GitMergeWindow : EditorWindow
    {
        private VCS vcs = new VCSGit();
        
        private const string EDITOR_PREFS_AUTOMERGE = "GitMerge_automerge";
        private const string EDITOR_PREFS_AUTOFOCUS = "GitMerge_autofocus";
        
        public static bool automerge { private set; get; }
        public static bool autofocus { private set; get; }
        
        private MergeManagerBase mergeManager;

        private MergeFilter filter = new MergeFilter();
        private MergeFilterBar filterBar = new MergeFilterBar();

        public bool mergeInProgress => mergeManager != null;

        private PageView pageView = new PageView();
        private Vector2 scrollPosition = Vector2.zero;
        private int tab = 0;
        private List<GameObjectMergeActions> mergeActionsFiltered;

        [MenuItem("Window/GitMerge")]
        static void OpenEditor()
        {
            var window = EditorWindow.GetWindow(typeof(GitMergeWindow), false, "GitMerge");
            // In case we're merging and the scene becomes edited,
            // the shown SerializedProperties should be repainted
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(500, 100);
        }

        private void OnEnable()
        {
            pageView.NumElementsPerPage = 200;
            filterBar.filter = filter;
            filter.OnChanged += CacheMergeActions;
            LoadSettings();
        }

        private static void LoadSettings()
        {
            automerge = EditorPrefs.GetBool(EDITOR_PREFS_AUTOMERGE, true);
            autofocus = EditorPrefs.GetBool(EDITOR_PREFS_AUTOFOCUS, true);
        }

        void OnHierarchyChange()
        {
            // Repaint if we changed the scene
            this.Repaint();
        }

        // Always check for editor state changes, and abort the active merge process if needed
        private void Update()
        {
            if (MergeAction.inMergePhase &&
                (EditorApplication.isCompiling ||
                 EditorApplication.isPlayingOrWillChangePlaymode))
            {
                ShowNotification(new GUIContent("Aborting merge due to editor state change."));
                AbortMerge(false);
            }
        }

        private void AbortMerge(bool showNotification = true)
        {
            mergeManager.AbortMerge(showNotification);
            mergeManager = null;
        }

        private void OnGUI()
        {
            Resources.DrawLogo();
            DrawTabButtons();
            switch (tab)
            {
                case 0:
                    OnGUIStartMergeTab();
                    break;

                default:
                    OnGUISettingsTab();
                    break;
            }
        }

        /// <summary>
        /// Tab that offers scene merging.
        /// </summary>
        private void OnGUIStartMergeTab()
        {
            if (!mergeInProgress)
            {
                DisplayPrefabMergeField();
                GUILayout.Space(20);
                DisplaySceneMergeButton();
            }
            else
            {
                DisplayMergeProcess();
            }
        }

        private void DisplaySceneMergeButton()
        {
            var activeScene = SceneManager.GetActiveScene();

            GUILayout.Label("Open Scene: " + activeScene.path);
            if (activeScene.path != "" &&
                !mergeInProgress &&
                GUILayout.Button("Start merging the open scene", GUILayout.Height(30)))
            {
                var manager = new MergeManagerScene(this, vcs);
                if (manager.TryInitializeMerge())
                {
                    this.mergeManager = manager;
                    CacheMergeActions();
                }
            }
        }

        private void DisplayPrefabMergeField()
        {
            if (!mergeInProgress)
            {
                var path = PathDetectingDragAndDropField("Drag a scene or prefab here to start merging", 80);
                if (path != null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    
                    if (IsPrefabAsset(asset))
                    {
                        var manager = new MergeManagerPrefab(this, vcs);
                        if (manager.TryInitializeMerge(path))
                        {
                            this.mergeManager = manager;
                            CacheMergeActions();
                        }
                    }
                    else if (IsSceneAsset(asset))
                    {
                        var manager = new MergeManagerScene(this, vcs);
                        if (manager.TryInitializeMerge(path))
                        {
                            this.mergeManager = manager;
                            CacheMergeActions();
                        }
                    }
                }
            }
        }

        private static bool IsPrefabAsset(Object asset)
        {
            var assetType = asset.GetType();
            return assetType == typeof(GameObject) ||
                   assetType == typeof(DefaultAsset);
        }

        private static bool IsSceneAsset(Object asset)
        {
            var assetType = asset.GetType();
            return assetType == typeof(SceneAsset);
        }

        private static string PathDetectingDragAndDropField(string text, float height)
        {
            var currentEvent = Event.current;

            using (new GUIBackgroundColor(Color.black))
            {
                // Caching these sounds good on paper, but Unity tends to forget them randomly
                var content = EditorGUIUtility.IconContent("RectMask2D Icon", string.Empty);
                content.text = text;
                
                var buttonStyle = GUI.skin.GetStyle("Button");
                var style = new GUIStyle(GUI.skin.GetStyle("Box"));
                style.stretchWidth = true;
                style.normal.background = buttonStyle.normal.background;
                style.normal.textColor = buttonStyle.normal.textColor;
                style.alignment = TextAnchor.MiddleCenter;
                style.imagePosition = ImagePosition.ImageAbove;

                GUILayout.Box(content, style, GUILayout.Height(height));
            }
            var rect = GUILayoutUtility.GetLastRect();

            if (rect.Contains(currentEvent.mousePosition))
            {
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                            var asset = DragAndDrop.objectReferences[0];
                            if (IsPrefabAsset(asset) || IsSceneAsset(asset))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            }
                            break;
                        case EventType.DragPerform:
                            var path = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
                            DragAndDrop.AcceptDrag();
                            return path;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tab that offers various settings for the tool.
        /// </summary>
        private void OnGUISettingsTab()
        {
            var vcsPath = vcs.GetExePath();
            var vcsPathNew = EditorGUILayout.TextField("Path to git.exe", vcsPath);
            if (vcsPath != vcsPathNew)
            {
                vcs.SetPath(vcsPathNew);
            }

            automerge = DisplaySettingsToggle(automerge,
                EDITOR_PREFS_AUTOMERGE,
                "Automerge",
                "(Automerge new/deleted GameObjects/Components upon merge start)");
            
            autofocus = DisplaySettingsToggle(autofocus,
                EDITOR_PREFS_AUTOFOCUS,
                "Auto Highlight",
                "(Highlight GameObjects when applying a MergeAction to it)");
        }

        private static bool DisplaySettingsToggle(bool value, string editorPrefsKey, string title, string description)
        {
            var newValue = EditorGUILayout.Toggle(title, value);
            if (value != newValue)
            {
                EditorPrefs.SetBool(editorPrefsKey, value);
            }
            GUILayout.Label(description);
            return newValue;
        }

        /// <summary>
        /// If no merge is in progress, draws the buttons to switch between tabs.
        /// Otherwise, draws the "abort merge" button.
        /// </summary>
        private void DrawTabButtons()
        {
            if (!mergeInProgress)
            {
                string[] tabs = { "Merge", "Settings" };
                tab = GUI.SelectionGrid(new Rect(72, 36, 300, 22), tab, tabs, 3);
            }
            else
            {
                GUI.backgroundColor = new Color(1, 0.4f, 0.4f, 1);
                if (GUI.Button(new Rect(72, 36, 300, 22), "Abort merge"))
                {
                    mergeManager.AbortMerge();
                    mergeManager = null;
                }
                GUI.backgroundColor = Color.white;
            }
        }

        /// <summary>
        /// Displays all MergeActions and the "apply merge" button if a merge is in progress.
        /// </summary>
        private void DisplayMergeProcess()
        {
            DrawCommandBar();

            var done = DisplayMergeActions();
            GUILayout.BeginHorizontal();
            if (done && GUILayout.Button("Apply merge", GUILayout.Height(40)))
            {
                mergeManager.CompleteMerge();
                mergeManager = null;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Display extra commands to simplify merge process
        /// </summary>
        private void DrawCommandBar()
        {
            DrawQuickMergeSideSelectionCommands();
            filterBar.Draw();
        }

        /// <summary>
        /// Allow to select easily 'use ours' or 'use theirs' for all actions
        /// </summary>
        private void DrawQuickMergeSideSelectionCommands()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Use ours", "Use theirs for all. Do not apply merge automatically.")))
                {
                    mergeManager.allMergeActions.ForEach((action) => action.UseOurs());
                }
                if (GUILayout.Button(new GUIContent("Use theirs", "Use theirs for all. Do not apply merge automatically.")))
                {
                    mergeManager.allMergeActions.ForEach((action) => action.UseTheirs());
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays all GameObjectMergeActions.
        /// </summary>
        /// <returns>True, if all MergeActions are flagged as "merged".</returns>
        private bool DisplayMergeActions()
        {
            var textColor = GUI.skin.label.normal.textColor;
            GUI.skin.label.normal.textColor = Color.black;

            bool done = true;

            pageView.Draw(mergeActionsFiltered.Count, (index) =>
            {
                var actions = mergeActionsFiltered[index];
                actions.OnGUI();
                done = done && actions.merged;
            });

            GUI.skin.label.normal.textColor = textColor;
            return done;
        }

        private void CacheMergeActions()
        {
            if (filter.useFilter)
            {
                mergeActionsFiltered = mergeManager.allMergeActions.Where((actions) => filter.IsPassingFilter(actions)).ToList();
            }
            else
            {
                mergeActionsFiltered = mergeManager.allMergeActions;
            }
        }
    }
}