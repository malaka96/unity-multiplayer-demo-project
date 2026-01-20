//using UnityEngine;

//public class PlayerSpawner : MonoBehaviour
//{
//    public static PlayerSpawner Instance;
//    public GameObject playerPrefab;

//    GameObject player;

//    void Awake() => Instance = this;

//    public void SpawnPlayer(bool isHost)
//    {
//        player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
//        player.GetComponent<PlayerController>().enabled = isHost;
//    }

//    public void ApplyHostState(string json)
//    {
//        if (NetworkManager.Instance.isHost) return;

//        var state = JsonUtility.FromJson<HostState>(json);
//        player.transform.position = Vector3.Lerp(
//            player.transform.position,
//            state.pos,
//            Time.deltaTime * 10f
//        );
//        player.transform.rotation =
//            Quaternion.Euler(0, state.rotY, 0);
//    }
//}
