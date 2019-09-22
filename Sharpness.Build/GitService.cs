using System.Linq;

namespace Sharpness.Build
{
    public class GitService
    {
        readonly CommandService _command;

        string _branch;
        string _commit;
        string _message;

        public GitService(CommandService command)
        {
            _command = command;
        }

        public string Branch
        {
            get
            {
                if (string.IsNullOrEmpty(_branch))
                {
                    _branch = _command
                        .Read("git symbolic-ref --short HEAD")
                        .FirstOrDefault();
                }

                return _branch;
            }
        }

        public string Commit
        {
            get
            {
                if (string.IsNullOrEmpty(_commit))
                {
                    _commit = _command
                        .Read("git rev-parse HEAD")
                        .FirstOrDefault();
                }

                return _commit;
            }
        }

        public string Message
        {
            get
            {
                if (string.IsNullOrEmpty(_message))
                {
                    _message = _command
                        .Read("git show-branch --no-name HEAD")
                        .FirstOrDefault();
                }

                return _message;
            }
        }
    }
}
