using System.Linq;

namespace Sharpness.Build
{
    public class GitService
    {
        readonly CommandService _command;

        string _gitBranch;
        string _gitCommit;
        string _gitMessage;

        public GitService(CommandService command)
        {
            _command = command;
        }

        public string GitBranch
        {
            get
            {
                if (string.IsNullOrEmpty(_gitBranch))
                {
                    _gitBranch = _command
                        .Read("git symbolic-ref --short HEAD")
                        .FirstOrDefault();
                }

                return _gitBranch;
            }
        }

        public string GitCommit
        {
            get
            {
                if (string.IsNullOrEmpty(_gitCommit))
                {
                    _gitCommit = _command
                        .Read("git rev-parse HEAD")
                        .FirstOrDefault();
                }

                return _gitCommit;
            }
        }

        public string GitMessage
        {
            get
            {
                if (string.IsNullOrEmpty(_gitMessage))
                {
                    _gitMessage = _command
                        .Read("git show-branch --no-name HEAD")
                        .FirstOrDefault();
                }

                return _gitMessage;
            }
        }
    }
}
