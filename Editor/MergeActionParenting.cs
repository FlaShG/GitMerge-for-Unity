using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction that handles a differing parents for a Transform.
    /// </summary>
    public class MergeActionParenting : MergeAction
    {
        private Transform transform;
        private Transform ourParent;
        private Transform theirParent;

        public MergeActionParenting(Transform transform, Transform ourParent, Transform theirParent)
            : base(transform.gameObject, null)
        {
            this.transform = transform;
            this.ourParent = ourParent;
            this.theirParent = theirParent;
        }

        protected override void ApplyOurs()
        {
            transform.parent = ourParent;
        }

        protected override void ApplyTheirs()
        {
            var ourVersion = ObjectDictionaries.GetOurCounterpartFor(theirParent) as Transform;
            if (theirParent && !ourVersion)
            {
                if (EditorUtility.DisplayDialog("The chosen parent currently does not exist.", "Do you want do add it?", "Yes", "No"))
                {
                    ObjectDictionaries.EnsureExistence(theirParent.gameObject);
                    ourVersion = ObjectDictionaries.GetOurCounterpartFor(theirParent) as Transform;

                    transform.parent = ourVersion;
                }
                else
                {
                    throw new System.Exception("User Abort.");
                }
            }
            else
            {
                transform.parent = ourVersion;
            }
        }

        public override void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Parent");

            GUILayout.BeginHorizontal();

            GUILayout.Label(ourParent ? ourParent.ToString() : "None", GUILayout.Width(100));

            if (MergeButton(">>>", usingOurs))
            {
                UseOurs();
            }

            var c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            var newParent = EditorGUILayout.ObjectField(transform.parent, typeof(Transform), true, GUILayout.Width(170)) as Transform;
            if (newParent != transform.parent)
            {
                transform.parent = newParent;
                UsedNew();
            }
            GUI.backgroundColor = c;

            if (MergeButton("<<<", usingTheirs))
            {
                UseTheirs();
            }

            GUILayout.Label(theirParent ? theirParent.ToString() : "None", GUILayout.Width(100));

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static bool MergeButton(string text, bool green)
        {
            if (green)
            {
                GUI.color = Color.green;
            }
            bool result = GUILayout.Button(text, GUILayout.ExpandWidth(false));
            GUI.color = Color.white;
            return result;
        }
    }
}