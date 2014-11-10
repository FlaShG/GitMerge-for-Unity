using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GitMergeActions
{
    public GameObject ours { private set; get; }
    public GameObject theirs { private set; get; }
    private string name;
    public bool merged { private set; get; }
    public bool hasActions
    {
        get { return actions.Count > 0; }
    }
    private List<GitMergeAction> actions;

    
    public GitMergeActions(GameObject ours, GameObject theirs)
    {
        actions = new List<GitMergeAction>();

        this.ours = ours;
        this.theirs = theirs;
        name = "";
        if(ours)
        {
            name = "Your["+GetPath(ours)+"]";
        }
        if(theirs)
        {
            if(ours)
            {
                name += " vs. ";
            }
            name += "Their["+GetPath(theirs)+"]";
        }

        if(theirs && !ours)
        {
            actions.Add(new GitMergeActionNewGameObject(ours, theirs));
        }
        if(ours && !theirs)
        {
            actions.Add(new GitMergeActionDeleteGameObject(ours, theirs));
        }
        if(ours && theirs)
        {
            FindComponentDifferences();
        }

        //Some Actions have a default and are merged from the beginning.
        //If all the others did was to add GameObjects, we're done with merging from the start.
        CheckIfMerged();
    }

    private void FindComponentDifferences()
    {
        var ourComponents = ours.GetComponents<Component>();
        var theirComponents = theirs.GetComponents<Component>();

        var theirDict = new Dictionary<int, Component>();
        foreach(var theirComponent in theirComponents)
        {
            theirDict.Add(ObjectIDFinder.GetIdentifierFor(theirComponent), theirComponent);
        }

        foreach(var ourComponent in ourComponents)
        {
            var id = ObjectIDFinder.GetIdentifierFor(ourComponent);
            Component theirComponent;
            theirDict.TryGetValue(id, out theirComponent);

            if(theirComponent) //both components exist
            {
                FindPropertyDifferences(ourComponent, theirComponent);
            }
            else //component doesn't exist in their version, offer a deletion
            {
                actions.Add(new GitMergeActionDeleteComponent(ours, ourComponent));
            }

            theirDict.Remove(id);
        }

        foreach(var theirComponent in theirDict.Values)
        {
            //new Components from them
            actions.Add(new GitMergeActionNewComponent(ours, theirComponent));
        }
    }

    private void FindPropertyDifferences(Component ourComponent, Component theirComponent)
    {
        var ourSerialized = new SerializedObject(ourComponent);
        var theirSerialized = new SerializedObject(theirComponent);

        var ourProperty = ourSerialized.GetIterator();
        if(ourProperty.Next(true))
        {
            var theirProperty = theirSerialized.GetIterator();
            theirProperty.Next(true);
            while(ourProperty.NextVisible(false))
            {
                theirProperty.NextVisible(false);

                if(!ourProperty.GetValue(true).Equals(theirProperty.GetValue(true)))
                {
                    actions.Add(new GitMergeActionChangeValues(ours, theirs, ourProperty.Copy(), theirProperty.Copy()));
                }
            }
        }
    }

    private static string GetPath(GameObject g)
    {
        var t = g.transform;
        var sb = new StringBuilder(t.name);
        while(t.parent != null)
        {
            t = t.parent;
            sb.Append(t.name+"/", 0, 1);
        }
        return sb.ToString();
    }
    
    public void CheckIfMerged()
    {
        merged = actions.TrueForAll(action => action.merged);
    }

    public void UseOurs()
    {
        foreach(var action in actions)
        {
            action.UseOurs();
        }
    }

    public void OnGUI()
    {
        GUILayout.Label(name);
        foreach(var action in actions)
        {
            if(action.OnGUIMerge())
            {
                CheckIfMerged();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    
}
