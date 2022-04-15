
using UnityEngine;
using System;

namespace GitMerge
{
#if CSHARP_7_3_OR_NEWER
    public readonly struct GUIBackgroundColor : IDisposable
#else
    public struct GUIBackgroundColor : IDisposable
#endif
    {
        private readonly Color previousColor;

        public GUIBackgroundColor(Color color)
        {
            previousColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
        }

        public void Dispose()
        {
            GUI.backgroundColor = previousColor;
        }
    }
}
