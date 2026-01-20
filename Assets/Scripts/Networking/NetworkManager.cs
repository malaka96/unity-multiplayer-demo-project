//using UnityEngine;
//using System;

//public class NetworkManager : MonoBehaviour
//{
//    public static NetworkManager Instance;

//    public bool isHost;
//    public string roomCode;
//    public string playerId;

//    void Awake()
//    {
//        Instance = this;
//        playerId = Guid.NewGuid().ToString();
//    }

//    void Start()
//    {
//        WSClient.Instance.Connect();
//    }

//    public void CreateRoom()
//    {
//        WSClient.Instance.Send(JsonUtility.ToJson(new
//        {
//            type = "create",
//            playerId = playerId
//        }));
//    }

//    public void JoinRoom(string code)
//    {
//        WSClient.Instance.Send(JsonUtility.ToJson(new
//        {
//            type = "join",
//            room = code,
//            playerId = playerId
//        }));
//    }

//    public void OnMessage(string json)
//    {
//        if (json.Contains("\"created\""))
//        {
//            isHost = true;
//            roomCode = JsonUtility.FromJson<RoomResponse>(json).room;
//            PlayerSpawner.Instance.SpawnPlayer(true);
//        }
//        else if (json.Contains("\"joined\""))
//        {
//            isHost = false;
//            roomCode = JsonUtility.FromJson<RoomResponse>(json).room;
//            PlayerSpawner.Instance.SpawnPlayer(false);
//        }
//        else if (json.Contains("\"state\""))
//        {
//            PlayerSpawner.Instance.ApplyHostState(json);
//        }
//    }
//}
