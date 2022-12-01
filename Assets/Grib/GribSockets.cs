using System.Collections.Generic;
using WebSocketSharp;
using Newtonsoft.Json;
using System;
using UnityEngine;


[System.Serializable]
public class GribSockets
{
    private string host;

    private WebSocket socket;
    private bool isConnected;

    private Action<EventArgs> OnConnected;
    private Action<CloseEventArgs> OnDisconnected;

    public bool IsConnected { get => isConnected; }

    System.Threading.SynchronizationContext syncContext;

    Dictionary<string, Action<OdometerMessage>> subscriptions = new Dictionary<string, Action<OdometerMessage>>();

    public GribSockets(string host, Action<EventArgs> onConnected, Action<CloseEventArgs> onDisconnected)
    {
        syncContext = System.Threading.SynchronizationContext.Current;
        this.host = host;
        OnConnected = onConnected;
        OnDisconnected = onDisconnected;
        Connect(host);
    }

    public void SendMessage (string head, object body = null)
    {
        if (!isConnected)
            return;
        string msg = JsonConvert.SerializeObject(new MessageFrame(head, body));
        socket.SendAsync(JsonConvert.SerializeObject(new MessageFrame(head, body)),(complete)=> 
        {
            if (!complete)
                Debug.LogWarning("Message lost: " + head);
        });
    }

    private void Connect(string host)
    {
        socket = new WebSocket(host);
        socket.OnMessage += GotMessage;
        socket.OnClose += Socket_OnClose;
        socket.OnOpen += Socket_OnOpen;
        socket.ConnectAsync();
    }

    private void GotMessage(object sender, MessageEventArgs e)
    {
        if (e.IsText)
        {
            Debug.Log(e.Data);
            var data = JsonConvert.DeserializeObject<OdometerMessage>(e.Data);
            if (subscriptions.ContainsKey(data.operation))
            {
                syncContext.Post(_ =>
                {
                    subscriptions[data.operation](data);
                }, null);
            }
        }
    }

    private void Socket_OnClose(object sender, CloseEventArgs e)
    {
        if (IsConnected)
        {
            syncContext.Post(_ =>
            {
                OnDisconnected?.Invoke(e);
            }, null);

            Debug.Log("Disconnected. Trying to reconnect...");
        }
        isConnected = false;
        if (!disconnecting)
            Connect(host);
    }

    private void Socket_OnOpen(object sender, EventArgs e)
    {
        isConnected = true;
        syncContext.Post(_ =>
        {
            OnConnected?.Invoke(e);
        }, null);
    }

    public void Subscribe(string operation, Action<OdometerMessage> action)
    {
        if (!subscriptions.ContainsKey(operation))
            subscriptions.Add(operation, action);
    }

    public void Unsubscribe(string operation, Action<OdometerMessage> action)
    {
        if (subscriptions.ContainsKey(operation))
            subscriptions.Remove(operation);
    }

    bool disconnecting = false;
    public void Disconnect()
    {
        disconnecting = true;
        socket.CloseAsync();
    }
}
[System.Serializable]
class MessageFrame
{
    public string operation;
    public object body;

    public MessageFrame(string operation, object body)
    {
        this.operation = operation;
        this.body = body;
    }
}

public class OdometerMessage
{
    public string operation;
    public float value;
    public float odometer;
    public bool status;
}
