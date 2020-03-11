using System;

namespace Sharpness.Logging.Common
{
    public class LogEntry
    {
        public LogEntry(DateTime date)
        {
            Date = date.ToString("o");
        }

        public string Date { get; }
        public int Level { get; set; }
        public int EventId { get; set; }
        public string Scopes { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string EventName { get; set; }
        public string Exception { get; set; }
    }
}
