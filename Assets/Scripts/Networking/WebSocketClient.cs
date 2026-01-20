using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string url = "ws://localhost:8080/ws";

    [Header("UI Elements - Lobby")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private InputField joinCodeInput;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject inRoomPanel;

    [Header("Game Start")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private GameObject playerPrefab;           // Your player prefab (character + controller + camera)
    [SerializeField] private Transform spawnPointsParent;       // Optional: empty GameObject with child spawn points

    private WebSocket ws;
    public string MyPlayerId { get; private set; } = Guid.NewGuid().ToString();
    public string CurrentRoomId { get; private set; }

    private bool amIHost = false;
    private readonly List<string> playersInRoom = new List<string>();
    private readonly Dictionary<string, string> playerNames = new Dictionary<string, string>();

    async void Start()
    {
        // Button listeners
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(() => CreateRoom("Player" + UnityEngine.Random.Range(100, 999)));

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinButtonClicked);

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        // Initial UI state
        if (createRoomPanel) createRoomPanel.SetActive(true);
        if (inRoomPanel) inRoomPanel.SetActive(false);

        ConnectToServer();
    }

    private async void ConnectToServer()
    {
        ws = new WebSocket(url);

        ws.OnOpen += () => Debug.Log("Connected to server!");
        ws.OnMessage += OnMessageReceived;
        ws.OnError += (err) => Debug.LogError("WebSocket Error: " + err);
        ws.OnClose += (code) => Debug.Log("WebSocket Closed: " + code);

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
                    amIHost = true;
                    ShowRoomCode(msg.roomId);
                    UpdateStartButtonVisibility();
                    break;

                case "JOIN_SUCCESS":
                    CurrentRoomId = msg.roomId;
                    ShowRoomCode(msg.roomId);
                    break;

                case "PLAYER_JOINED":
                    if (!playersInRoom.Contains(msg.playerId))
                    {
                        playersInRoom.Add(msg.playerId);
                        playerNames[msg.playerId] = msg.playerName ?? "Unknown";
                    }

                    statusText.text = $"{msg.playerName ?? "Someone"} joined!  ({playersInRoom.Count} players)";
                    UpdateStartButtonVisibility();
                    break;

                case "GAME_START":
                    StartLocalGame();
                    break;

                case "ERROR":
                    statusText.text = "Error: " + (msg.content ?? msg.message ?? "Unknown error");
                    statusText.color = Color.red;
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
        if (statusText != null)
        {
            statusText.text = $"Room Code: {code}\nShare this with friends!";
            statusText.color = Color.green;
        }

        if (createRoomPanel) createRoomPanel.SetActive(false);
        if (inRoomPanel) inRoomPanel.SetActive(true);
    }

    private void UpdateStartButtonVisibility()
    {
        if (startGameButton == null) return;

        Debug.Log(playersInRoom.Count);
        bool canStart = amIHost && playersInRoom.Count >= 1;
        startGameButton.gameObject.SetActive(canStart);
    }

    private void OnJoinButtonClicked()
    {
        string code = joinCodeInput?.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Please enter a room code";
            statusText.color = Color.yellow;
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

    private void OnStartGameClicked()
    {
        Debug.Log(amIHost);
        if (!amIHost) return;

        var msg = new OutgoingMessage
        {
            type = "START_GAME",
            playerId = MyPlayerId,
            roomId = CurrentRoomId
        };

        Send(msg);
        statusText.text = "Starting game...";
        statusText.color = Color.cyan;
    }

    private void StartLocalGame()
    {
        statusText.text = "Game Started!";
        statusText.color = Color.white;

        // Hide lobby UI
        if (createRoomPanel) createRoomPanel.SetActive(false);
        if (inRoomPanel) inRoomPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned!");
            return;
        }

        Transform[] spawnPoints = null;
        if (spawnPointsParent != null)
        {
            spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>()
                .Where(t => t != spawnPointsParent).ToArray();
        }

        int index = 0;

        foreach (var playerId in playersInRoom)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            // Use spawn points if available
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var point = spawnPoints[index % spawnPoints.Length];
                position = point.position;
                rotation = point.rotation;
            }
            else
            {
                // Simple fallback spread
                position = new Vector3(index * 4f - (playersInRoom.Count - 1) * 2f, 1f, 0);
            }

            // Spawn the player
            GameObject playerInstance = Instantiate(playerPrefab, position, rotation);

            // Try to configure player (you'll probably need to adjust class & method names)
            var playerController = playerInstance.GetComponent<PlayerController_CharacterController>(); //  CHANGE TO YOUR ACTUAL SCRIPT NAME
            if (playerController != null)
            {
                playerController.Initialize(playerId, playerNames.GetValueOrDefault(playerId, "Unknown"));

                bool isLocal = string.Equals(playerId, MyPlayerId, StringComparison.Ordinal);
                playerController.SetAsLocalPlayer(isLocal);

                // Optional: enable/disable components for remote players
                // e.g. disable camera, input, audio listener for non-local players
            }

            index++;
        }
    }

    private void Send(OutgoingMessage msg)
    {
        if (ws == null || ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("Cannot send - WebSocket not connected");
            return;
        }

        string json = JsonUtility.ToJson(msg);
        Debug.Log("Sent: " + json);
        ws.SendText(json);
    }

    private async void OnDestroy()
    {
        if (ws != null)
        {
            await ws.Close();
        }
    }

    // Optional: call this when leaving room / disconnecting
    private void ClearRoomData()
    {
        playersInRoom.Clear();
        playerNames.Clear();
        amIHost = false;
        UpdateStartButtonVisibility();
    }
}



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
    public string message; // in case server uses different field name
}