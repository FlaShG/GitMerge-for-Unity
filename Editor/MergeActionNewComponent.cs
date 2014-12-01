using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    public class MergeActionNewComponent : MergeAction
    {
        protected Component ourComponent;
        protected Component theirComponent;

        public MergeActionNewComponent(GameObject ours, Component theirComponent)
            : base(ours, null)
        {
            this.theirComponent = theirComponent;
        }

        protected override void ApplyOurs()
        {
            if(ourComponent)
            {
                ObjectDictionaries.RemoveInstanceOf(theirComponent);
                Object.DestroyImmediate(ourComponent);
            }
        }

        protected override void ApplyTheirs()
        {
            if(!ourComponent)
            {
                ourComponent = ours.AddComponent(theirComponent);
                ObjectDictionaries.SetAsInstance(ourComponent, theirComponent);
            }
        }

        public override void OnGUI()
        {
            GUILayout.Label(theirComponent.GetPlainType());

            var defaultOptionColor = merged ? Color.gray : Color.white;

            GUI.color = usingOurs ? Color.green : defaultOptionColor;
            if(GUILayout.Button("Don't add Component"))
            {
                UseOurs();
            }
            GUI.color = usingTheirs ? Color.green : defaultOptionColor;
            if(GUILayout.Button("Add new Component"))
            {
                UseTheirs();
            }
        }
    }
}