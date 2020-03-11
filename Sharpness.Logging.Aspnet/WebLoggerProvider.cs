using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sharpness.Logging.Aspnet.Internal;

namespace Sharpness.Logging.Aspnet
{
    [ProviderAlias("Web")]
    public class WebLoggerProvider : IDisposable, ILoggerProvider, ISupportExternalScope
    {

        #region Fields

        private bool _disposed;
        private LogLevel _logLevel;
        private IDisposable _optionsChangeToken;
        private IExternalScopeProvider _scopeProvider;

        private readonly WebLoggerProcessor _loggerProcessor;
        private readonly ConcurrentDictionary<string, WebLogger> _loggers;

        #endregion

        #region Constructor

        public WebLoggerProvider(WebLoggerOptions options)
        {
            _loggers = new ConcurrentDictionary<string, WebLogger>();
            _loggerProcessor = new WebLoggerProcessor();

            UpdateOptions(options);
        }

        public WebLoggerProvider(IOptionsMonitor<WebLoggerOptions> optionsMonitor)
            : this(optionsMonitor.CurrentValue)
        {
            _optionsChangeToken = optionsMonitor.OnChange(UpdateOptions);
        }

        #endregion

        #region Destructor

        ~WebLoggerProvider()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_optionsChangeToken != null)
                {
                    _optionsChangeToken.Dispose();
                    _optionsChangeToken = null;
                }

                _loggerProcessor.Dispose();

                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Private

        private void UpdateOptions(WebLoggerOptions options)
        {
            _logLevel = options?.LogLevel ?? LogLevel.Information;

            foreach (var logger in _loggers.Values)
            {
                logger.SetLogLevel(_logLevel);
            }

            _loggerProcessor.UpdateOptions(options?.ApiUrl, options?.ClientId, options?.ClientSecret);
        }

        private IExternalScopeProvider GetScopeProvider()
        {
            return _scopeProvider
                    ?? (_scopeProvider = new LoggerExternalScopeProvider());
        }

        #endregion

        #region Public

        public ILogger CreateLogger(string сategory)
        {
            return _loggers.GetOrAdd(сategory, category => new WebLogger(
                category,
                _logLevel,
                _loggerProcessor,
                GetScopeProvider()
            ));
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (var logger in _loggers.Values)
            {
                logger.SetScopeProvider(GetScopeProvider());
            }
        }

        #endregion

    }
}
