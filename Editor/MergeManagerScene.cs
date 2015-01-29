using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
            isMergingScene = true;

            //Ask if the scene should be saved, because...
            if(!EditorApplication.SaveCurrentSceneIfUserWantsTo())
            {
                return false;
            }
            //...we are reloading it to prevent objects from not having a scene id.
            EditorApplication.OpenScene(EditorApplication.currentScene);

            MergeAction.inMergePhase = false;

            ObjectDictionaries.Clear();

            //checkout "their" version
            GetTheirVersionOf(EditorApplication.currentScene);
            AssetDatabase.Refresh();

            //find all of "our" objects
            var ourObjects = GetAllSceneObjects();
            ObjectDictionaries.SetAsOurObjects(ourObjects);

            //add "their" objects
            EditorApplication.OpenSceneAdditive(theirFilename);

            //delete scene file
            AssetDatabase.DeleteAsset(theirFilename);

            //find all of "their" objects
            var addedObjects = GetAllNewSceneObjects(ourObjects);
            ObjectDictionaries.SetAsTheirObjects(addedObjects);

            //create list of differences that have to be merged
            BuildAllMergeActions(ourObjects, addedObjects);

            if(allMergeActions.Count == 0)
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
            var old = oldObjects;

            foreach(var obj in old)
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
            EditorApplication.SaveScene();

            allMergeActions = null;

            //Mark as merged for git
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

            //Save scene
            EditorApplication.SaveScene();
        }
    }
}