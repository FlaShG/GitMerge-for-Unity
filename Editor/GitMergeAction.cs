using UnityEngine;
using UnityEditor;
using System.Collections;

public abstract class GitMergeAction
{
    //Don't highlight objects if not in merge phase
    public static bool inMergePhase;

    public bool merged { private set; get; }
    public GameObject ours { protected set; get; }
    public GameObject theirs { protected set; get; }
    protected bool usingOurs;
    protected bool usingTheirs;
    protected bool usingNew;
    protected bool automatic;


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

        automatic = !inMergePhase;

        HighlightObject();
    }
    public void UseTheirs()
    {
        merged = true;
        ApplyTheirs();
        usingOurs = false;
        usingTheirs = true;
        usingNew = false;

        automatic = !inMergePhase;

        HighlightObject();
    }
    public void UsedNew()
    {
        merged = true;
        usingOurs = false;
        usingTheirs = false;
        usingNew = true;

        automatic = !inMergePhase;
    }

    protected abstract void ApplyOurs();
    protected abstract void ApplyTheirs();

    public bool OnGUIMerge()
    {
        var wasMerged = merged;
        if(merged)
        {
            GUI.backgroundColor = automatic ? new Color(.9f, .9f, .3f, 1) : new Color(.2f, .8f, .2f, 1);
        }
        else
        {
            GUI.backgroundColor = new Color(1f, .25f, .25f, 1);
        }
        GUILayout.BeginHorizontal(GitMergeResources.styles.mergeAction);
        GUI.backgroundColor = Color.white;
        OnGUI();
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
        return merged && !wasMerged;
    }

    public abstract void OnGUI();

    private void HighlightObject()
    {
        if(ours && inMergePhase)
        {
            ours.Highlight();
        }
    }
}
