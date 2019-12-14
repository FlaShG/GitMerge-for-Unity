using UnityEngine;

namespace GitMerge
{
    public class Resources : ScriptableObject
    {
        private static Resources _styles;
        public static Resources styles
        {
            get
            {
                if (!_styles)
                {
                    _styles = UnityEngine.Resources.Load<Resources>("GitMergeStyles");
                }
                return _styles;
            }
        }
        private static Texture2D _logo;
        public static Texture2D logo
        {
            get
            {
                if (!_logo)
                {
                    _logo = UnityEngine.Resources.Load<Texture2D>("GitMergeLogo");
                }
                return _logo;
            }
        }

        public GUIStyle mergeActions;
        public GUIStyle mergeAction;

        public static void DrawLogo()
        {
            GUI.DrawTexture(new Rect(5, 5, logo.width, logo.height), logo);
            GUILayout.Space(logo.height + 15);
        }
    }
}