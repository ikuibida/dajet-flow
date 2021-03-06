using DaJet.Flow;

namespace DaJet.RabbitMQ
{
    public sealed class Message : IMessage
    {
        public string AppId { get; set; }
        public string UserId { get; set; }
        public string ClusterId { get; set; }
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public byte Priority { get; set; }
        public byte DeliveryMode { get; set; } = 2; // Non-persistent = 1; Persistent = 2
        public string ContentType { get; set; } = "application/json";
        public string ContentEncoding { get; set; } = "UTF-8";
        public string Type { get; set; }
        public string Body { get; set; }
        public string ReplyTo { get; set; }
        public string Expiration { get; set; }
        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
    }
}