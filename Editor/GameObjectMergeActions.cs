
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// One instance of this class represents one GameObject with relevance to the merge process.
    /// Holds all MergeActions that can be applied to the GameObject or its Components.
    /// Is considered as "merged" when all its MergeActions are "merged".
    /// </summary>
    public class GameObjectMergeActions
    {
        /// <summary>
        /// Reference to "our" version of the GameObject.
        /// </summary>
        public GameObject ours { private set; get; }
        /// <summary>
        /// Reference to "their" versoin of the GameObject.
        /// </summary>
        public GameObject theirs { private set; get; }

        public string name { private set; get; }
        public bool merged { private set; get; }
        public bool hasActions
        {
            get { return actions.Count > 0; }
        }
        /// <summary>
        /// All actions available for solving specific conflicts on the GameObject.
        /// </summary>
        private List<MergeAction> actions;


        public GameObjectMergeActions(GameObject ours, GameObject theirs)
        {
            actions = new List<MergeAction>();

            this.ours = ours;
            this.theirs = theirs;
            GenerateName();

            if (theirs && !ours)
            {
                actions.Add(new MergeActionNewGameObject(ours, theirs));
            }
            if (ours && !theirs)
            {
                actions.Add(new MergeActionDeleteGameObject(ours, theirs));
            }
            if (ours && theirs)
            {
                FindPropertyDifferences();
                FindComponentDifferences();
            }

            // Some Actions have a default and are merged from the beginning.
            // If all the others did was to add GameObjects, we're done with merging from the start.
            CheckIfMerged();
        }

        /// <summary>
        /// Generate a title for this object
        /// </summary>
        private void GenerateName()
        {
            name = "";
            if (ours)
            {
                name = "Your[" + GetPath(ours) + "]";
            }
            if (theirs)
            {
                if (ours)
                {
                    name += " vs. ";
                }
                name += "Their[" + GetPath(theirs) + "]";
            }
        }

        /// <summary>
        /// Finds the differences between properties of the two GameObjects.
        /// That means the name, layer, tag... everything that's not part of a Component. Also, the parent.
        /// </summary>
        private void FindPropertyDifferences()
        {
            CheckForDifferentParents();
            FindPropertyDifferences(ours, theirs);
        }

        /// <summary>
        /// Since parenting is quite special, here's some dedicated handling.
        /// </summary>
        private void CheckForDifferentParents()
        {
            var transform = ours.GetComponent<Transform>();
            var ourParent = transform.parent;
            var theirParent = theirs.GetComponent<Transform>().parent;
            if (!ObjectID.GetFor(ourParent).Equals(ObjectID.GetFor(theirParent)))
            {
                actions.Add(new MergeActionParenting(transform, ourParent, theirParent));
            }
        }

        /// <summary>
        /// Check for Components that one of the sides doesn't have, and/or for defferent values
        /// on Components.
        /// </summary>
        private void FindComponentDifferences()
        {
            var ourComponents = ours.GetComponents<Component>();
            var theirComponents = theirs.GetComponents<Component>();

            // Map "their" Components to their respective ids.
            var theirDict = new Dictionary<ObjectID, Component>();
            foreach (var theirComponent in theirComponents)
            {
                // Ignore null components.
                if (theirComponent != null)
                {
                    theirDict.Add(ObjectID.GetFor(theirComponent), theirComponent);
                }
            }

            foreach (var ourComponent in ourComponents)
            {
                // Ignore null components.
                if (ourComponent == null) continue;

                // Try to find "their" equivalent to our Components.
                var id = ObjectID.GetFor(ourComponent);
                Component theirComponent;
                theirDict.TryGetValue(id, out theirComponent);

                if (theirComponent) // Both Components exist.
                {
                    FindPropertyDifferences(ourComponent, theirComponent);
                    // Remove "their" Component from the dict to only keep those new to us.
                    theirDict.Remove(id);
                }
                else
                {
                    // Component doesn't exist in their version, offer a deletion.
                    actions.Add(new MergeActionDeleteComponent(ours, ourComponent));
                }
            }

            // Everything left in the dict is a...
            foreach (var theirComponent in theirDict.Values)
            {
                // ...new Component from them.
                actions.Add(new MergeActionNewComponent(ours, theirComponent));
            }
        }

        /// <summary>
        /// Find all the values different in "our" and "their" version of a component.
        /// </summary>
        private void FindPropertyDifferences(Object ourObject, Object theirObject)
        {
            var ourSerialized = new SerializedObject(ourObject);
            var theirSerialized = new SerializedObject(theirObject);

            var ourProperty = ourSerialized.GetIterator();
            if (ourProperty.NextVisible(true))
            {
                var theirProperty = theirSerialized.GetIterator();
                theirProperty.NextVisible(true);
                while (ourProperty.NextVisible(false))
                {
                    theirProperty.NextVisible(false);

                    if (ourObject is GameObject)
                    {
                        if (MergeManager.isMergingPrefab)
                        {
                            // If merging a prefab, ignore the gameobject name.
                            if (ourProperty.GetPlainName() == "Name")
                            {
                                continue;
                            }
                        }
                    }

                    if (DifferentValues(ourProperty, theirProperty))
                    {
                        // We found a difference, accordingly add a MergeAction.
                        actions.Add(new MergeActionChangeValues(ours, ourProperty.Copy(), theirProperty.Copy()));
                    }
                }
            }
        }

        /// <summary>
        /// Returns true when the two properties have different values, false otherwise.
        /// </summary>
        private static bool DifferentValues(SerializedProperty ourProperty, SerializedProperty theirProperty)
        {
            if (!ourProperty.IsRealArray())
            {
                // Regular single-value property.
                return DifferentValuesFlat(ourProperty, theirProperty);
            }
            else
            {
                // Array property.
                if (ourProperty.arraySize != theirProperty.arraySize)
                {
                    return true;
                }

                var op = ourProperty.Copy();
                var tp = theirProperty.Copy();

                op.Next(true);
                op.Next(true);
                tp.Next(true);
                tp.Next(true);

                for (int i = 0; i < ourProperty.arraySize; ++i)
                {
                    op.Next(false);
                    tp.Next(false);

                    if (DifferentValuesFlat(op, tp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool DifferentValuesFlat(SerializedProperty ourProperty, SerializedProperty theirProperty)
        {
            var our = ourProperty.GetValue();
            var their = theirProperty.GetValue();

            if (ourProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (our != null && their != null)
                {
                    our = ObjectID.GetFor(our as Object);
                    their = ObjectID.GetFor(their as Object);
                }
            }

            return !object.Equals(our, their);
        }

        /// <summary>
        /// Get the path of a GameObject in the hierarchy.
        /// </summary>
        private static string GetPath(GameObject g)
        {
            var t = g.transform;
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }

        private void CheckIfMerged()
        {
            merged = actions.TrueForAll(action => action.merged);
        }

        /// <summary>
        /// Use "our" version for all conflicts.
        /// This is used on all GameObjectMergeActions objects when the merge is aborted.
        /// </summary>
        public void UseOurs()
        {
            foreach (var action in actions)
            {
                action.UseOurs();
            }
            merged = true;
        }

        /// <summary>
        /// Use "their" version for all conflicts.
        /// </summary>
        public void UseTheirs()
        {
            foreach (var action in actions)
            {
                action.UseTheirs();
            }
            merged = true;
        }

        //If the foldout is open
        private bool open;
        public void OnGUI()
        {
            if (open)
            {
                GUI.backgroundColor = new Color(0, 0, 0, .8f);
            }
            else
            {
                GUI.backgroundColor = merged ? new Color(0, .5f, 0, .8f) : new Color(.5f, 0, 0, .8f);
            }
            GUILayout.BeginVertical(Resources.styles.mergeActions);
            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal();
            open = EditorGUILayout.Foldout(open, new GUIContent(name));

            if (ours && GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                // Highlight the instance of the prefab, not the prefab itself.
                // Otherwise, "ours".
                var objectToHighlight = MergeManager.isMergingPrefab ? MergeManagerPrefab.ourPrefabInstance : ours;
                objectToHighlight.Highlight();
            }
            GUILayout.EndHorizontal();

            if (open)
            {
                // Display all merge actions.
                foreach (var action in actions)
                {
                    if (action.OnGUIMerge())
                    {
                        CheckIfMerged();
                    }
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Use ours >>>", EditorStyles.miniButton))
                {
                    UseOurs();
                }

                if (GUILayout.Button("<<< Use theirs", EditorStyles.miniButton))
                {
                    UseTheirs();
                }
                GUILayout.EndHorizontal();
            }

            // If "ours" is null, the GameObject doesn't exist in one of the versions.
            // Try to get a reference if the object exists in the current merging state.
            // If it exists, the new/gelete MergeAction will have a reference.
            if (!ours)
            {
                foreach (var action in actions)
                {
                    ours = action.ours;
                }
            }

            GUILayout.EndVertical();

            GUI.backgroundColor = Color.white;
        }
    }
}