using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GitMerge
{
    public static class GameObjectExtensions
    {
        public static Component AddComponent(this GameObject go, Component original)
        {
            var c = go.AddComponent(original.GetType());

            var originalSerialized = new SerializedObject(original).GetIterator();
            var newSerialized = new SerializedObject(c).GetIterator();

            if(originalSerialized.Next(true))
            {
                newSerialized.Next(true);

                while(originalSerialized.NextVisible(false))
                {
                    newSerialized.NextVisible(false);

                    newSerialized.SetValue(originalSerialized.GetValue());
                }
            }

            return c;
        }

        public static void SetActiveForMerging(this GameObject go, bool active)
        {
            go.SetActive(active);
            go.hideFlags = active ? HideFlags.None : HideFlags.HideAndDontSave;
        }

        public static void Highlight(this GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            var view = SceneView.lastActiveSceneView;
            if(view)
            {
                view.FrameSelected();
            }
        }
    }
}