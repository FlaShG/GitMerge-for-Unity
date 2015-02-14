using UnityEditor;
using System.Diagnostics;
using System.ComponentModel;

namespace GitMerge
{
    /// <summary>
    /// This abstract class represents a vcs interface.
    /// It manages saving and retrieving the exe path from/to the EditorPrefs
    /// and offers a small set of actions using the vcs.
    /// </summary>
    public abstract class VCS
    {
        protected abstract string GetDefaultPath();
        protected abstract string EditorPrefsKey();

        //The two important methods
        public abstract void GetTheirs(string path);
        public abstract void MarkAsMerged(string path);

        //This one's for experimental three-way merging
        public abstract void GetBase(string path);

        public string exe()
        {
            if(EditorPrefs.HasKey(EditorPrefsKey()))
            {
                return EditorPrefs.GetString(EditorPrefsKey());
            }

            return GetDefaultPath();
        }

        public void SetPath(string path)
        {
            EditorPrefs.SetString(EditorPrefsKey(), path);
        }

        /// <summary>
        /// Executes the VCS as a subprocess.
        /// </summary>
        /// <param name="args">The parameters passed. Like "status" for "git status"</param>
        /// <returns>Whatever the call returns.</returns>
        protected string Execute(string args)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = exe();
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;

            try
            {
                process.Start();
            }
            catch(Win32Exception)
            {
                throw new VCSException();
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
}