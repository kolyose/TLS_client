using System;

public class ServerEventArgs:EventArgs
{
    public readonly BaseServerEvent Data;
    public ServerEventArgs(BaseServerEvent data)
    {
        Data = data;
    }
}

public interface IConnectionService
{
    event EventHandler<ServerEventArgs> ServerEvent;
    event EventHandler<EventArgs> Connected;
    event EventHandler<EventArgs> Disconnected;

    void Connect(string host, int port);
    void Disconnect();
    void Reset();
    void SendMessage(OutcomeMessage message);
}