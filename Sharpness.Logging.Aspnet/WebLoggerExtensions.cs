using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sharpness.Logging.Aspnet.Internal;

namespace Sharpness.Logging.Aspnet
{
    public static class WebLoggerExtensions
    {
        public static ILoggingBuilder AddWebLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor
                    .Singleton<ILoggerProvider, WebLoggerProvider>()
            );

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton
                <
                    IConfigureOptions<WebLoggerOptions>,
                    WebLoggerOptionsSetup
                >()
            );

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton
                <
                    IOptionsChangeTokenSource<WebLoggerOptions>,
                    LoggerProviderOptionsChangeTokenSource
                    <
                        WebLoggerOptions,
                        WebLoggerProvider
                    >
                >()
            );

            return builder;
        }

        public static ILoggingBuilder AddWebLogger(
            this ILoggingBuilder builder,
            Action<WebLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddWebLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
