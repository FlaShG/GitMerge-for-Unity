
namespace GitMerge
{
    using UnityEngine;
    using System.IO;
    using System.Collections.Generic;
    using UnityEditor;

    public abstract class MergeManager
    {
        protected VCS vcs { private set; get; }
        protected GitMergeWindow window { private set; get; }

        internal List<GameObjectMergeActions> allMergeActions;

        protected static string fileName;
        protected static string theirFilename;

        public static bool isMergingScene { protected set; get; }
        public static bool isMergingPrefab { get { return !isMergingScene; } }


        public MergeManager(GitMergeWindow window, VCS vcs)
        {
            this.window = window;
            this.vcs = vcs;
            allMergeActions = new List<GameObjectMergeActions>();
        }

        /// <summary>
        /// Creates "their" version of the file at the given path,
        /// named filename--THEIRS.unity.
        /// </summary>
        /// <param name="path">The path of the file, relative to the project folder.</param>
        protected void CheckoutTheirVersionOf(string path)
        {
            fileName = path;

            string basepath = Path.GetDirectoryName(path);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            string ourFilename = Path.Combine(basepath, sceneName + "--OURS" + extension);
            theirFilename = Path.Combine(basepath, sceneName + "--THEIRS" + extension);

            File.Copy(path, ourFilename);
            try
            {
                vcs.CheckoutTheirs(path);
            }
            catch (VCSException e)
            {
                File.Delete(ourFilename);
                throw e;
            }
            File.Move(path, theirFilename);
            File.Move(ourFilename, path);
        }

        /// <summary>
        /// Finds all specific merge conflicts between two sets of GameObjects,
        /// representing "our" scene and "their" scene.
        /// </summary>
        /// <param name="ourObjects">The GameObjects of "our" version of the scene.</param>
        /// <param name="theirObjects">The GameObjects of "their" version of the scene.</param>
        protected void BuildAllMergeActions(List<GameObject> ourObjects, List<GameObject> theirObjects)
        {
            allMergeActions = new List<GameObjectMergeActions>();

            //Map "their" GameObjects to their respective ids
            var theirObjectsDict = new Dictionary<ObjectID, GameObject>();
            foreach (var theirs in theirObjects)
            {
                theirObjectsDict.Add(ObjectID.GetFor(theirs), theirs);
            }

            foreach (var ours in ourObjects)
            {
                //Try to find "their" equivalent to "our" GameObjects
                var id = ObjectID.GetFor(ours);
                GameObject theirs;
                theirObjectsDict.TryGetValue(id, out theirs);

                //If theirs is null, mergeActions.hasActions will be false
                var mergeActions = new GameObjectMergeActions(ours, theirs);
                if (mergeActions.hasActions)
                {
                    allMergeActions.Add(mergeActions);
                }
                //Remove "their" GameObject from the dict to only keep those new to us
                theirObjectsDict.Remove(id);
            }

            //Every GameObject left in the dict is a...
            foreach (var theirs in theirObjectsDict.Values)
            {
                //...new GameObject from them
                var mergeActions = new GameObjectMergeActions(null, theirs);
                if (mergeActions.hasActions)
                {
                    allMergeActions.Add(mergeActions);
                }
            }
        }

        public abstract void CompleteMerge();

        public virtual void AbortMerge(bool showNotification = true)
        {
            MergeAction.inMergePhase = false;

            foreach (var actions in allMergeActions)
            {
                actions.UseOurs();
            }
            ObjectDictionaries.DestroyTheirObjects();
            ObjectDictionaries.Clear();
            allMergeActions = null;

            if (showNotification)
            {
                window.ShowNotification(new GUIContent("Merge aborted."));
            }
        }
    }
}