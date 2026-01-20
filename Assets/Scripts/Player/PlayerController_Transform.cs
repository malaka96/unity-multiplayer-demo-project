using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController_CharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -25f;

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
    bool isSliding;
    float slideTimer;

    [Header("Character Controller")]
    public float normalHeight = 2.7f;
    public float slideHeight = 1f;

    CharacterController controller;
    Vector3 velocity;
    Vector3 moveDirection;
    public bool isGrounded;

    void Awake()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = normalHeight;

        InitXAxisRotation();
    }

    void Update()
    {
        GroundCheck();
        ReadInput();
        HandleMovement();
        HandleJumpAndGravity();
        HandleAnimation();

        // Slide trigger
        if (!isSliding && isGrounded && Input.GetKeyDown(KeyCode.C) && moveDirection.magnitude > 0.1f)
            StartSlide();

        if (isSliding)
            HandleSlide();
    }

    // ------------------ INPUT ------------------
    void ReadInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Quaternion camYRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        Vector3 forward = camYRot * Vector3.forward;
        Vector3 right = camYRot * Vector3.right;

        moveDirection = (forward * v + right * h).normalized;
    }

    // ------------------ MOVEMENT ------------------
    void HandleMovement()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            RotateObjectsOnXAxis();

            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            ResetRotatedObjectsOnXAxis();
        }

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    // ------------------ JUMP + GRAVITY ------------------
    void HandleJumpAndGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ------------------ X-AXIS ROTATION ------------------
    void InitXAxisRotation()
    {
        rotateForward = new bool[xAxisObjects.Length];
        for (int i = 0; i < rotateForward.Length; i++)
            rotateForward[i] = i < 2;
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

            float x = xAxisObjects[i].localEulerAngles.x;
            if (x > 180f) x -= 360f;

            float newX = Mathf.MoveTowards(x, 0f, rotateSpeed * Time.deltaTime);
            Vector3 rot = xAxisObjects[i].localEulerAngles;
            xAxisObjects[i].localEulerAngles = new Vector3(newX, rot.y, rot.z);
        }
    }

    // ------------------ GROUND ------------------
    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundRadius,
            groundMask
        );
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
        controller.height = slideHeight;
    }

    void HandleSlide()
    {
        slideTimer += Time.deltaTime;
        float t = Mathf.Clamp01(slideTimer / slideDuration);

        float xRot = (t <= 0.5f)
            ? Mathf.Lerp(0f, slideRotationX, t * 2f)
            : Mathf.Lerp(slideRotationX, 0f, (t - 0.5f) * 2f);

        foreach (var obj in slideObjects)
        {
            if (obj == null) continue;
            Vector3 rot = obj.localEulerAngles;
            rot.x = xRot;
            obj.localEulerAngles = rot;
        }

        controller.height = (t <= 0.5f)
            ? Mathf.Lerp(normalHeight, slideHeight, t * 2f)
            : Mathf.Lerp(slideHeight, normalHeight, (t - 0.5f) * 2f);

        if (t >= 1f)
        {
            isSliding = false;
            InitXAxisRotation();
        }
    }
}
