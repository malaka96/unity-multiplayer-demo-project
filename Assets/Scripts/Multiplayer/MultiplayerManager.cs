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
    public string RoomId { get; private set; }

    private GameObject localObject;

    public bool sendMovements = false;

    Vector3 playerLastPos;

    async void Start()
    {
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif

        if (webSocket == null || webSocket.State != WebSocketState.Open) return;
        if (localObject == null) return;

        var controller = localObject.GetComponentInChildren<PlayerController_CharacterController>();
        if (controller == null) return;

        if (Input.GetKeyDown(KeyCode.U))
            sendMovements = !sendMovements;

        if(sendMovements)
            SendPlayerState(controller);
    }

    public void CreateRoom()
    {
        RoomId = Random.Range(100000, 999999).ToString();

        SendRoomMessage("CREATE_ROOM");
    }

    public void JoinRoom(string inputRoomId)
    {
        RoomId = inputRoomId;
        SendRoomMessage("JOIN_ROOM");
    }

    void SendRoomMessage(string type)
    {
        ChatMessage msg = new ChatMessage
        {
            sender = username,
            type = type,
            roomId = RoomId
        };

        webSocket.SendText(JsonUtility.ToJson(msg));
    }


    void SendPlayerState(PlayerController_CharacterController controller)
    {

        if (Vector3.Distance(controller.transform.position, playerLastPos) < 0.01f)
            return;

        playerLastPos = controller.transform.position;

        MovePayload payload = new MovePayload
        {
            px = controller.transform.position.x,
            py = controller.transform.position.y,
            pz = controller.transform.position.z,

            rx = controller.transform.eulerAngles.x,
            ry = controller.transform.eulerAngles.y,
            rz = controller.transform.eulerAngles.z,

            moveX = controller.NetworkMoveX,
            moveZ = controller.NetworkMoveZ,

            isGrounded = controller.NetworkIsGrounded,
            isSliding = controller.NetworkIsSliding
        };

        ChatMessage msg = new ChatMessage
        {
            sender = username,
            type = "MOVE",
            roomId = RoomId,
            payload = JsonUtility.ToJson(payload)
        };


        webSocket.SendText(JsonUtility.ToJson(msg));
    }

    async void OnJoinClicked()
    {
        username = "Player" + Random.Range(1000, 9999);
        webSocket = new WebSocket("ws://localhost:1000/ws");

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected");
            webSocket.SendText(JsonUtility.ToJson(
                new ChatMessage { sender = username, type = "JOIN" }
            ));
        };

        webSocket.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log(msg);
            HandleServerMessage(msg);
        };

        webSocket.OnClose += (code) =>
        {
            Debug.Log("Disconnected");
        };

        await webSocket.Connect();
    }

    void HandleServerMessage(string msg)
    {
        ChatMessage chatMsg = JsonUtility.FromJson<ChatMessage>(msg);

        if (chatMsg.type == "JOIN")
        {
            if (players.ContainsKey(chatMsg.sender)) return;

            int index = players.Count % spawnPoints.Length;
            GameObject obj = Instantiate(playerPrefab, spawnPoints[index].position, Quaternion.identity);
            obj.name = chatMsg.sender;

            var controller = obj.GetComponentInChildren<PlayerController_CharacterController>();
            if (controller != null)
            {
                bool isLocal = chatMsg.sender == username;
                controller.SetAsLocalPlayer(isLocal);
            }
            else
                Debug.Log("controller is null");

            players.Add(chatMsg.sender, obj);
            if (chatMsg.sender == username)
                localObject = obj;
        }
        else if (chatMsg.type == "LEAVE")
        {
            if (!players.ContainsKey(chatMsg.sender)) return;

            Destroy(players[chatMsg.sender]);
            players.Remove(chatMsg.sender);
        }
        else if (chatMsg.type == "MOVE")
        {
            if (chatMsg.sender == username) return;
            if (!players.ContainsKey(chatMsg.sender)) return;

            MovePayload move = JsonUtility.FromJson<MovePayload>(chatMsg.payload);
            var controller = players[chatMsg.sender]
                .GetComponentInChildren<PlayerController_CharacterController>();

            controller?.ApplyRemoteState(move);
        }
    }

    async void OnApplicationQuit()
    {
        if (webSocket == null) return;

        webSocket.SendText(JsonUtility.ToJson(
            new ChatMessage { sender = username, type = "LEAVE" }
        ));

        await webSocket.Close();
    }
}

[System.Serializable]
public class ChatMessage
{
    public string sender;
    public string type;
    public string roomId;
    public string payload;
}

[System.Serializable]
public class MovePayload
{
    public float px, py, pz;
    public float rx, ry, rz;

    public float moveX;
    public float moveZ;

    public bool isGrounded;
    public bool isSliding;
}
