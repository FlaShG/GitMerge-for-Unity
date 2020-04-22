// #define DEBUG_IDS

namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Struct representing a GameObject ID.
    /// Similar to GlobalObjectID, but used for robustness against changes to Unity's API.
    /// </summary>
    public struct ObjectID
    {        
        public readonly ulong id;
        public readonly ulong prefabId;

        public ObjectID(ulong id, ulong prefabId)
        {
            this.id = id;
            this.prefabId = prefabId;
        }
            
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (ObjectID)obj;
                
            return id == other.id && prefabId == other.prefabId;
        }
            
        public override int GetHashCode()
        {
            return unchecked(id.GetHashCode() + prefabId.GetHashCode());
        }

        public override string ToString()
        {
            return "[" + id + (prefabId != 0 ? "/" + prefabId : "") + "]";
        }

        public static ObjectID GetFor(Object o)
        {
            var goid = GlobalObjectId.GetGlobalObjectIdSlow(o);
            var id = goid.targetObjectId;
            var prefabId = goid.targetPrefabId;
            return new ObjectID(id, prefabId);
        }

#if DEBUG_IDS
        [MenuItem("Window/GitMerge Test ObjectID")]
        private static void Test()
        {
            // Debug.Log(GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeGameObject));
            Debug.Log(GetFor(Selection.activeGameObject));
        }
#endif
    }
}
