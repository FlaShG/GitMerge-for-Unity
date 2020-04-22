
namespace GitMerge
{
    public class VCSException : System.Exception
    {
        public VCSException(string message = "Could not find the VCS executable. Please enter the path to your VCS in the settings.") : base(message)
        {
        }
    }
}