using UnityEngine;
using UnityEditor;

public class GitMergeActionNewComponent : GitMergeAction
{
    protected Component ourComponent;
    protected Component theirComponent;

    public GitMergeActionNewComponent(GameObject ours, Component theirComponent)
        : base(ours, null)
    {
        this.theirComponent = theirComponent;
    }

    protected override void ApplyOurs()
    {
        if(ourComponent)
        {
            Object.DestroyImmediate(ourComponent);
        }
    }

    protected override void ApplyTheirs()
    {
        if(!ourComponent)
        {
            ourComponent = ours.AddComponent(theirComponent);
        }
    }

    public override void OnGUI()
    {
        GUILayout.Label(TypeOf(theirComponent));

        var defaultOptionColor = merged ? Color.gray : Color.white;

        GUI.color = usingOurs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Don't add Component"))
        {
            UseOurs();
        }
        GUI.color = usingTheirs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Add new Component"))
        {
            UseTheirs();
        }
    }

    private string TypeOf(object o)
    {
        var s = o.GetType().ToString();
        var i = s.LastIndexOf('.');
        if(i >= 0)
        {
            s = s.Substring(i+1);
        }
        return s;
    }
}
