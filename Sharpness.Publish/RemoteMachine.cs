using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Sharpness.Build;

namespace Sharpness.Publish
{

    public class SshKey
    {
        public SshKey(byte[] data)
            : this(new MemoryStream(data)) { }

        public SshKey(byte[] data, string password)
            : this(new MemoryStream(data), password) { }

        public SshKey(Stream file)
        {
            KeyFile = new PrivateKeyFile(file);
        }

        public SshKey(Stream file, string password)
        {
            KeyFile = new PrivateKeyFile(file, password);
        }

        public SshKey(string file)
        {
            KeyFile = new PrivateKeyFile(file);
        }

        public SshKey(string file, string password)
        {
            KeyFile = new PrivateKeyFile(file, password);
        }

        internal PrivateKeyFile KeyFile { get; }
    }

    public class RemoteMachine : IDisposable
    {
        readonly ConnectionInfo _connection;

        SshClient _ssh;
        SftpClient _sftp;

        RemoteMachine(ConnectionInfo connection) { _connection = connection; }

        public RemoteMachine(string host, string user, SshKey key)
            : this(new PrivateKeyConnectionInfo(host, user, key.KeyFile)) { }

        public RemoteMachine(string host, string user, string password)
            : this(new PasswordConnectionInfo(host, user, password)) { }

        void EnsureSshCreated()
        {
            if (_ssh == null)
            {
                _ssh = new SshClient(_connection);
            }

            if (!_ssh.IsConnected)
            {
                _ssh.Connect();
            }
        }

        void EnsureSftpCreated()
        {
            if (_sftp == null)
            {
                _sftp = new SftpClient(_connection);
            }

            if (!_sftp.IsConnected)
            {
                _sftp.Connect();
            }
        }

        string RunSshCommand(string command)
        {
            EnsureSshCreated();

            var terminal = _ssh.RunCommand(command);
            return terminal.Result;
        }

        Dictionary<string, string> GetRemoteFilesNameHashDictionary(string remoteDirectory)
        {
            string[] opensslMd5Output =
                RunSshCommand($"find {remoteDirectory} -type f -exec openssl md5 {{}} \\;")
                .Split('\n');

            Dictionary<string, string> files = new Dictionary<string, string>();

            Regex regex = new Regex($"MD5\\({remoteDirectory.Replace("/", "\\/")}\\/(.+?)\\)=\\s+(\\w+)");

            foreach (string line in opensslMd5Output)
            {
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string file = match.Groups[1].Value;
                    string hash = match.Groups[2].Value;
                    files.Add(file, hash);
                }
            }

            return files;
        }

        Dictionary<string, string> GetLocalFilesNameHashDictionary(string localDirectory)
        {
            string ComputeMD5(string filePath, HashAlgorithm md5)
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }

            void FillDictionary(string directory, Dictionary<string, string> files, HashAlgorithm md5)
            {
                foreach (string filePath in Directory.GetFiles(directory))
                {
                    string hash = ComputeMD5(filePath, md5).ToLower();
                    string file = filePath
                        .Replace(localDirectory, "")
                        .TrimStart(Path.DirectorySeparatorChar);
                    files.Add(file, hash);
                }
                foreach (string childDirectory in Directory.GetDirectories(directory))
                {
                    FillDictionary(childDirectory, files, md5);
                }
            }

            using (HashAlgorithm md5 = HashAlgorithm.Create(HashAlgorithmName.MD5.Name))
            {
                Dictionary<string, string> files = new Dictionary<string, string>();

                FillDictionary(localDirectory, files, md5);

                return files;
            }
        }

        public void PublishDirectory(string localDirectory, string remoteDirectory, bool removeRemoteFiles = true)
        {
            FileInfo zip = null;

            try
            {
                zip = new FileInfo(Guid.NewGuid().ToString("N") + ".zip");

                Console.WriteLine("Computing local MD5 hashes...");
                Dictionary<string, string> localFiles = GetLocalFilesNameHashDictionary(localDirectory);
                Console.WriteLine($"Total {localFiles.Count} local files found.");

                Console.WriteLine("Computing remote MD5 hashes...");
                Dictionary<string, string> remoteFiles = GetRemoteFilesNameHashDictionary(remoteDirectory);
                Console.WriteLine($"Total {remoteFiles.Count} remote files found.");

                Console.WriteLine("Comparing local and remote files...");

                List<string> filesCreated = new List<string>();
                List<string> filesChanged = new List<string>();
                List<string> filesRemoved = new List<string>();
                List<string> filesKeeped = new List<string>();

                foreach (var file in localFiles.Keys)
                {
                    if (remoteFiles.ContainsKey(file))
                    {
                        if (remoteFiles[file] == localFiles[file])
                        {
                            filesKeeped.Add(file);
                        }
                        else
                        {
                            filesChanged.Add(file);
                        }
                    }
                    else
                    {
                        if (!file.EndsWith(".DS_Store", StringComparison.InvariantCultureIgnoreCase))
                        {
                            filesCreated.Add(file);
                        }
                    }
                }

                foreach (var file in remoteFiles.Keys)
                {
                    if (!localFiles.ContainsKey(file))
                    {
                        filesRemoved.Add(file);
                    }
                }

                Console.WriteLine($"{filesCreated.Count} files created.");
                Console.WriteLine($"{filesChanged.Count} files changed.");
                Console.WriteLine($"{filesRemoved.Count} files removed.");
                Console.WriteLine($"{filesKeeped.Count} files same.");

                bool changed = false;

                if (removeRemoteFiles && filesRemoved.Count > 0)
                {
                    Console.WriteLine("Removing obsolete remote files...");

                    string filesToRemove = string.Join(
                        " ",
                        filesRemoved.Select(f => '"' + remoteDirectory + '/' + f + '"')
                    );

                    RunSshCommand($"sudo rm {filesToRemove}");

                    changed = true;
                }

                if (filesCreated.Count > 0 || filesChanged.Count > 0)
                {
                    Console.WriteLine("Creating publish zip...");

                    using (var zipStream = zip.Create())
                    {
                        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, false))
                        {
                            foreach (var file in filesCreated.Concat(filesChanged))
                            {
                                using (var entryStream = zipArchive.CreateEntry(file, CompressionLevel.Optimal).Open())
                                {
                                    using (var fileStream = File.Open(Path.Combine(localDirectory, file), FileMode.Open))
                                    {
                                        fileStream.CopyTo(entryStream);
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine($"Copying {zip.Length / 1024d / 1024d:F3}MB to remote...");

                    EnsureSftpCreated();

                    using (var stream = zip.OpenRead())
                    {
                        _sftp.UploadFile(stream, $"{remoteDirectory}/{zip.Name}");
                    }

                    RunSshCommand($"sudo unzip -o {remoteDirectory}/{zip.Name} -d {remoteDirectory}");
                    RunSshCommand($"sudo rm {remoteDirectory}/{zip.Name}");

                    changed = true;
                }

                if (changed)
                {
                    Console.WriteLine("Successfully published!");
                }
                else
                {
                    Console.WriteLine("Nothing to publish!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Publish failed!");
                Console.WriteLine(e);
            }
            finally
            {
                if (zip?.Exists ?? false)
                {
                    zip?.Delete();
                }
            }

        }

        public void RestartService(string serviceName)
        {
            try
            {
                Console.WriteLine($"Restarting {serviceName} service...");

                string error = RunSshCommand($"sudo systemctl restart {serviceName}");

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

                Console.WriteLine("Service restarted!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Service restart failed!");
                Console.WriteLine(e);
            }
        }

        public void Dispose()
        {
            _ssh?.Dispose();
            _sftp?.Dispose();
        }
    }

}
