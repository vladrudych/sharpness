using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sharpness.Logging.Aspnet.Internal
{
    internal class WebLogger : ILogger
    {
        #region Fields

        private LogLevel _logLevel;
        private readonly string _category;
        private IExternalScopeProvider _scopeProvider;
        private readonly WebLoggerProcessor _loggerProcessor;

        #endregion

        #region Constructor

        public WebLogger
        (
            string category,
            LogLevel logLevel,
            WebLoggerProcessor loggerProcessor,
            IExternalScopeProvider scopeProvider
        )
        {
            _category = category;
            _logLevel = logLevel;
            _scopeProvider = scopeProvider;
            _loggerProcessor = loggerProcessor;
        }

        #endregion

        #region Public

        public IDisposable BeginScope<TState>(TState state)
        {
            return _scopeProvider?.Push(state) ?? NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            bool enabled = logLevel != LogLevel.None
                && _logLevel != LogLevel.None
                && logLevel >= _logLevel;

            return enabled;
        }

        public void Log<TState>
        (
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter
        )
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, eventId, message, exception);
            }
        }

        #endregion

        #region Private

        private void WriteMessage(LogLevel logLevel, EventId eventId, string message, Exception exception)
        {
            WebLogEntry entry = new WebLogEntry
            {
                Level = logLevel,
                Date = DateTime.UtcNow,
                Category = _category,
                EventId = eventId,
                Exception = exception,
                Message = message
            };

            if (_scopeProvider != null)
            {
                entry.Scopes = new List<object>();
                _scopeProvider.ForEachScope(
                    (s, scopes) => scopes.Add(s),
                    entry.Scopes
                );
            }

            _loggerProcessor.EnqueueEntry(entry);
        }

        #endregion

        #region Internal

        internal void SetLogLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        internal void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        #endregion
    }
}
