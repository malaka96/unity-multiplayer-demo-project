using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController_CharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -25f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.25f;
    [SerializeField] LayerMask groundMask;

    [Header("Camera")]
    [SerializeField] Transform cameraTransform;

    [Header("Script Anim Parts")]
    [SerializeField] Transform[] handsAndLegs;
    [SerializeField] float minX = -30f;
    [SerializeField] float maxX = 30f;
    [SerializeField] float rotateSpeed = 40f;

    [Header("Slide")]
    [SerializeField] Transform[] legs;
    [SerializeField] float slideRotationX = 90f;
    [SerializeField] float slideDuration = 0.5f;

    // NETWORK
    public float NetworkMoveX { get; private set; }
    public float NetworkMoveZ { get; private set; }
    public bool NetworkIsGrounded => isGrounded;
    public bool NetworkIsSliding => isSliding;

    public bool isLocalPlayer;
    CharacterController controller;
    Vector3 velocity;
    Vector3 moveDirection;
    bool isGrounded;
    bool isSliding;
    float slideTimer;
    bool[] rotateForward;

    const float NORMAL_HEIGHT = 2.7f;
    const float SLIDE_HEIGHT = 1f;

    float lastNetworkUpdateTime;
    const float NETWORK_TIMEOUT = 0.15f;

    public void SetAsLocalPlayer(bool local)
    {
        isLocalPlayer = local;
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(local);
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        InitXAxisRotation();
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            CheckNetworkTimeout();
            HandleScriptAnimation();
            return;
        }

        GroundCheck();
        ReadInput();
        HandleMovement();
        HandleJumpAndGravity();

        if (!isSliding && isGrounded && Input.GetKeyDown(KeyCode.C) && moveDirection.magnitude > 0.1f)
            StartSlide();

        if (isSliding)
            HandleSlide();
    }

    void ReadInput()
    {
        NetworkMoveX = Input.GetAxisRaw("Horizontal");
        NetworkMoveZ = Input.GetAxisRaw("Vertical");

        Quaternion camY = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        moveDirection = (camY * new Vector3(NetworkMoveX, 0, NetworkMoveZ)).normalized;
    }

    void HandleMovement()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            RotateObjectsOnXAxis();
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDirection),
                rotationSpeed * Time.deltaTime
            );
        }
        else ResetRotatedObjects();

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    void HandleJumpAndGravity()
    {
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
    }

    void InitXAxisRotation()
    {
        rotateForward = new bool[handsAndLegs.Length];
        for (int i = 0; i < rotateForward.Length; i++)
            rotateForward[i] = i < 2;
    }

    void RotateObjectsOnXAxis()
    {
        for (int i = 0; i < handsAndLegs.Length; i++)
        {
            float x = handsAndLegs[i].localEulerAngles.x;
            if (x > 180) x -= 360;

            if (x >= maxX) rotateForward[i] = false;
            if (x <= minX) rotateForward[i] = true;

            x += rotateSpeed * Time.deltaTime * (rotateForward[i] ? 1 : -1);
            x = Mathf.Clamp(x, minX, maxX);

            handsAndLegs[i].localEulerAngles =
                new Vector3(x, handsAndLegs[i].localEulerAngles.y, 0);
        }
    }

    void ResetRotatedObjects()
    {
        foreach (var t in handsAndLegs)
        {
            float x = t.localEulerAngles.x;
            if (x > 180) x -= 360;
            t.localEulerAngles =
                new Vector3(Mathf.MoveTowards(x, 0, rotateSpeed * Time.deltaTime), t.localEulerAngles.y, 0);
        }
    }

    void HandleScriptAnimation()
    {
        if (moveDirection.magnitude > 0.1f)
            RotateObjectsOnXAxis();
        else
            ResetRotatedObjects();
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = 0;
        controller.height = SLIDE_HEIGHT;
    }

    void HandleSlide()
    {
        slideTimer += Time.deltaTime;
        float t = slideTimer / slideDuration;

        float x = Mathf.Sin(t * Mathf.PI) * slideRotationX;
        foreach (var l in legs)
            l.localEulerAngles = new Vector3(x, 0, 0);

        if (t >= 1)
        {
            isSliding = false;
            controller.height = NORMAL_HEIGHT;
            InitXAxisRotation();
        }
    }

    // CALLED FROM NETWORK
    public void ApplyRemoteState(MovePayload move)
    {
        lastNetworkUpdateTime = Time.time;

        transform.position = Vector3.Lerp(
            transform.position,
            new Vector3(move.px, move.py, move.pz),
            15f * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(move.rx, move.ry, move.rz),
            15f * Time.deltaTime
        );

        moveDirection = new Vector3(move.moveX, 0, move.moveZ).normalized;
        isGrounded = move.isGrounded;
        isSliding = move.isSliding;
    }

    void CheckNetworkTimeout()
    {
        if (Time.time - lastNetworkUpdateTime > NETWORK_TIMEOUT)
        {
            moveDirection = Vector3.zero;
            isSliding = false;
        }
    }

}
