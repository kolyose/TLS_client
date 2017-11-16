using System;

public class OutcomeMessage
{
    protected uint _type;
    protected int _headerLength;
    protected int _payloadLength = 0;
    public byte[] _payload;

    public OutcomeMessage(uint type, int headerLength, byte[] payload = null)
    {
        _type = type;
        _headerLength = headerLength;
        _payload = payload;

        if (_payload != null)
            _payloadLength = _payload.Length;
    }

    public byte[] ToByteArray()
    {
        int resultLength = _headerLength + _payloadLength;
        byte[] result = new byte[resultLength];

        Buffer.BlockCopy(BitConverter.GetBytes(_type), 0, result, 0, 1);
        Buffer.BlockCopy(BitConverter.GetBytes(_payloadLength), 0, result, 1, 2);

        if (_payload != null)
            Buffer.BlockCopy(_payload, 0, result, 3, _payloadLength);

        return result;
    }

    public override string ToString()
    {
        return BitConverter.ToString(ToByteArray());
    }
}
