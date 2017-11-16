using UnityEngine;

using System;
using System.Text;

public enum EnumServerEventType
{
    None=0,
    RaceNewEntrant=1,
    RaceFinished=2
}

public class BaseServerEvent:IDisposable
{        
    public byte[] Payload { get; set; }
    public string Receiver { get; set; }

    protected short _type;
    public EnumServerEventType Type { get; set; }

    protected Action<BaseServerEvent> _onDisposeCallback;

    public BaseServerEvent(Action<BaseServerEvent> onDisposeCallback)
    {
        _onDisposeCallback = onDisposeCallback;
    }

    public virtual void Init()
    {
        short length = GetReceiverLength();
        StringBuilder messageData = new StringBuilder();
        Decoder decoder = Encoding.UTF8.GetDecoder();
        char[] chars = new char[decoder.GetCharCount(Payload, 2, length)];
        decoder.GetChars(Payload, 2, length, chars, 0);
        messageData.Append(chars);
        Receiver = messageData.ToString();

        _type = BitConverter.ToInt16(Payload, GetTypeStartIndex());
        Type = (EnumServerEventType)_type;
    }

    public virtual void Dispose()
    {
        Payload = null;
        Receiver = string.Empty;

        _onDisposeCallback.Invoke(this);
    }

    protected short GetReceiverLength()
    {
        return BitConverter.ToInt16(Payload, 0);
    }

    protected int GetTypeStartIndex()
    {
        return GetReceiverLength() + 2;
    }    
}
