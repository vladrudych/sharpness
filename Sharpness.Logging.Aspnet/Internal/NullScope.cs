using System;

namespace Sharpness.Logging.Aspnet.Internal
{
    internal class NullScope : IDisposable
    {
        public static NullScope Instance => new NullScope();

        private NullScope() { }

        public void Dispose() { }
    }
}
