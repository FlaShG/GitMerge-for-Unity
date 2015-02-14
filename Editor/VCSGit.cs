using UnityEngine;
using System.Text.RegularExpressions;

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

        public override void GetBase(string path)
        {
            var head = GetCommitID(Execute("show head"));
            var mergeHead = GetCommitID(Execute("show merge_head"));
            var mergeBase = Execute("merge-base "+head+" "+mergeHead);

            Execute("checkout " + mergeBase + " \"" + path + "\"");
        }

        /// <summary>
        /// Filters the commit id from the string printed by "git show object"
        /// </summary>
        /// <param name="showResult">The result of a "git show object" call</param>
        /// <returns>The commit id of the object shown.</returns>
        private string GetCommitID(string showResult)
        {
            var pattern = @"(?<=(commit ))[\dabcdef]+\b";
            var match = Regex.Match(showResult, pattern);
            if(!match.Success)
            {
                throw new VCSException("Could not find commit ID.");
            }
            return match.Value;
        }
    }
}