using DaJet.Flow.Contracts.V1;
using System.Text.Json;

namespace DaJet.Flow.Contracts.Transformers.V1
{
    public sealed class OutgoingIncomingTransformer : Transformer<OutgoingMessage, IncomingMessage>
    {
        protected override void _Transform(in OutgoingMessage input, out IncomingMessage output)
        {
            output = new IncomingMessage()
            {
                DateTimeStamp = DateTime.Now,
                Headers = input.Headers,
                MessageType = input.MessageType,
                MessageBody = input.MessageBody
            };

            Dictionary<string, string> headers;

            try
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(input.Headers);
            }
            catch (Exception error)
            {
                throw new FormatException($"Message headers format exception. Message number: {{{input.MessageNumber}}}. Error message: {error.Message}");
            }

            if (headers is not null && headers.TryGetValue("Sender", out string sender))
            {
                output.Sender = sender;
            }
        }
    }
}