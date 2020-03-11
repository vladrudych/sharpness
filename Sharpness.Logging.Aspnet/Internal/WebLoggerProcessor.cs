using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Sharpness.Logging.Common;

namespace Sharpness.Logging.Aspnet.Internal
{
    internal class WebLoggerProcessor : IDisposable
    {
        private const int _exceptionSleepDuration = 100;
        private const int _maxQueuedMessages = 1024 * 4;

        private string _apiUrl;
        private string _clientId;
        private string _clientSecret;

        private Thread _thread;
        private LogWriter _writer;
        private BlockingCollection<WebLogEntry> _messageQueue;

        internal void UpdateOptions(string apiUrl, string clientId, string clientSecret)
        {
            bool changeConnection = apiUrl != _apiUrl
                || _clientSecret != clientSecret
                || _clientId != clientId;

            _apiUrl = apiUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;

            if (changeConnection)
            {
                _writer?.Dispose();
                _writer = new LogWriter(_apiUrl, _clientId, _clientSecret);
            }
        }

        public WebLoggerProcessor()
        {

            StartThread();
        }

        private void StartThread()
        {
            _thread?.Abort();
            _messageQueue?.CompleteAdding();
            _messageQueue?.Dispose();

            _messageQueue = new BlockingCollection<WebLogEntry>(_maxQueuedMessages);

            _thread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "WebLoggerProcessor.Thread"
            };
            _thread.Start();
        }

        public void Dispose()
        {
            _writer.Dispose();
            _messageQueue.CompleteAdding();
        }

        public void EnqueueEntry(WebLogEntry entry)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(entry);
                    return;
                }
                catch (InvalidOperationException e)
                {
                    WriteInternalException(e);
                }
            }

            _ = WriteMessage(entry);
        }


        private LogEntry PrepareDto(WebLogEntry entry)
        {
            var dto = new LogEntry(entry.Date)
            {
                Level = (int)entry.Level,
                Category = entry.Category,
                EventId = entry.EventId.Id,
                EventName = entry.EventId.Name,
                Message = entry.Message,
                Exception = entry.Exception?.ToString()
            };

            if (entry.Scopes != null)
            {
                dto.Scopes = string.Join(" => ", entry.Scopes);
            }

            return dto;
        }

        private async Task WriteMessage(WebLogEntry entry)
        {
            try
            {
                if (_writer == null)
                {
                    throw new Exception("Unable to write web log without connection!");
                }

                var dto = PrepareDto(entry);
                await _writer.Write(dto);
            }
            catch (Exception e)
            {
                WriteInternalException(e);
                Thread.Sleep(_exceptionSleepDuration);
            }
        }


#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        private async void ProcessLogQueue()
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            try
            {
                foreach (var message in _messageQueue.GetConsumingEnumerable())
                {
                    await WriteMessage(message);
                }

                _messageQueue.Dispose();
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch (Exception e)
                {
                    WriteInternalException(e);
                }

                if (Thread.CurrentThread == _thread)
                {
                    StartThread();
                }
            }
        }

        private void WriteInternalException(Exception e)
        {
            var message = $"{GetType().FullName}: Exception in background thread!";
            Console.WriteLine(message);
            Console.WriteLine(e);
        }

    }
}
