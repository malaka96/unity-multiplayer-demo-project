//using WebSocketSharp;
//using UnityEngine;
//using System;
//using System.Net.WebSockets;

//public class WSClient : MonoBehaviour
//{
//    public static WSClient Instance;
//    WebSocket ws;

//    void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);
//    }

//    public void Connect()
//    {
//        ws = new WebSocket("ws://localhost:8080");
//        ws.OnMessage += (s, e) =>
//        {
//            NetworkManager.Instance.OnMessage(e.Data);
//        };
//        ws.Connect();
//    }

//    public void Send(string json)
//    {
//        if (ws != null && ws.IsAlive)
//            ws.Send(json);
//    }
//}
