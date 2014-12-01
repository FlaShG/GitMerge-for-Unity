using UnityEngine;
using System.Collections.Generic;

namespace GitMerge
{
    public static class ObjectDictionaries
    {
        //This dict holds all of "our" objects
        //Needed for Reference handling
        //<fileID, GameObject>
        private static Dictionary<int, Object> ourObjects = new Dictionary<int, Object>();

        //This dict maps our instances of their objects
        //Whenever we instantiate a copy of "their" new object, they're both added here
        private static Dictionary<Object, Object> ourInstances = new Dictionary<Object, Object>();

        //This dict holds all of "their" GameObjects
        //Needed for scene cleaning after merge
        //<GameObject, originallyActive>
        private static Dictionary<GameObject, bool> theirObjects = new Dictionary<GameObject, bool>();

        public static void SetAsOriginalObject(GameObject go)
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

        public static void Remove(GameObject go)
        {
            RemoveObject(go);
            foreach(var c in go.GetComponents<Component>())
            {
                RemoveObject(c);
            }
        }

        private static void RemoveObject(Object o)
        {
            ourObjects.Remove(ObjectIDFinder.GetIdentifierFor(o));
        }

        public static Object GetOriginalObject(int id)
        {
            Object result = null;
            ourObjects.TryGetValue(id, out result);
            return result;
        }

        public static void Clear()
        {
            ourObjects.Clear();
            theirObjects.Clear();
            ourInstances.Clear();
        }

        /// <summary>
        /// Mark o as an instance of theirs
        /// </summary>
        public static void SetAsInstance(GameObject o, GameObject theirs)
        {
            ourInstances.Add(theirs, o);
            var instanceComponents = o.GetComponents<Component>();
            var theirComponents = theirs.GetComponents<Component>();
            for(int i = 0; i < instanceComponents.Length; ++i)
            {
                SetAsInstance(instanceComponents[i], theirComponents[i]);
            }
        }

        public static void SetAsInstance(Component c, Component theirs)
        {
            ourInstances.Add(theirs, c);
        }

        public static void RemoveInstanceOf(GameObject theirs)
        {
            ourInstances.Remove(theirs);
            foreach(var c in theirs.GetComponents<Component>())
            {
                ourInstances.Remove(c);
            }
        }

        public static void RemoveInstanceOf(Component theirs)
        {
            ourInstances.Remove(theirs);
        }

        /// <summary>
        /// Returns:
        /// * the given object if it is "ours"
        /// * "our" instance of obj if it is "theirs"
        /// * null if there is no such instance
        /// </summary>
        /// <param name="obj">the original object</param>
        /// <returns>the instance of the original object</returns>
        public static Object GetInstanceOf(Object obj)
        {
            var result = obj;
            if(IsTheirs(obj))
            {
                ourInstances.TryGetValue(obj, out result);
            }
            return result;
        }

        private static bool IsTheirs(Object obj)
        {
            var go = obj as GameObject;
            if(go)
            {
                return theirObjects.ContainsKey(go);
            }
            var c = obj as Component;
            if(c)
            {
                return theirObjects.ContainsKey(c.gameObject);
            }
            return false;
        }

        public static void SetAsMergeObject(GameObject go, bool active)
        {
            if(!theirObjects.ContainsKey(go))
            {
                theirObjects.Add(go, go.activeSelf);
            }
            go.SetActiveForMerging(false);
        }

        public static void SetActiveForMerging(this GameObject go, bool active)
        {
            go.SetActive(active);
            go.hideFlags = active ? HideFlags.None : HideFlags.HideAndDontSave;
        }

        public static GameObject InstantiateForMerging(GameObject go)
        {
            var copy = GameObject.Instantiate(go) as GameObject;

            bool wasActive;
            if(!theirObjects.TryGetValue(go, out wasActive))
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
            foreach(var obj in theirObjects.Keys)
            {
                Object.DestroyImmediate(obj);
            }
            theirObjects.Clear();
        }
    }
}