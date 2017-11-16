using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

public enum EnumMessageType
{
    AUTH=1,
    HEARTBEAT=3
}

public class ServerMessagesController:MonoBehaviour
{
    public event EventHandler<ServerEventArgs> ServerEvent;
    public IConnectionService _connectionService;

    private const int MESSAGE_HEADER_LENGTH = 3; //in bytes
    private const int HEARTBEAT_PERIOD = 5; //in seconds

    private static Queue<Action> Actions = new Queue<Action>();
    private Coroutine _sendHeartbeatCoroutine;
    private string _authToken;   

    public virtual void Update()
    {
        while (Actions.Count > 0)
            Actions.Dequeue().Invoke();
    }

    // Use this for initialization
    void Awake()
    {
        if (_connectionService == null)
            _connectionService = GetComponent<IConnectionService>();

        _connectionService.Connected += ServiceConnectedHandler;
        _connectionService.Disconnected += ServiceDisconnectedHandler;
        _connectionService.ServerEvent += ServerEventHandler;
    }

    public void Connect(string host, int port, string authToken)
    {
        _authToken = authToken;
        _connectionService.Connect(host, port);
    }

    public void Disconnect()
    {
        _connectionService.Disconnect();
    }

    public void Authenticate(string token)
    {        
        byte[] payload = Encoding.UTF8.GetBytes(token);
        OutcomeMessage authMessage = new OutcomeMessage((int)EnumMessageType.AUTH, MESSAGE_HEADER_LENGTH, payload);
        _connectionService.SendMessage(authMessage);
    }

    private void ServiceConnectedHandler(object sender, EventArgs e)
    {
        Actions.Enqueue(() =>
        {
            Authenticate(_authToken);
        });

        // passing action to the main thread
        Actions.Enqueue(() => 
        {           
            //start coroutine for heartbeat messages
            OutcomeMessage heartbeatMessage = new OutcomeMessage((int)EnumMessageType.HEARTBEAT, MESSAGE_HEADER_LENGTH);
            _sendHeartbeatCoroutine = StartCoroutine(SendMessageRepeatedly(heartbeatMessage, HEARTBEAT_PERIOD));
        });        
    }

    private IEnumerator SendMessageRepeatedly(OutcomeMessage message, float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log("HEARTBEAT");
            _connectionService.SendMessage(message);
        }        
    }

    private void ServiceDisconnectedHandler(object sender, EventArgs e)
    {       
        Actions.Enqueue(() =>
        {
            StopCoroutine(_sendHeartbeatCoroutine);
            _connectionService.Reset();
        });     
    }

    private void ServerEventHandler(object sender, ServerEventArgs e)
    {
        Actions.Enqueue(() =>
        {
            HandleServerEvent(e);
        });
    }

    private void HandleServerEvent(ServerEventArgs e)
    {
        switch (e.Data.Type)
        {
            case EnumServerEventType.RaceNewEntrant:
                {
                    RaceNewEntrantServerEvent evt = e.Data as RaceNewEntrantServerEvent;
                    Debug.Log("RaceNewEntrant => raceId: " + evt.RaceId.ToString() + " entrantId: " + evt.EntrantId.ToString());

                    //Do not forget to dispose the event after usage!!!
                    evt.Dispose();

                    break;
                }

            case EnumServerEventType.RaceFinished:
                {
                    RaceFinishedServerEvent evt = e.Data as RaceFinishedServerEvent;
                    Debug.Log("RaceFinished => raceId: " + evt.RaceId.ToString());

                    //Do not forget to dispose the event after usage!!!
                    evt.Dispose();

                    break;
                }

            default:
                break;
        }
    }
}