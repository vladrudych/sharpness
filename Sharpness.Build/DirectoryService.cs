using System.IO;
using System.Linq;

namespace Sharpness.Build
{
    public class DirectoryService
    {
        string _solutionDirectory;

        public string SolutionDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_solutionDirectory))
                {
                    string directory = Directory.GetCurrentDirectory();

                    while (!string.IsNullOrEmpty(directory))
                    {
                        if (Directory.GetFiles(directory, "*.sln").Any())
                        {
                            _solutionDirectory = directory;
                            break;
                        }
                        directory = Path.GetDirectoryName(directory);
                    }
                }

                return _solutionDirectory;
            }
        }
    }
}
