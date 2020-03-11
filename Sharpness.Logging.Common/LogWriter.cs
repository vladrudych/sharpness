using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Sharpness.Logging.Common
{
    public class LogWriter : IDisposable
    {
        private readonly HMACSHA256 _hmac;
        private readonly HubConnection _connection;

        public LogWriter(string apiUrl, string clientId, string clientSecret)
        {
            _hmac = new HMACSHA256
            {
                Key = Encoding.UTF8.GetBytes(clientSecret)
            };

            _connection = new HubConnectionBuilder()
                .WithUrl(GetConnectionUrl(apiUrl, clientId))
                .WithAutomaticReconnect()
                .Build();
        }

        private string GetConnectionUrl(string apiUrl, string clientId)
        {
            string time = DateTime.UtcNow.Ticks.ToString();

            string hash = Convert.ToBase64String(
                _hmac.ComputeHash(Encoding.UTF8.GetBytes(time))
            );

            hash = HttpUtility.UrlEncode(hash);

            return $"{apiUrl}?clientId={clientId}&hash={hash}&time={time}";
        }

        public void Dispose()
        {
            _hmac.Dispose();
            _connection.DisposeAsync();
        }

        public async Task Write(LogEntry entry)
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
            }

            await _connection.InvokeAsync("Write", entry);
        }
    }
}
