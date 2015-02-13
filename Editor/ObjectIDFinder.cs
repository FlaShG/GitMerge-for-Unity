﻿using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace GitMerge
{
    public static class ObjectIDFinder
    {
        //soooo hacky
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

		public static int GetSerializedIdentifier(this Object o)
		{
			return GetIdentifierFor(o);
		}
    }
}