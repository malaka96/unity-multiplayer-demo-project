using UnityEngine;

public class PlayerController_Transform : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("X Axis Rotators")]
    public Transform[] xAxisObjects;
    public float minX = -30f;
    public float maxX = 30f;
    public float rotateSpeed = 40f;
    bool[] rotateForward;

    [Header("Slide Settings")]
    public Transform[] slideObjects;
    public float slideRotationX = 90f;
    public float slideDuration = 0.5f;
    bool isSliding = false;
    float slideTimer = 0f;

    [Header("Colliders")]
    public CapsuleCollider capsuleCollider;

    Rigidbody rb;
    Vector3 moveDirection;
    public bool isGrounded;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        InitXAxisRotation();

        // Store initial rotations of slide objects
        if (slideObjects != null && slideObjects.Length > 0)
        {
            for (int i = 0; i < slideObjects.Length; i++)
            {
                slideObjects[i].localEulerAngles = Vector3.zero;
            }
        }
    }

    void Update()
    {
        GroundCheck();
        ReadInput();
        HandleAnimation();

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Slide trigger
        if (!isSliding && isGrounded && Input.GetKeyDown(KeyCode.C) && moveDirection.magnitude > 0.1f)
            StartSlide();

        // Slide handling
        if (isSliding)
            HandleSlide();
    }

    void FixedUpdate()
    {
        Move();
    }

    // ------------------ INPUT ------------------
    void ReadInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Use only camera Y rotation to stabilize movement
        Quaternion camYRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        Vector3 forward = camYRot * Vector3.forward;
        Vector3 right = camYRot * Vector3.right;

        moveDirection = (forward * v + right * h).normalized;
    }

    // ------------------ MOVEMENT ------------------
    void Move()
    {
        // Rigidbody movement (kinematic-like)
        Vector3 targetMove = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + targetMove);

        // Player rotation
        if (moveDirection.magnitude > 0.1f)
        {
            RotateObjectsOnXAxis();

            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            ResetRotatedObjectsOnXAxis();
        }
    }

    // ------------------ X-AXIS ROTATION ------------------
    void InitXAxisRotation()
    {
        rotateForward = new bool[xAxisObjects.Length];
        for (int i = 0; i < rotateForward.Length; i++)
            rotateForward[i] = i < 2; // first 2 forward, last 2 backward
    }

    void RotateObjectsOnXAxis()
    {
        for (int i = 0; i < xAxisObjects.Length; i++)
        {
            if (xAxisObjects[i] == null) continue;

            float x = xAxisObjects[i].localEulerAngles.x;
            if (x > 180f) x -= 360f;

            if (x >= maxX) rotateForward[i] = false;
            if (x <= minX) rotateForward[i] = true;

            float delta = rotateSpeed * Time.deltaTime * (rotateForward[i] ? 1f : -1f);
            x = Mathf.Clamp(x + delta, minX, maxX);

            Vector3 rot = xAxisObjects[i].localEulerAngles;
            xAxisObjects[i].localEulerAngles = new Vector3(x, rot.y, rot.z);
        }
    }

    void ResetRotatedObjectsOnXAxis()
    {
        for (int i = 0; i < xAxisObjects.Length; i++)
        {
            if (xAxisObjects[i] == null) continue;

            float currentX = xAxisObjects[i].localEulerAngles.x;
            if (currentX > 180f) currentX -= 360f;

            float newX = Mathf.MoveTowards(currentX, 0f, rotateSpeed * Time.deltaTime);

            Vector3 rot = xAxisObjects[i].localEulerAngles;
            xAxisObjects[i].localEulerAngles = new Vector3(newX, rot.y, rot.z);
        }
    }

    // ------------------ GROUND ------------------
    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
    }

    // ------------------ ANIMATION ------------------
    void HandleAnimation()
    {
        // animator.SetFloat("Speed", moveDirection.magnitude);
        // animator.SetBool("Grounded", isGrounded);
    }

    // ------------------ SLIDE ------------------
    void StartSlide()
    {
        if (slideObjects == null || slideObjects.Length == 0) return;

        isSliding = true;
        slideTimer = 0f;

        // Shrink capsule height at start
        capsuleCollider.height = 1f;
    }

    void HandleSlide()
    {
        slideTimer += Time.deltaTime;
        float t = Mathf.Clamp01(slideTimer / slideDuration);

        float xRot = (t <= 0.5f) ?
            Mathf.Lerp(0f, slideRotationX, t * 2f) :
            Mathf.Lerp(slideRotationX, 0f, (t - 0.5f) * 2f);

        // Rotate slide objects
        foreach (var obj in slideObjects)
        {
            if (obj == null) continue;
            Vector3 rot = obj.localEulerAngles;
            rot.x = xRot;
            obj.localEulerAngles = rot;
        }

        // Smoothly restore capsule height
        float startHeight = 1f;
        float targetHeight = 2.7f;
        capsuleCollider.height = (t <= 0.5f) ?
            Mathf.Lerp(targetHeight, startHeight, t * 2f) :
            Mathf.Lerp(startHeight, targetHeight, (t - 0.5f) * 2f);

        if (t >= 1f)
        {
            isSliding = false;
            InitXAxisRotation();
        }
    }
}
