using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 2.2f, -4f);
    public float mouseSensitivity = 2.5f;
    public float smoothTime = 0.1f;
    public float rotationSmoothTime = 0.1f;

    private float yaw;
    private float pitch = 15f;
    private Vector3 velocity;
    private Quaternion rotationVelocity;

    void LateUpdate()
    {
        // Mouse input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -20f, 60f);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);

        // Smooth camera position
        Vector3 desiredPos = player.position + rot * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        // Smooth camera rotation
        Quaternion lookRot = Quaternion.LookRotation(player.position + Vector3.up * 1.5f - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSmoothTime * 10f);
    }
}
