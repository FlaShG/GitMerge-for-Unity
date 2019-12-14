
namespace GitMerge
{
    using UnityEngine;

    public class VCSGit : VCS
    {
        protected override string GetDefaultPath()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return @"C:\Program Files (x86)\Git\bin\git.exe";
            }
            return @"/usr/bin/git";
        }

        protected override string EditorPrefsKey()
        {
            return "GitMerge_git";
        }

        public override void CheckoutOurs(string path)
        {
            GetAbsoluteFolderPathAndFilename(path, out var absoluteFolderPath, out var filename);
            Execute("checkout --ours \"" + filename + "\"", absoluteFolderPath);
        }

        public override void CheckoutTheirs(string path)
        {
            GetAbsoluteFolderPathAndFilename(path, out var absoluteFolderPath, out var filename);
            Execute("checkout --theirs \"" + filename + "\"", absoluteFolderPath);
        }

        public override void MarkAsMerged(string path)
        {
            GetAbsoluteFolderPathAndFilename(path, out var absoluteFolderPath, out var filename);
            Execute("add \"" + filename + "\"", absoluteFolderPath);
        }
    }
}