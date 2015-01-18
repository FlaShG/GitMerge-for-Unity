using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GitMerge
{
    public class VCSGit : VCS
    {
        protected override string GetDefaultPath()
        {
            if(Application.platform == RuntimePlatform.WindowsEditor)
            {
                return @"C:\Program Files (x86)\Git\bin\git.exe";
            }
            return @"/usr/bin/git";
        }

        protected override string EditorPrefsKey()
        {
            return "GitMerge_git";
        }

        public override void GetTheirs(string path)
        {
            Execute("checkout --theirs \"" + path + "\"");
        }

        public override void MarkAsMerged(string path)
        {
            Execute("add \"" + path + "\"");
        }
    }
}