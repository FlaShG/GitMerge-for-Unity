using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles GameObjects that exist in "their" version but not in "ours".
    /// </summary>
    public class MergeActionNewGameObject : MergeActionExistence
    {
        public MergeActionNewGameObject(GameObject ours, GameObject theirs)
            : base(ours, theirs)
        {
            if (GitMergeWindow.automerge)
            {
                UseTheirs();
            }
        }

        protected override void ApplyOurs()
        {
            if (ours)
            {
                ObjectDictionaries.RemoveCopyOf(theirs);
                GameObject.DestroyImmediate(ours, true);
            }
        }

        protected override void ApplyTheirs()
        {
            if (!ours)
            {
                ours = ObjectDictionaries.InstantiateFromMerging(theirs);
                ObjectDictionaries.SetAsCopy(ours, theirs);
            }
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