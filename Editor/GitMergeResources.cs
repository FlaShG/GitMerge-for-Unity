using UnityEngine;

public class GitMergeResources : ScriptableObject
{
    public static GitMergeResources styles { private set; get; }
    private static Texture2D _logo;
    public static Texture2D logo
    {
        get
        {
            if(!_logo)
            {
                _logo = Resources.Load<Texture2D>("GitMergeLogo");
            }
            return _logo;
        }
    }

    static GitMergeResources()
    {
        styles = Resources.Load<GitMergeResources>("GitMergeStyles");
    }

    public GUIStyle mergeActions;
    public GUIStyle mergeAction;

    public static void DrawLogo()
    {
        GUI.DrawTexture(new Rect(5,5,logo.width,logo.height), logo);
        GUILayout.Space(logo.height + 15);
    }
}
