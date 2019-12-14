using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace GitMerge
{
    public class MergeManagerScene : MergeManager
    {
        public MergeManagerScene(GitMergeWindow window, VCS vcs)
            : base(window, vcs)
        {

        }

        public bool InitializeMerge()
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            if (activeScene.isDirty)
            {
                window.ShowNotification(new GUIContent("Please make sure there are no unsaved changes before attempting to merge."));
                return false;
            }

            isMergingScene = true;
            var scenePath = activeScene.path;
            
            vcs.CheckoutOurs(scenePath);
            EditorSceneManager.OpenScene(scenePath);
            
            MergeAction.inMergePhase = false;

            ObjectDictionaries.Clear();

            //checkout "their" version
            CheckoutTheirVersionOf(scenePath);
            AssetDatabase.Refresh();

            List<GameObject> ourObjects;
            Scene theirScene;
            try
            {
                //find all of "our" objects
                ourObjects = GetAllSceneObjects();
                ObjectDictionaries.AddToOurObjects(ourObjects);

                //add "their" objects
                theirScene = EditorSceneManager.OpenScene(theirFilename, OpenSceneMode.Additive);
            }
            finally
            {
                //delete scene file
                AssetDatabase.DeleteAsset(theirFilename);
            }
            
            //find all of "their" objects
            var addedObjects = GetAllNewSceneObjects(ourObjects);
            ObjectDictionaries.AddToTheirObjects(addedObjects);

            //create list of differences that have to be merged
            BuildAllMergeActions(ourObjects, addedObjects);

            EditorSceneManager.UnloadSceneAsync(theirScene);

            if (allMergeActions.Count == 0)
            {
                window.ShowNotification(new GUIContent("No conflict found for this scene."));
                return false;
            }
            MergeAction.inMergePhase = true;
            return true;
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

            //directly committing here might not be that smart, since there might be more conflicts

            window.ShowNotification(new GUIContent("Scene successfully merged."));
        }

        /// <summary>
        /// Aborts merge by using "our" version in all conflicts.
        /// Cleans up merge related data.
        /// </summary>
        public override void AbortMerge()
        {
            base.AbortMerge();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}