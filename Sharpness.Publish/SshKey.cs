using System.IO;
using Renci.SshNet;

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
}
