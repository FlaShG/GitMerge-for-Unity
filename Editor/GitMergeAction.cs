using UnityEngine;
using UnityEditor;
using System.Collections;

public abstract class GitMergeAction
{
    public bool merged { private set; get; }
    protected GameObject ours;
    protected GameObject theirs;
    protected bool usingOurs;
    protected bool usingTheirs;
    protected bool usingNew;


    public GitMergeAction(GameObject ours, GameObject theirs)
    {
        this.ours = ours;
        this.theirs = theirs;
    }

    public void UseOurs()
    {
        merged = true;
        ApplyOurs();
        usingOurs = true;
        usingTheirs = false;
        usingNew = false;

        HighlightObject();
    }
    public void UseTheirs()
    {
        merged = true;
        ApplyTheirs();
        usingOurs = false;
        usingTheirs = true;
        usingNew = false;

        HighlightObject();
    }
    public void UsedNew()
    {
        usingOurs = false;
        usingTheirs = false;
        usingNew = true;
    }

    protected abstract void ApplyOurs();
    protected abstract void ApplyTheirs();

    public bool OnGUIMerge()
    {
        var wasMerged = merged;
        GUI.backgroundColor = merged ? Color.green : Color.red;
        GUILayout.BeginHorizontal(EditorStyles.inspectorFullWidthMargins);
        OnGUI();
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
        return merged && !wasMerged;
    }

    public abstract void OnGUI();

    private void HighlightObject()
    {
        if(ours)
        {
            ours.Highlight();
        }
    }
}
