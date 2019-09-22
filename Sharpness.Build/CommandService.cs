using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

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
            return Run(command, workingDirectory, true);
        }
    }
}
