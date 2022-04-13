using DaJet.Flow;
using DaJet.Flow.Contracts;
using System.Text.Json;

namespace DaJet.RabbitMQ
{
    public sealed class DbToRmqTransformer : Transformer<OutgoingMessage, Message>
    {
        protected override void _Transform(in OutgoingMessage input, out Message output)
        {
            output = new Message();

            //Dictionary<string, string> headers = null;
            //try
            //{
            //    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(input.Headers);
            //}
            //catch { /* do nothing */ }

            //if (headers != null && headers.TryGetValue("Sender", out string sender))
            //{
            //    output.AppId = sender;
            //}

            //if (headers != null)
            //{
            //    foreach (var header in headers)
            //    {
            //        output.Headers.TryAdd(header.Key, header.Value);
            //    }
            //}

            _ = output.Headers.TryAdd("Headers", input.Headers); // FIXME
            output.AppId = input.Sender;
            output.Type = input.MessageType;
            output.Body = input.MessageBody;
        }
    }
}