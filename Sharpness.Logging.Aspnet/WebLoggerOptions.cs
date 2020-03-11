using Microsoft.Extensions.Logging;

namespace Sharpness.Logging.Aspnet
{
    public class WebLoggerOptions
    {
        public string ApiUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
