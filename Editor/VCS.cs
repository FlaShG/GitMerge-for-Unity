
namespace GitMerge
{
    using UnityEngine;
    using UnityEditor;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.IO;

    /// <summary>
    /// This abstract class represents a vcs interface.
    /// It manages saving and retrieving the exe path from/to the EditorPrefs
    /// and offers a small set of actions using the vcs.
    /// </summary>
    public abstract class VCS
    {
        protected abstract string GetDefaultPath();
        protected abstract string EditorPrefsKey();
        
        public abstract void CheckoutOurs(string path);
        public abstract void CheckoutTheirs(string path);
        public abstract void MarkAsMerged(string path);

        public string GetExePath()
        {
            if (EditorPrefs.HasKey(EditorPrefsKey()))
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
        /// <param name="args">The parameters passed. Like "status" for "git status".</param>
        /// <returns>Whatever the call returns.</returns>
        protected string Execute(string args, string workingDirectoryPath)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = GetExePath();
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WorkingDirectory = workingDirectoryPath;
            process.StartInfo = startInfo;

            try
            {
                process.Start();
            }
            catch (Win32Exception)
            {
                throw new VCSException();
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }

        private static string GetAboluteFolderPath(string relativeFilePath)
        {
            var projectPath = Application.dataPath;
            projectPath = Directory.GetParent(projectPath).FullName;
            var fullPath = Path.Combine(projectPath, relativeFilePath);
            return Path.GetDirectoryName(fullPath);
        }

        protected static void GetAbsoluteFolderPathAndFilename(string relativeFilePath, out string absoluteFolderPath, out string filename)
        {
            absoluteFolderPath = GetAboluteFolderPath(relativeFilePath);
            filename = Path.GetFileName(relativeFilePath);
        }
    }
}