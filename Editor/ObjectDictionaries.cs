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

        //This set holds all of "their" objects
        //Needed to determine if we should look up "our" instance of it
        private static HashSet<GameObject> theirObjects = new HashSet<GameObject>();

        //This dict maps our instances of their objects
        //Whenever we instantiate a copy of "their" new object, they're both added here
        private static Dictionary<Object, Object> ourInstances = new Dictionary<Object, Object>();

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
                return theirObjects.Contains(go);
            }
            var c = obj as Component;
            if(c)
            {
                return theirObjects.Contains(c.gameObject);
            }
            return false;
        }

        public static void SetAsTheirs(GameObject obj)
        {
            theirObjects.Add(obj);
        }
    }
}