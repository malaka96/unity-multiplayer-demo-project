using UnityEngine;
using UnityEngine.UI;

public class RoomUIManager : MonoBehaviour
{
    [Header("UI")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public InputField roomIdInput;
    public Text roomIdText;
    public Text statusText;

    [Header("Managers")]
    public MultiplayerManager multiplayerManager;

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);

        statusText.text = "Not connected";
        roomIdText.text = "";
    }

    void OnCreateRoom()
    {
        multiplayerManager.CreateRoom();
        roomIdText.text = "Room ID: " + multiplayerManager.RoomId;
        statusText.text = "Room created. Waiting for players...";
    }

    void OnJoinRoom()
    {
        if (string.IsNullOrEmpty(roomIdInput.text))
        {
            statusText.text = "Please enter a Room ID";
            return;
        }

        multiplayerManager.JoinRoom(roomIdInput.text);
        statusText.text = "Joining room...";
    }
}
