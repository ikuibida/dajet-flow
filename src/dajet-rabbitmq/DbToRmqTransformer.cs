using DaJet.Flow;
using DaJet.Flow.Contracts.V1;
using System.Text.Json;

namespace DaJet.RabbitMQ
{
    public sealed class DbToRmqTransformer : Transformer<OutgoingMessage, Message>
    {
        protected override void _Transform(in OutgoingMessage input, out Message output)
        {
            output = new Message();

            Dictionary<string, string> headers = JsonSerializer.Deserialize<Dictionary<string, string>>(input.Headers);

            if (headers != null && headers.TryGetValue("Sender", out string sender))
            {
                output.AppId = sender;
            }

            // TODO: configure headers

            output.Type = input.MessageType;
            output.Body = input.MessageBody;
        }
    }
}