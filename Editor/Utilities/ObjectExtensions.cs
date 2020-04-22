using UnityEngine;

namespace GitMerge
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Get a fine, readable type string. Doesn't really need to be a Component extension method.
        /// Example: UnityEngine.BoxCollider => BoxCollider
        /// </summary>
        /// <param name="o">The object whose type we want to display</param>
        /// <returns>The well readable type string</returns>
        public static string GetPlainType(this object o)
        {
            var s = o.GetType().ToString();
            var i = s.LastIndexOf('.');
            if (i >= 0)
            {
                s = s.Substring(i + 1);
            }
            return s;
        }
    }
}