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
        protected string ourString
        {
            get { return ourInitialValue != null ? ourInitialValue.ToString() : "[none]"; }
        }
        protected string theirString
        {
            get { return theirInitialValue != null ? theirInitialValue.ToString() : "[none]"; }
        }
        protected Object ourObject;

        public MergeActionChangeValues(GameObject ours, Object ourObject, SerializedProperty ourProperty, SerializedProperty theirProperty)
            : base(ours, null)
        {
            this.ourObject = ourObject;
            
            this.ourProperty = ourProperty;
            this.theirProperty = theirProperty;

            ourInitialValue = ourProperty.GetValue();
            theirInitialValue = theirProperty.GetValue();
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

            GUILayout.Label(ourString, GUILayout.Width(100));

            if(MergeButton(">>>", usingOurs))
            {
                UseOurs();
            }

            var c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            if(ourProperty.isArray)
            {
                GUILayout.Label("[Array]");
            }
            else
            {
                var oldValue = ourProperty.GetValue();
                EditorGUILayout.PropertyField(ourProperty, new GUIContent(""), GUILayout.Width(170));
                if(!object.Equals(ourProperty.GetValue(), oldValue))
                {
                    ourProperty.serializedObject.ApplyModifiedProperties();
                    UsedNew();
                }
            }

            GUI.backgroundColor = c;

            if(MergeButton("<<<", usingTheirs))
            {
                UseTheirs();
            }
            GUILayout.Label(theirString, GUILayout.Width(100));

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
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