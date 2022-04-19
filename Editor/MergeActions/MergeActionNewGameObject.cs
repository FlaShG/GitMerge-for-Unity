using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles GameObjects that exist in "their" version but not in "ours".
    /// </summary>
    public class MergeActionNewGameObject : MergeActionExistence
    {
        private readonly bool theirsWasActive;

        public MergeActionNewGameObject(GameObject ours, GameObject theirs)
            : base(ours, theirs)
        {
            theirsWasActive = theirs.activeSelf;

            if (GitMergeWindow.automerge)
            {
                UseTheirs();
            }
        }

        protected override void ApplyOurs()
        {
            /*
            if (ours)
            {
                ObjectDictionaries.RemoveCopyOf(theirs);
                GameObject.DestroyImmediate(ours, true);
            }
            */
            theirs.SetActive(false);
        }

        protected override void ApplyTheirs()
        {
            if (!ours)
            {
                ours = ObjectDictionaries.InstantiateFromMerging(theirs);
                ObjectDictionaries.SetAsCopy(ours, theirs);
            }
            theirs.SetActive(theirsWasActive);
        }

        public override bool TryGetDiscardedObject(out Object discardedObject)
        {
            if (usingOurs)
            {
                discardedObject = theirs;
                return true;
            }

            discardedObject = null;
            return false;
        }

        public override void EnsureExistence()
        {
            UseTheirs();
        }

        public override void OnGUI()
        {
            var defaultOptionColor = merged ? Color.gray : Color.white;

            GUI.color = usingOurs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Don't add GameObject"))
            {
                UseOurs();
            }
            GUI.color = usingTheirs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Add new GameObject"))
            {
                UseTheirs();
            }
        }
    }
}