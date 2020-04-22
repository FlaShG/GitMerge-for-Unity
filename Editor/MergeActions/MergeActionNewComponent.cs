using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles Components that exist in "our" version but not in "theirs".
    /// </summary>
    public class MergeActionNewComponent : MergeActionExistence
    {
        protected Component ourComponent;
        protected Component theirComponent;

        public MergeActionNewComponent(GameObject ours, Component theirComponent)
            : base(ours, null)
        {
            this.theirComponent = theirComponent;

            if (GitMergeWindow.automerge)
            {
                UseOurs();
            }
        }

        protected override void ApplyOurs()
        {
            if (ourComponent)
            {
                ObjectDictionaries.RemoveCopyOf(theirComponent);
                Object.DestroyImmediate(ourComponent, true);
            }
        }

        protected override void ApplyTheirs()
        {
            if (!ourComponent)
            {
                ourComponent = ours.AddComponent(theirComponent);
                ObjectDictionaries.SetAsCopy(ourComponent, theirComponent);
            }
        }

        public override void EnsureExistence()
        {
            UseTheirs();
        }

        public override void OnGUI()
        {
            GUILayout.Label(theirComponent.GetPlainType());

            var defaultOptionColor = merged ? Color.gray : Color.white;

            GUI.color = usingOurs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Don't add Component"))
            {
                UseOurs();
            }
            GUI.color = usingTheirs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Add new Component"))
            {
                UseTheirs();
            }
        }
    }
}