using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharpness.Build
{
    public class CommandService
    {
        List<string> Run(string command, string workingDirectory, bool readOutput)
        {
            List<string> lines = null;
            List<string> errors = null;

            Console.WriteLine(command);

            using (Process process = new Process())
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c " + command.Replace("'", "\"");
                }
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = "-c " + '"' + command + '"';
                }

                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }

                process.OutputDataReceived += (s, e) =>
                {
                    Console.WriteLine(e.Data);

                    if (readOutput && !string.IsNullOrEmpty(e.Data?.Trim()))
                    {
                        if (lines == null)
                        {
                            lines = new List<string>();
                        }

                        lines.Add(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    Console.WriteLine(e.Data);

                    if (!string.IsNullOrEmpty(e.Data?.Trim()))
                    {
                        if (errors == null)
                        {
                            errors = new List<string>();
                        }

                        errors.Add(e.Data);
                    }
                };

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

            }

            if (errors?.Any() ?? false)
            {
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            return lines;
        }

        public void Exec(string command, string workingDirectory = null)
        {
            Run(command, workingDirectory, false);
        }

        public List<string> Read(string command, string workingDirectory = null)
        {
            return Run(command, workingDirectory, false);
        }
    }

    public class DirectoryService
    {
        readonly CommandService _command;

        string _solutionDirectory;
        string _gitBranch;
        string _gitCommit;
        string _gitMessage;

        public DirectoryService()
        {
            _command = new CommandService();
        }

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

    public class SlackService
    {
        public string Name { get; set; } = "publish-bot";
        public string Icon { get; set; } = "rocket";

        public void SendMessage(string webhookUrl, string text)
        {
            try
            {
                Console.WriteLine("Sending slack message...");
                HttpWebRequest request = WebRequest.CreateHttp(webhookUrl);
                request.Method = WebRequestMethods.Http.Post;
                using (var stream = request.GetRequestStream())
                {
                    text = text.Replace("\"", "\\\"");

                    string content
                        = $"{{ \"username\": \"{Name}\", \"icon_emoji\": \"{Icon}\", \"text\": \"{text}\"}}";

                    byte[] bytes = Encoding.UTF8.GetBytes(content);
                    stream.Write(bytes, 0, bytes.Length);
                }
                using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    Console.WriteLine($"Slack response: {reader.ReadToEnd()}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Slack error!");
                Console.WriteLine(e);
            }
        }
    }
}
