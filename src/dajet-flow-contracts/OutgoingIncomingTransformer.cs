using DaJet.Flow.Contracts;
using System.Text.Json;

namespace DaJet.Flow.Transformers
{
    public sealed class OutgoingIncomingTransformer : Transformer<OutgoingMessage, IncomingMessage>
    {
        protected override void _Transform(in OutgoingMessage input, out IncomingMessage output)
        {
            output = new IncomingMessage()
            {
                Uuid = Guid.NewGuid(),
                DateTimeStamp = DateTime.Now,
                MessageNumber = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond,
                Sender = input.Sender,
                Headers = input.Headers,
                MessageType = input.MessageType,
                MessageBody = input.MessageBody
            };

            Dictionary<string, string>? headers;

            try
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(input.Headers);
            }
            catch (Exception error)
            {
                throw new FormatException($"Message headers format exception. Message number: {{{input.MessageNumber}}}. Error message: {error.Message}");
            }

            if (headers is not null && headers.TryGetValue("Sender", out string? sender) && !string.IsNullOrEmpty(sender))
            {
                output.Sender = sender;
            }
        }
    }
}