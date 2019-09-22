using System;
using System.IO;
using System.Net;
using System.Text;

namespace Sharpness.Build
{
    public class SlackService
    {
        public string Name { get; set; } = "pipelines-bot";
        public string Icon { get; set; } = "robot_face";

        public void SendMessage(string webhookUrl, string text)
        {
            try
            {
                Console.WriteLine("Sending slack message...");
                HttpWebRequest request = WebRequest.CreateHttp(webhookUrl);
                request.Method = WebRequestMethods.Http.Post;
                using (var stream = request.GetRequestStream())
                {
                    text = text.Replace("\"", "\\\"");

                    string content
                        = $"{{ \"username\": \"{Name}\", \"icon_emoji\": \"{Icon}\", \"text\": \"{text}\"}}";

                    byte[] bytes = Encoding.UTF8.GetBytes(content);
                    stream.Write(bytes, 0, bytes.Length);
                }
                using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    Console.WriteLine($"Slack response: {reader.ReadToEnd()}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Slack error!");
                Console.WriteLine(e);
            }
        }

    }
}
