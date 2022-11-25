using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GribConnection : MonoBehaviour
{
    public GribSockets socket;
    [SerializeField] Image statusLamp;
    public void Connect()
    {
        socket = new GribSockets("ws://185.246.65.199:9090/ws", OnConnected, OnDisconnected);
    }

    private void OnDisconnected(WebSocketSharp.CloseEventArgs e)
    {
        Debug.Log("Disconnected from server");
        statusLamp.color = Color.red;
    }

    private void OnConnected(System.EventArgs e)
    {
        Debug.Log("Connected to server");
        statusLamp.color = Color.green;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            socket.SendMessage("getCurrentOdometer");
        }
    }
}