using UnityEngine;

namespace GitMerge
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Get a fine, readable type string. Doesn't really need to be a Component extension method.
        /// Example: UnityEngine.BoxCollider => BoxCollider
        /// </summary>
        /// <param name="c">The Component whose type we want to display</param>
        /// <returns>The well readable type string</returns>
        public static string GetPlainType(this Component c)
        {
            var s = c.GetType().ToString();
            var i = s.LastIndexOf('.');
            if(i >= 0)
            {
                s = s.Substring(i + 1);
            }
            return s;
        }
    }
}