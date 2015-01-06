using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GitMerge
{
    /// <summary>
    /// The MergeAction allowing to merge the value of a single property of a Component.
    /// </summary>
    public class MergeActionChangeValues : MergeAction
    {
        protected SerializedProperty ourProperty;
        protected SerializedProperty theirProperty;
        protected object ourInitialValue;
        protected object theirInitialValue;
        protected readonly string ourString;
        protected readonly string theirString;
        protected Object ourObject;

        public MergeActionChangeValues(GameObject ours, Object ourObject, SerializedProperty ourProperty, SerializedProperty theirProperty)
            : base(ours, null)
        {
            this.ourObject = ourObject;

            this.ourProperty = ourProperty;
            this.theirProperty = theirProperty;

            ourInitialValue = ourProperty.GetValue();
            theirInitialValue = theirProperty.GetValue();

            ourString = SerializedValueString(ourProperty);
            theirString = SerializedValueString(theirProperty);
        }

        protected override void ApplyOurs()
        {
            ourProperty.SetValue(ourInitialValue);
            ourProperty.serializedObject.ApplyModifiedProperties();
        }

        protected override void ApplyTheirs()
        {
            var value = theirInitialValue;

            //If we're about references here, get "our" version of the object.
            if(ourProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                var id = ObjectIDFinder.GetIdentifierFor(theirInitialValue as Object);
                var obj = ObjectDictionaries.GetOurObject(id);

                //If we didn't have our own version of the object before, it must be new
                if(!obj)
                {
                    //Get our copy of the new object if it exists
                    obj = ObjectDictionaries.GetOurVersionOf(value as Object);
                }

                value = obj;
            }

            ourProperty.SetValue(value);
            ourProperty.serializedObject.ApplyModifiedProperties();
        }

        public override void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(ourObject.GetPlainType() + "." + ourProperty.GetPlainName() + ": " + ourProperty.propertyType);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(ourString, GUILayout.Width(100));
            DisplayArray(ourInitialValue);
            GUILayout.EndVertical();

            if(MergeButton(">>>", usingOurs))
            {
                UseOurs();
            }

            var c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            //GUILayout.Label(ourProperty.propertyType + "/" + ourProperty.type + ": " + ourProperty.GetValue());

            if(ourProperty.IsRealArray())
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal(GUILayout.Width(170));
                EditorGUILayout.PropertyField(ourProperty, new GUIContent("Array"), GUILayout.Width(80));
                if(ourProperty.isExpanded)
                {
                    var ourPropertyCopy = ourProperty.Copy();
                    var size = ourPropertyCopy.arraySize;

                    ourPropertyCopy.Next(true);
                    ourPropertyCopy.Next(true);

                    PropertyField(ourPropertyCopy, 70);
                    GUILayout.EndHorizontal();

                    for(int i = 0; i < size; ++i)
                    {
                        ourPropertyCopy.Next(false);
                        PropertyField(ourPropertyCopy);
                    }
                }
                else
                {
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            else
            {
                PropertyField(ourProperty);
            }

            GUI.backgroundColor = c;

            if(MergeButton("<<<", usingTheirs))
            {
                UseTheirs();
            }

            GUILayout.BeginVertical();
            GUILayout.Label(theirString, GUILayout.Width(100));
            DisplayArray(theirInitialValue);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DisplayArray(object value)
        {
            if(ourProperty.isExpanded)
            {
                var values = (object[])value;
                for(int i = 0; i < values.Length; ++i)
                {
                    GUILayout.Label(ValueString(values[i]), GUILayout.Width(100));
                }
            }
        }

        private void PropertyField(SerializedProperty p, float width = 170)
        {
            var oldValue = p.GetValue();
            EditorGUILayout.PropertyField(p, new GUIContent(""), GUILayout.Width(width));
            if(!object.Equals(p.GetValue(), oldValue))
            {
                p.serializedObject.ApplyModifiedProperties();
                UsedNew();
            }
        }

        private static string SerializedValueString(SerializedProperty p)
        {
            if(p.IsRealArray())
            {
                return "Array[" + p.arraySize + "]";
            }
            return ValueString(p.GetValue());
        }

        private static string ValueString(object o)
        {
            if(o == null)
            {
                return "[none]";
            }
            return o.ToString();
        }

        private static bool MergeButton(string text, bool green)
        {
            if(green)
            {
                GUI.color = Color.green;
            }
            bool result = GUILayout.Button(text, GUILayout.ExpandWidth(false));
            GUI.color = Color.white;
            return result;
        }
    }
}