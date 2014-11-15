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
        ourProperty.SetValue(theirInitialValue);
        ourProperty.serializedObject.ApplyModifiedProperties();
    }

    public override void OnGUI()
    {
        GUILayout.EndHorizontal();
        GUILayout.Label(ourComponent.GetPlainType());
        GUILayout.BeginHorizontal();


        GUILayout.Label(ourString, GUILayout.Width(100));
        if(GUILayout.Button(">>>"))
        {
            UseOurs();
        }

        var c = GUI.backgroundColor;
        GUI.backgroundColor = Color.white;

        var oldValue = ourProperty.GetValue();
        EditorGUILayout.PropertyField(ourProperty, new GUIContent(""));
        if(!object.Equals(ourProperty.GetValue(), oldValue))
        {
            ourProperty.serializedObject.ApplyModifiedProperties();
            UsedNew();
        }

        GUI.backgroundColor = c;

        if(GUILayout.Button("<<<"))
        {
            UseTheirs();
        }
        GUILayout.Label(theirString, GUILayout.Width(100));
    }
}
