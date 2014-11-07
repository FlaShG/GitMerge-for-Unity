using UnityEngine;
using System.Collections.Generic;

public static class GitMergeGameObjectExtensions
{
    //This dict holds all of "their" GameObjects
    //<GameObject, originallyActive>
    private static Dictionary<GameObject, bool> objects = new Dictionary<GameObject, bool>();

	public static void SetAsMergeObject(this GameObject go, bool active)
    {
        if(!objects.ContainsKey(go))
        {
            objects.Add(go, go.activeSelf);
        }
        go.SetActiveForMerging(false);
    }

    public static void SetActiveForMerging(this GameObject go, bool active)
    {
        go.SetActive(active);
        go.hideFlags = active ? HideFlags.None : HideFlags.HideAndDontSave;
    }

    public static GameObject InstantiateForMerging(this GameObject go)
    {
        var copy = GameObject.Instantiate(go) as GameObject;
        
        bool wasActive;
        if(!objects.TryGetValue(go, out wasActive))
        {
            wasActive = go.activeSelf;
        }

        copy.SetActive(wasActive);
        copy.hideFlags = HideFlags.None;
        copy.name = go.name;

        return copy;
    }

    public static void DestroyAllMergeObjects()
    {
        foreach(var obj in objects.Keys)
        {
            Object.DestroyImmediate(obj);
        }
        objects.Clear();
    }
}
