using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
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

        //EditorPrefs keys for settings
        private const string epAutomerge = "GitMerge_automerge";
        private const string epAutofocus = "GitMerge_autofocus";

        //Settings
        public static bool automerge { private set; get; }
        public static bool autofocus { private set; get; }

        //The MergeManager that has the actual merging logic
        private MergeManager manager;
        private MergeFilter filter = new MergeFilter();
        private MergeFilterBar filterBar = new MergeFilterBar();

        public bool mergeInProgress
        {
            get
            {
                return manager != null;
            }
        }

        private PageView pageView = new PageView();
        private Vector2 scrollPosition = Vector2.zero;
        private int tab = 0;
        private List<GameObjectMergeActions> mergeActionsFiltered;

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
            pageView.NumElementsPerPage = 200;
            filterBar.filter = filter;
            filter.OnChanged += CacheMergeActions;
            LoadSettings();
        }

        private static void LoadSettings()
        {
            if (EditorPrefs.HasKey(epAutomerge))
            {
                automerge = EditorPrefs.GetBool(epAutomerge);
            }
            else
            {
                automerge = true;
            }
            if (EditorPrefs.HasKey(epAutofocus))
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
            if (MergeAction.inMergePhase
            && (EditorApplication.isCompiling
            || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                ShowNotification(new GUIContent("Aborting merge due to editor state change."));
                AbortMerge(false);
            }
        }

        private void AbortMerge(bool showNotification = true)
        {
            manager.AbortMerge(showNotification);
            manager = null;
        }

        void OnGUI()
        {
            Resources.DrawLogo();
            DrawTabButtons();
            switch (tab)
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
            var activeScene = SceneManager.GetActiveScene();

            GUILayout.Label("Open Scene: " + activeScene.path);
            if (activeScene.path != ""
               && !mergeInProgress
               && GUILayout.Button("Start merging this scene", GUILayout.Height(80)))
            {
                var mm = new MergeManagerScene(this, vcs);
                if (mm.InitializeMerge())
                {
                    manager = mm;
                    CacheMergeActions();
                }
            }

            DisplayMergeProcess();
        }

        /// <summary>
        /// Tab that offers prefab merging.
        /// </summary>
        private void OnGUIPrefabTab()
        {
            GameObject prefab;
            if (!mergeInProgress)
            {
                GUILayout.Label("Drag your prefab here to start merging:");
                if (prefab = EditorGUILayout.ObjectField(null, typeof(GameObject), false, GUILayout.Height(60)) as GameObject)
                {
                    var mm = new MergeManagerPrefab(this, vcs);
                    if (mm.InitializeMerge(prefab))
                    {
                        manager = mm;
                        CacheMergeActions();
                    }
                }
            }

            DisplayMergeProcess();
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

            var amNew = EditorGUILayout.Toggle("Automerge", automerge);
            if (automerge != amNew)
            {
                automerge = amNew;
                EditorPrefs.SetBool(epAutomerge, automerge);
            }
            GUILayout.Label("(Automerge new/deleted GameObjects/Components upon merge start)");

            var afNew = EditorGUILayout.Toggle("Auto Highlight", autofocus);
            if (autofocus != afNew)
            {
                autofocus = afNew;
                EditorPrefs.SetBool(epAutofocus, autofocus);
            }
            GUILayout.Label("(Highlight GameObjects when applying a MergeAction to it)");
        }

        /// <summary>
        /// If no merge is in progress, draws the buttons to switch between tabs.
        /// Otherwise, draws the "abort merge" button.
        /// </summary>
        private void DrawTabButtons()
        {
            if (!mergeInProgress)
            {
                string[] tabs = { "Merge Scene", "Merge Prefab", "Settings" };
                tab = GUI.SelectionGrid(new Rect(72, 36, 300, 22), tab, tabs, 3);
            }
            else
            {
                GUI.backgroundColor = new Color(1, 0.4f, 0.4f, 1);
                if (GUI.Button(new Rect(72, 36, 300, 22), "Abort merge"))
                {
                    manager.AbortMerge();
                    manager = null;
                }
                GUI.backgroundColor = Color.white;
            }
        }

        /// <summary>
        /// Displays all MergeActions and the "apply merge" button if a merge is in progress.
        /// </summary>
        private void DisplayMergeProcess()
        {
            if (mergeInProgress)
            {
                DrawCommandBar();

                var done = DisplayMergeActions();
                GUILayout.BeginHorizontal();
                if (done && GUILayout.Button("Apply merge"))
                {
                    manager.CompleteMerge();
                    manager = null;
                }
                GUILayout.EndHorizontal();
            }
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
                    manager.allMergeActions.ForEach((action) => action.UseOurs());
                }
                if (GUILayout.Button(new GUIContent("Use theirs", "Use theirs for all. Do not apply merge automatically.")))
                {
                    manager.allMergeActions.ForEach((action) => action.UseTheirs());
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
                mergeActionsFiltered = manager.allMergeActions.Where((actions) => filter.IsPassingFilter(actions)).ToList();
            }
            else
            {
                mergeActionsFiltered = manager.allMergeActions;
            }
        }
    }
}