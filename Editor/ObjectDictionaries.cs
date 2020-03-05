
namespace GitMerge
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// Dictionaries that categorize the scene's objects into our objects, their objects, and temporary
    /// copies of their objects that have been instantiated while merging.
    /// </summary>
    public static class ObjectDictionaries
    {
        // This dict holds all of "our" objects.
        // Needed for Reference handling.
        private static Dictionary<ObjectID, Object> ourObjects = new Dictionary<ObjectID, Object>();

        // This dict maps our instances of their objects.
        // Whenever we instantiate a copy of "their" new object, they're both added here.
        private static Dictionary<Object, Object> ourInstances = new Dictionary<Object, Object>();

        // This dict holds all of "their" GameObjects.
        // Needed for scene cleaning after merge.
        // <GameObject, originallyActive>
        private static Dictionary<GameObject, bool> theirObjects = new Dictionary<GameObject, bool>();

        // This dict holds all GameObjects that might or might not exist,
        // depending on the current merge state. The referenced objects are the versions that will definitely exist throughout the merge.
        // Also maps the MergeActions responsible for their existence to them.
        private static Dictionary<GameObject, MergeActionExistence> schroedingersObjects = new Dictionary<GameObject, MergeActionExistence>();


        public static void AddToOurObjects(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                SetAsOurObject(obj);
            }
        }

        public static void AddToTheirObjects(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                SetAsTheirs(obj, false);
            }
        }


        public static void SetAsOurObject(GameObject go)
        {
            AddOurObject(go);
            foreach (var c in go.GetComponents<Component>())
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
            if (o == null)
                return;

            ourObjects.Add(ObjectID.GetFor(o), o);
        }

        public static void RemoveOurObject(GameObject go)
        {
            foreach (var c in go.GetComponents<Component>())
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
            if (o == null)
                return;

            ourObjects.Remove(ObjectID.GetFor(o));
        }

        public static Object GetOurObject(ObjectID id)
        {
            Object result = null;
            ourObjects.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Returns:
        /// * the given object if it is "ours"
        /// * "our" counterpart of obj if it is "theirs"
        /// * null if the object is deleted for some reason
        /// The returned object can be an instance of "their" object temporarily added for the merge
        /// </summary>
        /// <param name="obj">the original object</param>
        /// <returns>the counterpart of the object in "our" version</returns>
        public static Object GetOurCounterpartFor(Object obj)
        {
            var result = obj;
            if (IsTheirs(obj))
            {
                result = GetOurObject(ObjectID.GetFor(obj));
                if (!result)
                {
                    result = GetOurInstanceOfCopy(obj);
                }
            }
            return result;
        }

        public static void Clear()
        {
            ourObjects.Clear();
            theirObjects.Clear();
            ourInstances.Clear();
            schroedingersObjects.Clear();
        }

        /// <summary>
        /// Mark o as an instance of theirs
        /// </summary>
        public static void SetAsCopy(GameObject o, GameObject theirs)
        {
            ourInstances.Add(theirs, o);
            var instanceComponents = o.GetComponents<Component>();
            var theirComponents = theirs.GetComponents<Component>();
            for (int i = 0; i < instanceComponents.Length; ++i)
            {
                SetAsCopy(instanceComponents[i], theirComponents[i]);
            }
        }

        public static void SetAsCopy(Component c, Component theirs)
        {
            if (c == null)
                return;

            ourInstances.Add(theirs, c);
        }

        public static void RemoveCopyOf(GameObject theirs)
        {
            ourInstances.Remove(theirs);
            foreach (var c in theirs.GetComponents<Component>())
            {
                if (c != null)
                {
                    ourInstances.Remove(c);
                }
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
        public static Object GetOurInstanceOfCopy(Object obj)
        {
            var result = obj;
            if (IsTheirs(obj))
            {
                ourInstances.TryGetValue(obj, out result);
            }
            return result;
        }

        private static bool IsTheirs(Object obj)
        {
            var go = obj as GameObject;
            if (go)
            {
                return theirObjects.ContainsKey(go);
            }
            var c = obj as Component;
            if (c)
            {
                return theirObjects.ContainsKey(c.gameObject);
            }
            return false;
        }

        public static void SetAsTheirs(GameObject go, bool active)
        {
            if (!theirObjects.ContainsKey(go))
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

            // Destroy children.
            foreach (Transform t in copy.GetComponent<Transform>())
            {
                Object.DestroyImmediate(t.gameObject);
            }

            bool wasActive;
            if (!theirObjects.TryGetValue(go, out wasActive))
            {
                wasActive = go.activeSelf;
            }

            // Apply some special properties of the GameObject.
            copy.SetActive(wasActive);
            copy.hideFlags = HideFlags.None;
            copy.name = go.name;
            copy.GetComponent<Transform>().parent = GetOurCounterpartFor(go.GetComponent<Transform>().parent) as Transform;

            return copy;
        }

        public static void DestroyTheirObjects()
        {
            foreach (var obj in theirObjects.Keys)
            {
                if (obj != null && obj.transform.parent == null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            theirObjects.Clear();
        }

        public static void AddToSchroedingersObjects(GameObject go, MergeActionExistence mergeAction)
        {
            schroedingersObjects.Add(go, mergeAction);
        }

        public static void EnsureExistence(GameObject go)
        {
            schroedingersObjects[go].EnsureExistence();
        }
    }
}