using DaJet.Flow;
using DaJet.Flow.Contracts;
using System.Text;

namespace DaJet.RabbitMQ
{
    public sealed class RmqToDbTransformer : Transformer<Message, IncomingMessage>
    {
        protected override void _Transform(in Message input, out IncomingMessage output)
        {
            output = new IncomingMessage()
            {
                Uuid = Guid.NewGuid(),
                DateTimeStamp = DateTime.Now,
                MessageNumber = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond,
                ErrorCount = 0,
                ErrorDescription = String.Empty,
                Sender = input.AppId,
                MessageType = input.Type,
                MessageBody = input.Body
            };

            if (input.Headers.TryGetValue("Headers", out object value))
            {
                if (value is byte[] headers)
                {
                    output.Headers = Encoding.UTF8.GetString(headers);
                }
            }
        }
    }
}