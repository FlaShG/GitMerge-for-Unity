using UnityEngine;
using System.Collections.Generic;

public static class GitMergeOriginalObjects
{
    //This dict holds all of "our" GameObjects
    //Needed for Reference handling
    //<fileID, GameObject>
    private static Dictionary<int, Object> ourObjects = new Dictionary<int, Object>();

    public static void SetAsOriginalObject(this GameObject go)
    {
        Add(go);
        foreach(var c in go.GetComponents<Component>())
        {
            Add(c);
        }
    }

    private static void Add(Object o)
    {
        ourObjects.Add(ObjectIDFinder.GetIdentifierFor(o), o);
    }

    public static Object GetOriginalObject(int id)
    {
        Object result = null;
        ourObjects.TryGetValue(id, out result);
        return result;
    }

    internal static void Clear()
    {
        ourObjects.Clear();
    }
}
