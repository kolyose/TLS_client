
using UnityEngine;

using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading;

public class TLSConnectionService : MonoBehaviour, IConnectionService {

    public event EventHandler<ServerEventArgs> ServerEvent;
    public event EventHandler<EventArgs> Connected;
    public event EventHandler<EventArgs> Disconnected;

    private Socket _socket;
    private SslStream _stream;

    private const int SERVER_MESSAGE_HEADER_LENGTH = 3;
    private static readonly Queue<OutcomeMessage> _outcomeMessages = new Queue<OutcomeMessage>();
    private bool _sendingData;
    private string HOST;
    private int PORT;    

    public void Connect(string host, int port)
    {
        HOST = host;
        PORT = port;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.BeginConnect(HOST, PORT, new AsyncCallback(OnEndConnect), null);
    }

    public void Disconnect()
    {
        if (_socket != null)
            _socket.Close();       
    }

    private void OnEndConnect(IAsyncResult result)
    {
        _socket.EndConnect(result);
        _socket.NoDelay = true;
        
        InitTLS(); 
    }

    public void StartReading()
    {      
        ThreadPool.QueueUserWorkItem(ReadDataFromSocket);
    }

    public void SendMessage(OutcomeMessage message)
    {
        lock (_outcomeMessages)
        {
            _outcomeMessages.Enqueue(message);
            if (!_sendingData)
                ThreadPool.QueueUserWorkItem(SendDataToSocket);
        }
    }

    private void SendDataToSocket(object data)
    {
        if (!_stream.IsAuthenticated) return;

        OutcomeMessage message = null;
        lock (_outcomeMessages)
        {
            if (_outcomeMessages.Count > 0)
            {               
                message = _outcomeMessages.Dequeue();
                _sendingData = true;
            }
        }

        if (message != null)
        {
            _stream.Write((message as OutcomeMessage).ToByteArray());
            _stream.Flush();

            lock (_outcomeMessages)
            {
                if (_outcomeMessages.Count > 0)
                {
                    ThreadPool.QueueUserWorkItem(SendDataToSocket);
                }
                else
                {
                    _sendingData = false;
                }
            }
        }
    }
    
    private void InitTLS()
    {
        NetworkStream stream = new NetworkStream(_socket);
        try
        {
            _stream = new SslStream(stream, false, new RemoteCertificateValidationCallback(CertificateValidationCallback));
            X509Certificate2Collection certs = new X509Certificate2Collection();           
            _stream.AuthenticateAsClient(HOST, certs, SslProtocols.Tls, true);

            if (_stream.IsAuthenticated)
            {
                StartReading();
                OnConnected();
            }
        }
        catch (Exception e)
        {
            Debug.Log("Exception: " + e.Message);
        }
    }
    
    private void ReadingThreadDoWork(object sender, DoWorkEventArgs e)
    {
        ReadDataFromSocket();
        OnDisconnected();
    }

    private void ReadDataFromSocket(object data=null)
    {
        bool wasParsingSuccessful;
        do
        {
            wasParsingSuccessful = ParseEvent();
        }
        while (wasParsingSuccessful);
    }

    private bool ParseEvent()
    {
        byte[] headerBytes = ReadBytes(SERVER_MESSAGE_HEADER_LENGTH);
        //Debug.Log("headerBytes: " + BitConverter.ToString(headerBytes));
     
        if (headerBytes == null)
            return false;

        int payloadLength = GetPayloadLength(headerBytes);
        //Debug.Log("payloadLength: " + payloadLength.ToString());
        byte[] payloadBytes = ReadBytes(payloadLength);
        //Debug.Log("payloadBytes: " + BitConverter.ToString(payloadBytes));

        if (payloadBytes == null)
            return false;

        OnServerEvent(new ServerEventArgs(ServerEventsFactory.GetEvent(payloadBytes)));
        return true;
    }

    private byte[] ReadBytes(int messageLength)
    {
        byte[] result = new byte[messageLength];
        int totalBytesRead = 0;
        int chunkLength = 0;
        do
        {
            chunkLength = _stream.Read(result, totalBytesRead, messageLength - totalBytesRead);
            totalBytesRead += chunkLength;

        } while (totalBytesRead < messageLength && chunkLength > 0);

        if (chunkLength == 0)
            return null;

        return result;
    }

    private int GetPayloadLength(byte[] chunk)
    {
        return BitConverter.ToInt16(chunk, 1);
    }       

    private bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate == null || chain == null)
            return false;
        
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // If there is more than one error then it shouldn't be allowed
        if (chain.ChainStatus.Length == 1)
        {           
            // Self signed certificates have the issuer in the subject field
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors || certificate.Subject == certificate.Issuer)
            {
                // Self-signed certificates with an untrusted root are valid.
                if (chain.ChainStatus[0].Status == X509ChainStatusFlags.UntrustedRoot)
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected virtual void OnServerEvent(ServerEventArgs e)
    {
        if (ServerEvent != null)
            ServerEvent.Invoke(this, e);
    }

    protected virtual void OnConnected()
    {
        if (Connected != null)
            Connected.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnDisconnected()
    {
        if (Disconnected != null)
            Disconnected.Invoke(this, EventArgs.Empty);
    }
    
    public void Reset()
    {
        Debug.Log("TLSConnectionService::Reset");
        //TODO: reset all instances here for further re-connect
        _stream = null;
        _socket.Close();
        _outcomeMessages.Clear();
        _sendingData = false;
    }
}
