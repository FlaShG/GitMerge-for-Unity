
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using UnityEditor.SceneManagement;

    public class MergeManagerPrefab : MergeManagerBase
    {
        public static GameObject ourPrefab { private set; get; }
        private static GameObject theirPrefab;
        public static GameObject ourPrefabInstance { private set; get; }
        private static string previouslyOpenedScenePath;


        public MergeManagerPrefab(GitMergeWindow window, VCS vcs)
            : base(window, vcs)
        {

        }
        
        public bool TryInitializeMerge(string prefabPath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            isMergingScene = false;
            MergeAction.inMergePhase = false;

            ObjectDictionaries.Clear();
            
            vcs.CheckoutOurs(prefabPath);
            CheckoutTheirVersionOf(prefabPath);
            AssetDatabase.Refresh();

            ourPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (ourPrefab == null)
            {
                DeleteTheirPrefabAndLoadPreviousScene();
                return false;
            }

            // Open a new Scene that will only display the prefab.
            previouslyOpenedScenePath = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.ForceStop();

            // Instantiate our object in order to view it while merging.
            ourPrefabInstance = PrefabUtility.InstantiatePrefab(ourPrefab) as GameObject;
            
            // UI Elements need a Canvas to be displayed correctly:
            if (ourPrefabInstance.GetComponentInChildren<RectTransform>() != null) {
                GameObject defaultCanvas = new GameObject("Canvas");
                defaultCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                ourPrefabInstance.transform.SetParent(defaultCanvas.transform, false);
            }

            var ourObjects = GetAllObjects(ourPrefab);

            theirPrefab = AssetDatabase.LoadAssetAtPath(theirFilename, typeof(GameObject)) as GameObject;
            var theirObjects = GetAllObjects(theirPrefab);
            
            BuildAllMergeActions(ourObjects, theirObjects);

            AssetDatabase.DeleteAsset(theirFilename);

            if (allMergeActions.Count == 0)
            {
                DeleteTheirPrefabAndLoadPreviousScene();
                window.ShowNotification(new GUIContent("No conflict found for this prefab."));
                return false;
            }
            MergeAction.inMergePhase = true;
            ourPrefabInstance.Highlight();
            return true;
        }

        private static void DeleteTheirPrefabAndLoadPreviousScene()
        {
            AssetDatabase.DeleteAsset(theirFilename);
            OpenPreviousScene();
        }

        /// <summary>
        /// Recursively find all GameObjects that are part of the prefab
        /// </summary>
        /// <param name="prefab">The prefab to analyze</param>
        /// <param name="list">The list with all the objects already found. Pass null in the beginning.</param>
        /// <returns>The list with all the objects</returns>
        private static List<GameObject> GetAllObjects(GameObject prefab, List<GameObject> list = null)
        {
            if (list == null)
            {
                list = new List<GameObject>();
            }

            list.Add(prefab);
            foreach (Transform t in prefab.transform)
            {
                GetAllObjects(t.gameObject, list);
            }
            return list;
        }

        /// <summary>
        /// Completes the merge process after solving all conflicts.
        /// Cleans up the scene by deleting "their" GameObjects, clears merge related data structures,
        /// executes git add scene_name.
        /// </summary>
        public override void CompleteMerge()
        {
            MergeAction.inMergePhase = false;

            // ObjectDictionaries.Clear();

            allMergeActions = null;

            // TODO: Could we explicitly just save the prefab?
            AssetDatabase.SaveAssets();
            
            vcs.MarkAsMerged(fileName);

            // Directly committing here might not be that smart, since there might be more conflicts.

            ourPrefab = null;

            DeleteTheirPrefabAndLoadPreviousScene();
            window.ShowNotification(new GUIContent("Prefab successfully merged."));
        }

        /// <summary>
        /// Aborts merge by using "our" version in all conflicts.
        /// Cleans up merge related data.
        /// </summary>
        public override void AbortMerge(bool showNotification = true)
        {
            base.AbortMerge(showNotification);

            DeleteTheirPrefabAndLoadPreviousScene();
            ourPrefab = null;
        }

        /// <summary>
        /// Opens the previously opened scene, if there was any.
        /// </summary>
        private static void OpenPreviousScene()
        {
            if (!string.IsNullOrEmpty(previouslyOpenedScenePath))
            {
                EditorSceneManager.OpenScene(previouslyOpenedScenePath);
                previouslyOpenedScenePath = null;
            }
        }
    }
}