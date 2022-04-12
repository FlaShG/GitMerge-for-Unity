
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;

    public static class GameObjectExtensions
    {
        /// <summary>
        /// Adds the copy of a Component to a GameObject.
        /// </summary>
        /// <param name="go">The GameObject that will get the new Component</param>
        /// <param name="original">The original component to copy</param>
        /// <returns>The reference to the newly added Component copy</returns>
        public static Component AddComponent(this GameObject go, Component original)
        {
            var newComponent = go.AddComponent(original.GetType());

            var originalProperty = new SerializedObject(original).GetIterator();
            var newSerializedObject = new SerializedObject(newComponent);
            var newProperty = newSerializedObject.GetIterator();

            if (originalProperty.Next(true))
            {
                newProperty.Next(true);

                while (originalProperty.NextVisible(true))
                {
                    newProperty.NextVisible(true);
                    newProperty.SetValue(originalProperty.GetValue());
                }
            }

            newSerializedObject.ApplyModifiedProperties();

            return newComponent;
        }

        /// <summary>
        /// Activates/deactivates the GameObjct, and hides it when it is disabled.
        /// This is used for "their" objects to hide them while merging.
        /// </summary>
        /// <param name="go">The object do enable/disable</param>
        /// <param name="active">Enable or disable the object?</param>
        public static void SetActiveForMerging(this GameObject go, bool active)
        {
            go.SetActive(active);
            go.hideFlags = active ? HideFlags.None : HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Ping the GameObject in the hierarchy, select it, and center it in the scene view.
        /// </summary>
        /// <param name="go">The GameObject of interest</param>
        public static void Highlight(this GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            var view = SceneView.lastActiveSceneView;
            if (view)
            {
                view.FrameSelected();
            }
        }
    }
}