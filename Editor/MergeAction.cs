﻿using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GitMerge
{
    /// <summary>
    /// Each MergeAction represents a single, specific merge conflict.
    /// This can be a GameObject added or deleted in one of the versions,
    /// a Component added or deleted on a GameObject,
    /// or a single property changed on a Component.
    /// </summary>
    public abstract class MergeAction
    {
        //Don't highlight objects if not in merge phase.
        //Prevents highlighting while automerging.
        public static bool inMergePhase;

        //A MergeAction is considere "merged" when, at some point,
        //"our", "their" or a new version has been applied.
        public bool merged { private set; get; }

        public GameObject ours { protected set; get; }
        public GameObject theirs { protected set; get; }

        //Flags that indicate how this MergeAction has been resolved.
        protected bool usingOurs;
        protected bool usingTheirs;
        protected bool usingNew;
        //True when this action has been automatically resolved
        protected bool automatic;


        public MergeAction(GameObject ours, GameObject theirs)
        {
            this.ours = ours;
            this.theirs = theirs;
        }

        public void UseOurs()
        {
            merged = true;
            ApplyOurs();
            usingOurs = true;
            usingTheirs = false;
            usingNew = false;

            automatic = !inMergePhase;

            HighlightObject();
        }
        public void UseTheirs()
        {
            merged = true;
            ApplyTheirs();
            usingOurs = false;
            usingTheirs = true;
            usingNew = false;

            automatic = !inMergePhase;

            HighlightObject();
        }
        public void UsedNew()
        {
            merged = true;
            usingOurs = false;
            usingTheirs = false;
            usingNew = true;

            automatic = !inMergePhase;
        }

        //The implementations of these methods conatain the actual merging steps
        protected abstract void ApplyOurs();
        protected abstract void ApplyTheirs();

        /// <summary>
        /// Displays the MergeAction.
        /// </summary>
        /// <returns>True when the represented conflict has now been merged.</returns>
        public bool OnGUIMerge()
        {
            var wasMerged = merged;
            if(merged)
            {
                GUI.backgroundColor = automatic ? new Color(.9f, .9f, .3f, 1) : new Color(.2f, .8f, .2f, 1);
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

        //The actual UI of the MergeAction depends on the actual type
        public abstract void OnGUI();

        private void HighlightObject()
        {
            if(ours && inMergePhase)
            {
                ours.Highlight();
            }
        }
    }
}