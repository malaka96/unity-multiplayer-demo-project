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
    private GameObject localObject;

    private float timeer;

    public bool sendMovements = false;

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

        timeer = Time.deltaTime;

        // Continuously send local object transform if connected
        if (webSocket != null && webSocket.State == WebSocketState.Open && localObject != null && sendMovements)
        {
            SendObjectTransform(localObject.transform.position, localObject.transform.rotation.eulerAngles);
        }
    }

    public void SendObjectTransform(Vector3 pos, Vector3 rot)
    {
        MovePayload move = new MovePayload
        {
            x = pos.x,
            y = pos.y,
            z = pos.z,
            rx = rot.x,
            ry = rot.y,
            rz = rot.z,
            isSliding = false
        };

        ChatMessage moveMsg = new ChatMessage
        {
            sender = username,
            type = "MOVE",
            payload = JsonUtility.ToJson(move)
        };

        string json = JsonUtility.ToJson(moveMsg);
        webSocket.SendText(json);
        Debug.Log("Sent Object Transform: " + json);
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
                if (chatMsg.sender == username)
                {
                    localObject = playerObj;
                }
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
        else if (chatMsg.type == "MOVE")
        {
            MovePayload move = JsonUtility.FromJson<MovePayload>(chatMsg.payload);

            // If player doesn't exist yet, create them
            if (!players.ContainsKey(chatMsg.sender))
            {
                int index = players.Count % spawnPoints.Length;
                GameObject playerObj = Instantiate(playerPrefab, spawnPoints[index].position, Quaternion.identity);
                playerObj.name = chatMsg.sender;
                players.Add(chatMsg.sender, playerObj);
            }

            if (chatMsg.sender == username) return;

            GameObject remoteObj = players[chatMsg.sender];
            remoteObj.transform.position = new Vector3(move.x, move.y, move.z);
            remoteObj.transform.rotation = Quaternion.Euler(move.rx, move.ry, move.rz);
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

[System.Serializable]
public class MovePayload {
    public float x;
    public float y;
    public float z;
    public float rx;
    public float ry;
    public float rz;
    public bool isSliding; 
}
