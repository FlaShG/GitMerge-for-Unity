using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles a GameObject which exists in "their" version but not "ours".
    /// </summary>
    public class MergeActionDeleteGameObject : MergeAction
    {
        private bool oursWasActive;

        public MergeActionDeleteGameObject(GameObject ours, GameObject theirs)
            : base(ours, theirs)
        {
            oursWasActive = ours.activeSelf;

            if(GitMergeWindow.automerge)
            {
                UseOurs();
            }
        }

        protected override void ApplyOurs()
        {
            ours.SetActiveForMerging(true);
            ours.SetActive(oursWasActive);
        }

        protected override void ApplyTheirs()
        {
            ours.SetActiveForMerging(false);
            SceneView.currentDrawingSceneView.Repaint();
        }

        public override void OnGUI()
        {
            var defaultOptionColor = merged ? Color.gray : Color.white;

            GUI.color = usingOurs ? Color.green : defaultOptionColor;
            if(GUILayout.Button("Keep GameObject"))
            {
                UseOurs();
            }
            GUI.color = usingTheirs ? Color.green : defaultOptionColor;
            if(GUILayout.Button("Delete GameObject"))
            {
                UseTheirs();
            }
        }
    }
}