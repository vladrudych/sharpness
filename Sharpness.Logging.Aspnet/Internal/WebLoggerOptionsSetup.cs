using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sharpness.Logging.Aspnet.Internal
{
    internal class WebLoggerOptionsSetup
        : ConfigureFromConfigurationOptions<WebLoggerOptions>
    {
        public WebLoggerOptionsSetup
        (
            ILoggerProviderConfiguration<WebLoggerProvider> providerConfig
        )
            : base(providerConfig.Configuration)
        {
        }
    }
}
