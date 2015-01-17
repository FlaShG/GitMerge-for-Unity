using UnityEngine;
using System.Collections.Generic;

namespace GitMerge
{
    /// <summary>
    /// Dictionaries that categorize the scene's objects into our objects, their objects, and temporary
    /// copies of their objects that have been instantiated while merging.
    /// </summary>
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


        public static void SetAsOurObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                SetAsOurObject(obj);
            }
        }

        public static void SetAsTheirObjects(List<GameObject> objects)
        {
            foreach(var obj in objects)
            {
                SetAsTheirs(obj, false);
            }
        }


        public static void SetAsOurObject(GameObject go)
        {
            AddOurObject(go);
            foreach(var c in go.GetComponents<Component>())
            {
                AddOurObject(c);
            }
        }

        public static void SetAsOurObject(Component c)
        {
            AddOurObject(c);
        }

        private static void AddOurObject(Object o)
        {
            ourObjects.Add(ObjectIDFinder.GetIdentifierFor(o), o);
        }

        public static void RemoveOurObject(GameObject go)
        {
            foreach(var c in go.GetComponents<Component>())
            {
                RemoveOurSingleObject(c);
            }
            RemoveOurSingleObject(go);
        }

        public static void RemoveOurObject(Component c)
        {
            RemoveOurSingleObject(c);
        }

        private static void RemoveOurSingleObject(Object o)
        {
            ourObjects.Remove(ObjectIDFinder.GetIdentifierFor(o));
        }

        public static Object GetOurObject(int id)
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
        public static void SetAsCopy(GameObject o, GameObject theirs)
        {
            ourInstances.Add(theirs, o);
            var instanceComponents = o.GetComponents<Component>();
            var theirComponents = theirs.GetComponents<Component>();
            for(int i = 0; i < instanceComponents.Length; ++i)
            {
                SetAsCopy(instanceComponents[i], theirComponents[i]);
            }
        }

        public static void SetAsCopy(Component c, Component theirs)
        {
            ourInstances.Add(theirs, c);
        }

        public static void RemoveCopyOf(GameObject theirs)
        {
            ourInstances.Remove(theirs);
            foreach(var c in theirs.GetComponents<Component>())
            {
                ourInstances.Remove(c);
            }
        }

        public static void RemoveCopyOf(Component theirs)
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
        public static Object GetOurVersionOf(Object obj)
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

        public static void SetAsTheirs(GameObject go, bool active)
        {
            if(!theirObjects.ContainsKey(go))
            {
                theirObjects.Add(go, go.activeSelf);
            }
            go.SetActiveForMerging(false);
        }

        /// <summary>
        /// Copy an object that has been disabled and hidden for merging into the scene,
        /// enable and unhide the copy.
        /// </summary>
        /// <param name="go">The original GameObject.</param>
        /// <returns>The copy GameObject.</returns>
        public static GameObject InstantiateFromMerging(GameObject go)
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

        public static void DestroyTheirObjects()
        {
            foreach(var obj in theirObjects.Keys)
            {
                Object.DestroyImmediate(obj);
            }
            theirObjects.Clear();
        }
    }
}