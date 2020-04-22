using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The abstract base MergeAction for all MergeActions that manage whether or not an object exists
    /// </summary>
    public abstract class MergeActionExistence : MergeAction
    {
        public MergeActionExistence(GameObject ours, GameObject theirs)
            : base(ours, theirs)
        {
            ObjectDictionaries.AddToSchroedingersObjects(ours ?? theirs, this);
        }

        /// <summary>
        /// Apply whatever version that has the object existing, since it might be needed somewhere.
        /// When overriding, call either UseOurs or UseTheirs to make sure to trigger the side effects.
        /// </summary>
        public abstract void EnsureExistence();
    }
}