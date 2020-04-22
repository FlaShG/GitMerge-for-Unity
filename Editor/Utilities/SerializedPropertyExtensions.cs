using UnityEngine;
using UnityEditor;

namespace GitMerge
{
    public static class SerializedPropertyExtensions
    {
        public static object GetValue(this SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.AnimationCurve:
                    return p.animationCurveValue;
                case SerializedPropertyType.ArraySize:
                    return p.intValue;
                case SerializedPropertyType.Boolean:
                    return p.boolValue;
                case SerializedPropertyType.Bounds:
                    return p.boundsValue;
                case SerializedPropertyType.Character:
                    return p.stringValue; //TODO: might be bullshit
                case SerializedPropertyType.Color:
                    return p.colorValue;
                case SerializedPropertyType.Enum:
                    return p.enumValueIndex;
                case SerializedPropertyType.Float:
                    return p.floatValue;
                case SerializedPropertyType.Generic: //(arrays)
                    if (p.isArray)
                    {
                        var arr = new object[p.arraySize];
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            arr[i] = p.GetArrayElementAtIndex(i).GetValue();
                        }
                        return arr;
                    }
                    else
                    {
                        return null;
                    }
                case SerializedPropertyType.Gradient:
                    return 0; //TODO: erm
                case SerializedPropertyType.Integer:
                    return p.intValue;
                case SerializedPropertyType.LayerMask:
                    return p.intValue;
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue;
                case SerializedPropertyType.Quaternion:
                    return p.quaternionValue;
                case SerializedPropertyType.Rect:
                    return p.rectValue;
                case SerializedPropertyType.String:
                    return p.stringValue;
                case SerializedPropertyType.Vector2:
                    return p.vector2Value;
                case SerializedPropertyType.Vector3:
                    return p.vector3Value;
                case SerializedPropertyType.Vector4:
                    return p.vector4Value;
                default:
                    return 0;
            }
        }

        public static void SetValue(this SerializedProperty p, object value)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.AnimationCurve:
                    p.animationCurveValue = value as AnimationCurve;
                    break;
                case SerializedPropertyType.ArraySize:
                    //TODO: erm
                    p.intValue = (int)value;
                    break;
                case SerializedPropertyType.Boolean:
                    p.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Bounds:
                    p.boundsValue = (Bounds)value;
                    break;
                case SerializedPropertyType.Character:
                    p.stringValue = (string)value; //TODO: might be bullshit
                    break;
                case SerializedPropertyType.Color:
                    p.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.Enum:
                    p.enumValueIndex = (int)value;
                    break;
                case SerializedPropertyType.Float:
                    p.floatValue = (float)value;
                    break;
                case SerializedPropertyType.Generic: //(arrays)
                    if (p.isArray)
                    {
                        //var size = p.arraySize;
                        //var resetPath = p.propertyPath;
                        var values = (object[])value;
                        /*
                        for(int i = 0; i < p.arraySize; ++i)
                        {
                            Debug.Log(i + ": " + p.GetArrayElementAtIndex(i).GetValue());
                        }
                        */
                        p.ClearArray();
                        for (int i = 0; i < values.Length; ++i)
                        {
                            p.InsertArrayElementAtIndex(i);
                            //Debug.Log(i + ": " + pv.GetArrayElementAtIndex(i).GetValue());
                            p.GetArrayElementAtIndex(i).SetValue(values[i]);
                        }

                        //p.FindPropertyRelative(resetPath);
                    }
                    else
                    {
                        //p.stringValue = (string)value;
                    }
                    break;
                case SerializedPropertyType.Gradient:
                    //TODO: erm
                    break;
                case SerializedPropertyType.Integer:
                    p.intValue = (int)value;
                    break;
                case SerializedPropertyType.LayerMask:
                    p.intValue = (int)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    p.objectReferenceValue = value as Object; //TODO: what about non-UnityEngine objects?
                    break;
                case SerializedPropertyType.Quaternion:
                    p.quaternionValue = (Quaternion)value;
                    break;
                case SerializedPropertyType.Rect:
                    p.rectValue = (Rect)value;
                    break;
                case SerializedPropertyType.String:
                    p.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Vector2:
                    p.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    p.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    p.vector4Value = (Vector4)value;
                    break;
                default:
                    break;
            }
        }

        public static string GetPlainName(this SerializedProperty p)
        {
            var s = p.name;
            var i = s.IndexOf('_');
            if (i >= 0)
            {
                s = s.Substring(i + 1);
            }
            return s;
        }

        public static bool IsRealArray(this SerializedProperty p)
        {
            return p.propertyType == SerializedPropertyType.Generic && p.isArray;
        }
    }
}