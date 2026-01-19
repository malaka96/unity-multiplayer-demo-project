using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 4f;
    public float height = 2f;
    public float mouseSensitivity = 3f;
    public float smoothSpeed = 0.08f;

    [Header("Rotation Limits")]
    public float minY = -30f;
    public float maxY = 70f;

    private float yaw;
    private float pitch;
    private Vector3 currentVelocity;

    // Multiplayer-ready flag
    public bool isLocalPlayer = true;

    void Start()
    {
        if (!isLocalPlayer)
        {
            gameObject.SetActive(false); // Disable camera for remote players
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        RotateCamera();
        FollowTarget();
    }

    void RotateCamera()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minY, maxY);
    }

    void FollowTarget()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition =
            target.position - (rotation * Vector3.forward * distance)
            + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothSpeed
        );

        transform.LookAt(target.position + Vector3.up * height);
    }
}
