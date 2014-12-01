using UnityEngine;

namespace GitMerge
{
    public static class ComponentExtensions
    {
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