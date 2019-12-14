
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;

    public static class ObjectIDUtility
    {
        public static GlobalObjectId GetIdentifierFor(Object o)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(o);
        }

        public static void OverrideIdentifierFor(Object o, GlobalObjectId id)
        {
            throw new System.NotImplementedException();
        }
    }
}
