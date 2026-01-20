using System;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string url = "ws://localhost:8080/ws";

    [Header("UI Elements")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private InputField joinCodeInput;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject inRoomPanel;

    private WebSocket ws;
    public string MyPlayerId { get; private set; } = Guid.NewGuid().ToString();
    public string CurrentRoomId { get; private set; }

    async void Start()
    {
        createRoomButton.onClick.AddListener(() => CreateRoom("Player" + UnityEngine.Random.Range(100, 999)));
        joinRoomButton.onClick.AddListener(OnJoinButtonClicked);

        if (inRoomPanel) inRoomPanel.SetActive(false);

        ConnectToServer();
    }

    private async void ConnectToServer()
    {
        ws = new WebSocket(url);

        ws.OnOpen += () => Debug.Log("Connected!");
        ws.OnMessage += OnMessageReceived;
        ws.OnError += (err) => Debug.LogError("Error: " + err);
        ws.OnClose += (code) => Debug.Log("Closed: " + code);

        await ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
    }

    private void OnMessageReceived(byte[] bytes)
    {
        string json = System.Text.Encoding.UTF8.GetString(bytes);
        Debug.Log("Received: " + json);

        try
        {
            var msg = JsonUtility.FromJson<MessageResponse>(json);

            switch (msg.type)
            {
                case "ROOM_CREATED":
                    CurrentRoomId = msg.roomId;
                    ShowRoomCode(msg.roomId);
                    break;

                case "JOIN_SUCCESS":
                    CurrentRoomId = msg.roomId;
                    ShowRoomCode(msg.roomId);
                    break;

                case "PLAYER_JOINED":
                    statusText.text = $"{msg.playerName ?? "Someone"} joined the room!";
                    break;

                case "ERROR":
                    statusText.text = "Error: " + (msg.content ?? msg.message ?? "Unknown error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Message parse error: " + e.Message);
        }
    }

    private void ShowRoomCode(string code)
    {
        if (statusText)
        {
            statusText.text = $"Room Code: {code}\nShare this with friends!";
            statusText.color = Color.green;
        }

        if (inRoomPanel)
            inRoomPanel.SetActive(true);
    }

    private void OnJoinButtonClicked()
    {
        string code = joinCodeInput?.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Please enter room code";
            return;
        }

        JoinRoom(code, "Player" + UnityEngine.Random.Range(100, 999));
    }

    public void CreateRoom(string playerName)
    {
        var msg = new OutgoingMessage
        {
            type = "CREATE_ROOM",
            playerId = MyPlayerId,
            playerName = playerName
        };

        Send(msg);
    }

    public void JoinRoom(string roomId, string playerName)
    {
        var msg = new OutgoingMessage
        {
            type = "JOIN_ROOM",
            roomId = roomId,
            playerId = MyPlayerId,
            playerName = playerName
        };

        Send(msg);
    }

    private void Send(OutgoingMessage msg)
    {
        if (ws?.State != WebSocketState.Open) return;

        string json = JsonUtility.ToJson(msg);
        Debug.Log("Sent: " + json); // should now be correct JSON
        ws.SendText(json);
    }

    private async void OnDestroy()
    {
        if (ws != null)
        {
            await ws.Close();
        }
    }
}

// Helper classes
[System.Serializable]
public class OutgoingMessage
{
    public string type;
    public string playerId;
    public string playerName;
    public string roomId;
}

[System.Serializable]
public class MessageResponse
{
    public string type;
    public string roomId;
    public string playerId;
    public string playerName;
    public string content;
    public string message; // in case server uses different field
}