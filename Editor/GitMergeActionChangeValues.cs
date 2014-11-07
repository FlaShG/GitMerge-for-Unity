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

    public GitMergeActionChangeValues(GameObject ours, GameObject theirs, SerializedProperty ourProperty, SerializedProperty theirProperty)
        : base(ours, theirs)
    {
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
        GUILayout.Label(ourString, GUILayout.Width(100));
        if(GUILayout.Button(">>>"))
        {
            UseOurs();
        }
        EditorGUILayout.PropertyField(ourProperty);
        if(GUILayout.Button("<<<"))
        {
            UseTheirs();
        }
        GUILayout.Label(theirString, GUILayout.Width(100));
    }
}
