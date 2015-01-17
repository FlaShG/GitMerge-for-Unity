using UnityEngine;

namespace GitMerge
{
    public class Resources : ScriptableObject
    {
        public static Resources styles { private set; get; }
        private static Texture2D _logo;
        public static Texture2D logo
        {
            get
            {
                if(!_logo)
                {
                    _logo = UnityEngine.Resources.Load<Texture2D>("GitMergeLogo");
                }
                return _logo;
            }
        }

        static Resources()
        {
            styles = UnityEngine.Resources.Load<Resources>("GitMergeStyles");
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