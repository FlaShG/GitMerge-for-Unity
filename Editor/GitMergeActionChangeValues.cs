using UnityEngine;
using UnityEditor;
using System.Collections;

public class GitMergeActionChangeValues : GitMergeAction
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
    protected Component ourComponent;

    public GitMergeActionChangeValues(GameObject ours, Component ourComponent, SerializedProperty ourProperty, SerializedProperty theirProperty)
        : base(ours, null)
    {
        this.ourComponent = ourComponent;

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
            var obj = GitMergeOriginalObjects.GetOriginalObject(id);

            //If we didn't have our own version of the object before, it must be new
            if(!obj)
            {
                //Get our copy of the new object if it exists
                obj = GitMergeOriginalObjects.GetInstanceOf(value as Object);
            }

            value = obj;
        }

        ourProperty.SetValue(value);
        ourProperty.serializedObject.ApplyModifiedProperties();
    }

    public override void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Label(ourComponent.GetPlainType() + "." + ourProperty.GetPlainName());

        GUILayout.BeginHorizontal();

        GUILayout.Label(ourString, GUILayout.Width(100));

        if(MergeButton(">>>", usingOurs))
        {
            UseOurs();
        }

        var c = GUI.backgroundColor;
        GUI.backgroundColor = Color.white;

        var oldValue = ourProperty.GetValue();
        EditorGUILayout.PropertyField(ourProperty, new GUIContent(""), GUILayout.Width(170));
        if(!object.Equals(ourProperty.GetValue(), oldValue))
        {
            ourProperty.serializedObject.ApplyModifiedProperties();
            UsedNew();
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
