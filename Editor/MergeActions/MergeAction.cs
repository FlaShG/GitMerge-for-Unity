﻿
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Each MergeAction represents a single, specific merge conflict.
    /// This can be a GameObject added or deleted in one of the versions,
    /// a Component added or deleted on a GameObject,
    /// or a single property changed on a Component.
    /// </summary>
    public abstract class MergeAction
    {
        // Don't highlight objects if not in merge phase.
        // Prevents highlighting while automerging.
        public static bool inMergePhase;

        // A MergeAction is considered "merged" when, at some point,
        // "our", "their" or a new version has been applied.
        public bool merged { protected set; get; }

        public GameObject ours { protected set; get; }
        public GameObject theirs { protected set; get; }

        // Flags that indicate how this MergeAction has been resolved.
        protected bool usingOurs;
        protected bool usingTheirs;
        protected bool usingNew;

        protected bool wasResolvedAutomatically;


        public MergeAction(GameObject ours, GameObject theirs)
        {
            this.ours = ours;
            this.theirs = theirs;
        }

        /// <summary>
        /// Apply "our" change in the conflict, dismissing "their"s.
        /// </summary>
        public void UseOurs()
        {
            try
            {
                ApplyOurs();
            }
            catch
            {
                return;
            }
            merged = true;
            usingOurs = true;
            usingTheirs = false;
            usingNew = false;

            wasResolvedAutomatically = !inMergePhase;

            if (GitMergeWindow.autofocus)
            {
                HighlightObject();
            }

            RefreshPrefabInstance();
        }

        /// <summary>
        /// Apply "their" change in the conflict, dismissing "our"s.
        /// </summary>
        public void UseTheirs()
        {
            try
            {
                ApplyTheirs();
            }
            catch
            {
                return;
            }
            merged = true;
            usingOurs = false;
            usingTheirs = true;
            usingNew = false;

            wasResolvedAutomatically = !inMergePhase;

            if (GitMergeWindow.autofocus)
            {
                HighlightObject();
            }

            RefreshPrefabInstance();
        }

        /// <summary>
        /// Mark this <see cref="MergeAction"/> to use a new value instead of either of the conflicting ones.
        /// </summary>
        public void UsedNew()
        {
            merged = true;
            usingOurs = false;
            usingTheirs = false;
            usingNew = true;

            wasResolvedAutomatically = !inMergePhase;

            RefreshPrefabInstance();
        }

        /// <summary>
        /// Refreshes the prefab instance, if there is any.
        /// We change the prefab directly, so we have to do this to see the changes in the scene view.
        /// </summary>
        private static void RefreshPrefabInstance()
        {
            if (MergeManagerBase.isMergingPrefab)
            {
                PrefabUtility.RevertObjectOverride(MergeManagerPrefab.ourPrefabInstance, InteractionMode.AutomatedAction);
            }
        }

        // The implementations of these methods conatain the actual merging steps
        protected abstract void ApplyOurs();
        protected abstract void ApplyTheirs();

        /// <summary>
        /// Displays the MergeAction.
        /// </summary>
        /// <returns>True when the represented conflict has now been merged.</returns>
        public bool OnGUIMerge()
        {
            var wasMerged = merged;
            if (merged)
            {
                GUI.backgroundColor = wasResolvedAutomatically ? new Color(.9f, .9f, .3f, 1) : new Color(.2f, .8f, .2f, 1);
            }
            else
            {
                GUI.backgroundColor = new Color(1f, .25f, .25f, 1);
            }
            GUILayout.BeginHorizontal(Resources.styles.mergeAction);
            GUI.backgroundColor = Color.white;
            OnGUI();
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            return merged && !wasMerged;
        }

        // The actual UI of the MergeAction depends on the actual type
        public abstract void OnGUI();

        private void HighlightObject()
        {
            // Highlight the instance of the prefab, not the prefab itself
            // Otherwise, "ours".
            var objectToHighlight = MergeManagerBase.isMergingPrefab ? MergeManagerPrefab.ourPrefabInstance.GetChildWithEqualPath(ours) : ours;
            if (objectToHighlight && inMergePhase && objectToHighlight.hideFlags == HideFlags.None)
            {
                objectToHighlight.Highlight();
            }
        }
    }
}