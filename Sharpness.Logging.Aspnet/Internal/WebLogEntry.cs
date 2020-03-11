using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sharpness.Logging.Aspnet.Internal
{
    internal class WebLogEntry
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public LogLevel Level { get; set; }
        public EventId EventId { get; set; }
        public List<object> Scopes { get; set; }
        public Exception Exception { get; set; }
    }
}
