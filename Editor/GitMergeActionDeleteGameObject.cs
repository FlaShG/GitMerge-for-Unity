using UnityEngine;
using System.Collections;

public class GitMergeActionDeleteGameObject : GitMergeAction
{
    private GameObject copy;

    public GitMergeActionDeleteGameObject(GameObject ours, GameObject theirs)
        : base(ours, theirs)
    {
        copy = GameObject.Instantiate(ours) as GameObject;
        copy.name = ours.name;
        copy.hideFlags = HideFlags.HideAndDontSave;

        UseOurs();
    }

    protected override void ApplyOurs()
    {
        if(ours == null)
        {
            ours = GameObject.Instantiate(copy) as GameObject;
            ours.name = copy.name;
            ours.hideFlags = HideFlags.None;
        }
    }

    protected override void ApplyTheirs()
    {
        if(ours != null)
        {
            GameObject.DestroyImmediate(ours);
        }
    }

    public override void OnGUI()
    {
        var defaultOptionColor = merged ? Color.gray : Color.white;

        GUI.color = usingOurs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Keep GameObject"))
        {
            UseOurs();
        }
        GUI.color = usingTheirs ? Color.green : defaultOptionColor;
        if(GUILayout.Button("Delete GameObject"))
        {
            UseTheirs();
        }
    }
}