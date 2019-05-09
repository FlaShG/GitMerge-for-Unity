using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles a Component which exists in "their" version but not "ours".
    /// </summary>
    public class MergeActionDeleteComponent : MergeActionExistence
    {
        protected Component ourComponent;
        protected Component copy;

        public MergeActionDeleteComponent(GameObject ours, Component ourComponent)
            : base(ours, null)
        {
            this.ourComponent = ourComponent;

            var go = new GameObject("GitMerge Object");
            go.SetActiveForMerging(false);

            copy = go.AddComponent(ourComponent);

            if (GitMergeWindow.automerge)
            {
                UseOurs();
            }
        }

        protected override void ApplyOurs()
        {
            if (ourComponent == null)
            {
                ourComponent = ours.AddComponent(copy);
                ObjectDictionaries.SetAsOurObject(ourComponent);
            }
        }

        protected override void ApplyTheirs()
        {
            if (ourComponent != null)
            {
                ObjectDictionaries.RemoveOurObject(ourComponent);
                Object.DestroyImmediate(ourComponent, true);
            }
        }

        public override void EnsureExistence()
        {
            UseOurs();
        }

        public override void OnGUI()
        {
            GUILayout.Label(copy.GetPlainType());

            var defaultOptionColor = merged ? Color.gray : Color.white;

            GUI.color = usingOurs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Keep Component"))
            {
                UseOurs();
            }
            GUI.color = usingTheirs ? Color.green : defaultOptionColor;
            if (GUILayout.Button("Delete Component"))
            {
                UseTheirs();
            }
        }
    }
}