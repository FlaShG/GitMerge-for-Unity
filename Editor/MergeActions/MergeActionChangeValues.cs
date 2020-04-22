
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;
    using System.Linq;

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
        protected readonly string fieldname;

        public MergeActionChangeValues(GameObject ours, SerializedProperty ourProperty, SerializedProperty theirProperty)
            : base(ours, null)
        {
            this.ourProperty = ourProperty;
            this.theirProperty = theirProperty;

            fieldname = ourProperty.serializedObject.targetObject.GetPlainType() + "." + ourProperty.GetPlainName();

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
            if (ourProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                var id = ObjectID.GetFor(theirInitialValue as Object);
                var obj = ObjectDictionaries.GetOurObject(id);

                //If we didn't have our own version of the object before, it must be new
                if (!obj)
                {
                    //Get our copy of the new object if it exists
                    obj = ObjectDictionaries.GetOurInstanceOfCopy(value as Object);
                }

                value = obj;
            }

            ourProperty.SetValue(value);
            ourProperty.serializedObject.ApplyModifiedProperties();
        }

        public override void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(fieldname + ": " + ourProperty.propertyType);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(ourString, GUILayout.Width(100));
            DisplayArray(ourInitialValue);
            GUILayout.EndVertical();

            if (MergeButton(">>>", usingOurs))
            {
                UseOurs();
            }

            var c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            //GUILayout.Label(ourProperty.propertyType + "/" + ourProperty.type + ": " + ourProperty.GetValue());
            PropertyField(ourProperty);

            GUI.backgroundColor = c;

            if (MergeButton("<<<", usingTheirs))
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
            if (ourProperty.IsRealArray() && ourProperty.isExpanded)
            {
                var values = (object[])value;
                for (int i = 0; i < values.Length; ++i)
                {
                    GUILayout.Label(ValueString(values[i]), GUILayout.Width(100));
                }
            }
        }

        /// <summary>
        /// Displays the property field in the center of the window.
        /// This method distinguishes between certain properties.
        /// The GameObject tag, for example, shouldn't be displayed with a regular string field.
        /// </summary>
        /// <param name="p">The SerializedProerty to display</param>
        /// <param name="width">The width of the whole thing in the ui</param>
        private void PropertyField(SerializedProperty p, float width = 170)
        {
            if (p.IsRealArray())
            {
                DisplayArrayProperty(p, width);
            }
            else
            {
                var oldValue = p.GetValue();
                if (fieldname == "GameObject.TagString")
                {
                    var oldTag = oldValue as string;
                    var newTag = EditorGUILayout.TagField("", oldTag, GUILayout.Width(width));
                    if (newTag != oldTag)
                    {
                        p.SetValue(newTag);
                    }
                }
                else if (fieldname == "GameObject.StaticEditorFlags")
                {
                    DisplayStaticFlagChooser(p, width);
                }
                else
                {
                    EditorGUILayout.PropertyField(p, new GUIContent(""), GUILayout.Width(width));
                }
                if (!object.Equals(p.GetValue(), oldValue))
                {
                    p.serializedObject.ApplyModifiedProperties();
                    UsedNew();
                }
            }
        }

        private void DisplayArrayProperty(SerializedProperty p, float width)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(GUILayout.Width(170));
            EditorGUILayout.PropertyField(p, new GUIContent("Array"), GUILayout.Width(80));
            if (p.isExpanded)
            {
                var copy = p.Copy();
                var size = copy.arraySize;

                copy.Next(true);
                copy.Next(true);

                PropertyField(copy, 70);
                GUILayout.EndHorizontal();

                for (int i = 0; i < size; ++i)
                {
                    copy.Next(false);
                    PropertyField(copy);
                }
            }
            else
            {
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Displays Toggles that let the user set the static flags of the object.
        /// </summary>
        /// <param name="p">The StaticEditorFlags SerializedProperty to display</param>
        /// <param name="width">The width of the whole thing in the ui</param>
        private void DisplayStaticFlagChooser(SerializedProperty p, float width)
        {
            var flags = (StaticEditorFlags)p.intValue;
            GUILayout.BeginVertical(GUILayout.Width(width));

            p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, SerializedValueString(p));
            var allOn = true;
            if (p.isExpanded)
            {
                foreach (var flag in System.Enum.GetValues(typeof(StaticEditorFlags)).Cast<StaticEditorFlags>())
                {
                    var wasOn = (flags & flag) != 0;
                    var on = EditorGUILayout.Toggle(flag + "", wasOn);
                    if (wasOn != on)
                    {
                        flags = flags ^ flag;
                    }
                    if (!on)
                    {
                        allOn = false;
                    }
                }
            }
            if (allOn)
            {
                flags = (StaticEditorFlags)(-1);
            }
            p.intValue = (int)flags;

            GUILayout.EndVertical();
        }

        private string SerializedValueString(SerializedProperty p)
        {
            if (fieldname == "GameObject.StaticEditorFlags")
            {
                switch (p.intValue)
                {
                    case 0:
                        return "Not static";
                    case -1:
                        return "Static";
                    default:
                        return "Mixed static";
                }
            }
            else if (p.IsRealArray())
            {
                return "Array[" + p.arraySize + "]";
            }
            return ValueString(p.GetValue());
        }

        private static string ValueString(object o)
        {
            if (o == null)
            {
                return "[none]";
            }
            return o.ToString();
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