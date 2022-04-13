namespace DaJet.Flow
{
    public interface IMessage
    {
        //ReadOnlyMemory<byte> Headers { get; set; }
        //ReadOnlyMemory<byte> Payload { get; set; }

        //private byte[]? _buffer; // message body buffer
        //private ReadOnlyMemory<byte> GetMessageBody(in Message message)
        //{
        //    int bufferSize = message.Body.Length * 2; // char == 2 bytes

        //    if (_buffer == null)
        //    {
        //        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        //    }
        //    else if (_buffer.Length < bufferSize)
        //    {
        //        ArrayPool<byte>.Shared.Return(_buffer);
        //        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        //    }

        //    int encoded = Encoding.UTF8.GetBytes(message.Body, 0, message.Body.Length, _buffer, 0);

        //    ReadOnlyMemory<byte> messageBody = new ReadOnlyMemory<byte>(_buffer, 0, encoded);

        //    return messageBody;
        //}

        // Dispose byte buffer !!!
        //if (_buffer != null)
        //    {
        //        ArrayPool<byte>.Shared.Return(_buffer);
        //    }
    }
}