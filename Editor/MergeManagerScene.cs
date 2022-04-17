﻿
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.SceneManagement;
    using UnityEditor.SceneManagement;
    using System.Collections.Generic;

    public class MergeManagerScene : MergeManagerBase
    {
        private const int NUMBER_OF_INITIALIZATION_STEPS = 3;

        private Scene theirScene;

        public MergeManagerScene(GitMergeWindow window, VCS vcs)
            : base(window, vcs)
        {

        }

        public bool TryInitializeMerge(string path = null)
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            if (activeScene.isDirty)
            {
                window.ShowNotification(new GUIContent("Please make sure there are no unsaved changes before attempting to merge."));
                return false;
            }

            if (!CheckVCSAvailability())
            {
                return false;
            }

            DisplayProgressBar(0, "Checking out scene...");
            isMergingScene = true;
            var scenePath = path ?? activeScene.path;

            // Overwrite the current scene to prevent the reload/ignore dialog that pops up after the upcoming changes to the file.
            // Pressing "reload" on it would invalidate the GameObject references we're about to collect.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.ForceStop();

            vcs.CheckoutOurs(scenePath);
            CheckoutTheirVersionOf(scenePath);
            AssetDatabase.Refresh();

            DisplayProgressBar(1, "Opening scene...");

            activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            MergeAction.inMergePhase = false;
            ObjectDictionaries.Clear();

            List<GameObject> ourObjects;
            try
            {
                DisplayProgressBar(2, "Collecting differences...");
                // Find all of "our" objects
                ourObjects = GetAllSceneObjects();
                ObjectDictionaries.AddToOurObjects(ourObjects);

                // Add "their" objects
                theirScene = EditorSceneManager.OpenScene(theirFilename, OpenSceneMode.Additive);

                var addedObjects = GetAllNewSceneObjects(ourObjects);
                ObjectDictionaries.AddToTheirObjects(addedObjects);
                BuildAllMergeActions(ourObjects, addedObjects);
                
                MoveGameObjectsToScene(theirScene.GetRootGameObjects(), activeScene);
            }
            finally
            {
                EditorSceneManager.UnloadSceneAsync(theirScene);
                AssetDatabase.DeleteAsset(theirFilename);

                EditorUtility.ClearProgressBar();
            }
            
            if (allMergeActions.Count == 0)
            {
                window.ShowNotification(new GUIContent("No conflict found for this scene."));
                return false;
            }

            MergeAction.inMergePhase = true;
            return true;
        }

        private static void DisplayProgressBar(int step, string text)
        {
            var progress = step / (float)NUMBER_OF_INITIALIZATION_STEPS;
            EditorUtility.DisplayProgressBar("GitMerge for Unity", text, progress);
        }

        private static void MoveGameObjectsToScene(IEnumerable<GameObject> addedObjects, Scene scene)
        {
            foreach (var obj in addedObjects)
            {
                EditorSceneManager.MoveGameObjectToScene(obj, scene);
            }
        }

        private static List<GameObject> GetAllSceneObjects()
        {
            var objects = (GameObject[])Object.FindObjectsOfType(typeof(GameObject));
            return new List<GameObject>(objects);
        }

        /// <summary>
        /// Finds all GameObjects in the scene, minus the ones passed.
        /// </summary>
        private static List<GameObject> GetAllNewSceneObjects(List<GameObject> oldObjects)
        {
            var all = GetAllSceneObjects();

            foreach (var obj in oldObjects)
            {
                all.Remove(obj);
            }

            return all;
        }

        /// <summary>
        /// Completes the merge process after solving all conflicts.
        /// Cleans up the scene by deleting "their" GameObjects, clears merge related data structures,
        /// executes git add scene_name.
        /// </summary>
        public override void CompleteMerge()
        {
            MergeAction.inMergePhase = false;

            ObjectDictionaries.DestroyTheirObjects();
            ObjectDictionaries.Clear();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            allMergeActions = null;

            vcs.MarkAsMerged(fileName);

            // Directly committing here might not be that smart, since there might be more conflicts

            window.ShowNotification(new GUIContent("Scene successfully merged."));
        }

        /// <summary>
        /// Aborts merge by using "our" version in all conflicts.
        /// Cleans up merge related data.
        /// </summary>
        public override void AbortMerge(bool showNotification = true)
        {
            base.AbortMerge(showNotification);
            
            EditorSceneManager.CloseScene(theirScene, true);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}