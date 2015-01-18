
namespace GitMerge
{
    public class VCSException : System.Exception
    {
        public override string Message
        {
            get { return "Could not find the VCS executable. Please enter the path to your VCS in the settings."; }
        }
    }
}