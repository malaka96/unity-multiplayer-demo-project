using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NativeWebSocket;

public class MultiplayerManager : MonoBehaviour
{
    [Header("UI")]
    public Button joinButton;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private WebSocket webSocket;
    private Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    private string username;

    async void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
    }


    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
#endif
    }

    async void OnJoinClicked()
    {
        username = "Player" + Random.Range(1000, 9999);

        // Connect to Spring Boot raw WebSocket endpoint
        webSocket = new WebSocket("ws://localhost:1000/ws"); // use your backend port

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to server");

            // Send JOIN message as plain JSON
            ChatMessage joinMsg = new ChatMessage { sender = username, type = "JOIN" };
            string json = JsonUtility.ToJson(joinMsg);
            webSocket.SendText(json);
        };

        webSocket.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Message received: " + msg);
            HandleServerMessage(msg);
        };

        webSocket.OnClose += (code) =>
        {
            Debug.Log("Disconnected from server");
        };

        await webSocket.Connect();
    }

    void HandleServerMessage(string msg)
    {
        ChatMessage chatMsg = JsonUtility.FromJson<ChatMessage>(msg);

        if (chatMsg.type == "JOIN")
        {
            if (!players.ContainsKey(chatMsg.sender))
            {
                int index = players.Count % spawnPoints.Length;
                GameObject playerObj = Instantiate(playerPrefab, spawnPoints[index].position, Quaternion.identity);
                playerObj.name = chatMsg.sender;
                players.Add(chatMsg.sender, playerObj);
            }
        }
        else if (chatMsg.type == "LEAVE")
        {
            if (players.ContainsKey(chatMsg.sender))
            {
                Destroy(players[chatMsg.sender]);
                players.Remove(chatMsg.sender);
            }
        }
    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            // Send LEAVE before closing
            ChatMessage leaveMsg = new ChatMessage { sender = username, type = "LEAVE" };
            string json = JsonUtility.ToJson(leaveMsg);
            webSocket.SendText(json);

            await webSocket.Close();
        }
    }
}

[System.Serializable]
public class ChatMessage
{
    public string sender;
    public string type;
    public string payload;
}
