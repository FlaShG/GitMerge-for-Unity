
namespace GitMerge
{
    using System.Text;
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
            // Focussing on the same object twice, zooms in to the coordinate instead of the bounding box.
            if (Selection.activeObject == go) {
                return;
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            var view = SceneView.lastActiveSceneView;
            if (view)
            {
                view.FrameSelected();
            }
        }
        
        /// <summary>
        /// Gets the path of this GameObject in the hierarchy.
        /// </summary>
        public static string GetPath(this GameObject gameObject)
        {
            var t = gameObject.transform;
            var sb = new StringBuilder(RemovePostfix(t.name));
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, RemovePostfix(t.name) + "/");
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Returns a child of this GameObject that has the same relative path to this GamObject as
        /// the given GameObject has to it's root GameObject. 
        /// </summary>
        public static GameObject GetChildWithEqualPath(this GameObject gameObject, GameObject otherGameObject)
        {
            string fullHierarchyPath = otherGameObject.GetPath();
            string relativeHierarchyPath = fullHierarchyPath.Substring(fullHierarchyPath.IndexOf("/", 1) + 1);
            Transform result = gameObject.transform.Find(relativeHierarchyPath);
            return result != null ? result.gameObject : gameObject; // fallback to root if a GameObject with equal path doesn't exist
        }
        
        private static string RemovePostfix(string name)
        {
            if (name.EndsWith(MergeManagerBase.THEIR_FILE_POSTFIX))
            {
                return name.Substring(0, name.Length - MergeManagerBase.THEIR_FILE_POSTFIX.Length);
            }

            return name;
        }
    }
}