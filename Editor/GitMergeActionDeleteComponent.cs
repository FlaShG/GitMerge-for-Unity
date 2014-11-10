using UnityEngine;
using UnityEditor;

public class GitMergeActionDeleteComponent : GitMergeAction
{
    protected Component ourComponent;
    protected Component copy;

    public GitMergeActionDeleteComponent(GameObject ours, Component ourComponent)
        : base(ours, null)
    {
        this.ourComponent = ourComponent;

        var go = new GameObject("GitMerge Object");
        go.SetActiveForMerging(false);

        copy = go.AddComponent(ourComponent);
        UseOurs();
    }

    protected override void ApplyOurs()
    {
        if(ours == null)
        {
            ourComponent = ours.AddComponent(copy);
        }
    }

    protected override void ApplyTheirs()
    {
        if(ours != null)
        {
            Object.DestroyImmediate(ourComponent);
        }
    }

    public override void OnGUI()
    {
        var defaultOptionColor = merged ? Color.gray : Color.white;

        GUI.color = usingOurs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Keep Component"))
        {
            UseOurs();
        }
        GUI.color = usingTheirs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Delete Component"))
        {
            UseTheirs();
        }
    }
}
