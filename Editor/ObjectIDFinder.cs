using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace GitMerge
{
    public static class ObjectIDFinder
    {
        //soooo hacky
        //credits to thelackey3326
        //http://forum.unity3d.com/threads/how-to-get-the-local-identifier-in-file-for-scene-objects.265686/

        public static int GetIdentifierFor(Object o)
        {
            if(o == null)
            {
                return -1;
            }

            var inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

            SerializedObject serializedObject = new SerializedObject(o);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            SerializedProperty localIdProp =
                serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

            return localIdProp.intValue;
        }
    }
}