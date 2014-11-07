using UnityEngine;

public class GitMergeActionNewComponent : GitMergeAction
{
    protected Component ourComponent;
    protected Component theirComponent;

    public GitMergeActionNewComponent(GameObject ours, GameObject theirs, Component ourComponent, Component theirComponent)
        : base(ours, theirs)
    {
        this.ourComponent = ourComponent;
        this.theirComponent = theirComponent;
    }

    protected override void ApplyOurs()
    {

    }

    protected override void ApplyTheirs()
    {

    }

    public override void OnGUI()
    {

    }
}
