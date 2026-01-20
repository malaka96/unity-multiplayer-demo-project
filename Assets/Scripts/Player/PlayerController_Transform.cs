using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController_CharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -25f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Camera & Visuals")]
    [SerializeField] private Transform cameraTransform;     // usually child of player
    [SerializeField] private Animator animator;

    [Header("X Axis Rotators (leaning/tilting)")]
    [SerializeField] private Transform[] xAxisObjects;
    [SerializeField] private float minX = -30f;
    [SerializeField] private float maxX = 30f;
    [SerializeField] private float rotateSpeed = 40f;

    [Header("Slide Settings")]
    [SerializeField] private Transform[] slideObjects;
    [SerializeField] private float slideRotationX = 90f;
    [SerializeField] private float slideDuration = 0.5f;

    // Network / Multiplayer
    [Header("Multiplayer")]
    [SerializeField] private bool isLocalPlayer = false;
    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }

    // Private fields
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isSliding;
    private float slideTimer;
    private bool[] rotateForward;

    private const float NORMAL_HEIGHT = 2.7f;
    private const float SLIDE_HEIGHT = 1f;


    public void Initialize(string playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
    }


    public void SetAsLocalPlayer(bool local)
    {
        isLocalPlayer = local;

        // Very important for multiplayer:
        if (cameraTransform != null)
        {
            cameraTransform.gameObject.SetActive(isLocalPlayer);
        }

        
    }


    void Awake()
    {
        controller = GetComponent<CharacterController>();

  
        if (isLocalPlayer)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
    }


    void Start()
    {
        controller.height = NORMAL_HEIGHT;
        InitXAxisRotation();


        if (isLocalPlayer && cameraTransform == null)
        {
            Debug.LogWarning($"{name} is local player but has no camera assigned!");
        }
        if(!isLocalPlayer && cameraTransform != null)
        {
            cameraTransform.gameObject.SetActive(false);
        }
    }


    void Update()
    {
        GroundCheck();

 
        if (isLocalPlayer && !isSliding)
        {
            ReadInput();
        }

        HandleMovement();
        HandleJumpAndGravity();
        HandleAnimation();


        if (isLocalPlayer && !isSliding && isGrounded &&
            Input.GetKeyDown(KeyCode.C) && moveDirection.magnitude > 0.1f)
        {
            StartSlide();
        }

        if (isSliding)
        {
            HandleSlide();
        }
    }


  
    private void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Quaternion camYRot = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        Vector3 forward = camYRot * Vector3.forward;
        Vector3 right = camYRot * Vector3.right;

        moveDirection = (forward * v + right * h).normalized;
    }



    private void HandleMovement()
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



    private void HandleJumpAndGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

  
        if (isLocalPlayer && isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }



    private void InitXAxisRotation()
    {
        rotateForward = new bool[xAxisObjects.Length];
        for (int i = 0; i < rotateForward.Length; i++)
            rotateForward[i] = i < 2; 
    }

    private void RotateObjectsOnXAxis()
    {
        for (int i = 0; i < xAxisObjects.Length; i++)
        {
            var obj = xAxisObjects[i];
            if (obj == null) continue;

            float x = obj.localEulerAngles.x;
            if (x > 180f) x -= 360f;

            if (x >= maxX) rotateForward[i] = false;
            if (x <= minX) rotateForward[i] = true;

            float direction = rotateForward[i] ? 1f : -1f;
            float delta = rotateSpeed * Time.deltaTime * direction;

            x = Mathf.Clamp(x + delta, minX, maxX);

            obj.localEulerAngles = new Vector3(x, obj.localEulerAngles.y, obj.localEulerAngles.z);
        }
    }

    private void ResetRotatedObjectsOnXAxis()
    {
        for (int i = 0; i < xAxisObjects.Length; i++)
        {
            var obj = xAxisObjects[i];
            if (obj == null) continue;

            float x = obj.localEulerAngles.x;
            if (x > 180f) x -= 360f;

            float newX = Mathf.MoveTowards(x, 0f, rotateSpeed * Time.deltaTime);
            obj.localEulerAngles = new Vector3(newX, obj.localEulerAngles.y, obj.localEulerAngles.z);
        }
    }


 
    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
    }



    private void HandleAnimation()
    {
        if (animator == null) return;

        float speed = moveDirection.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetBool("Grounded", isGrounded);
    
    }



    private void StartSlide()
    {
        if (slideObjects == null || slideObjects.Length == 0) return;

        isSliding = true;
        slideTimer = 0f;
        controller.height = SLIDE_HEIGHT;


        if (animator != null)
            animator.SetTrigger("Slide");
    }

    private void HandleSlide()
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
            ? Mathf.Lerp(NORMAL_HEIGHT, SLIDE_HEIGHT, t * 2f)
            : Mathf.Lerp(SLIDE_HEIGHT, NORMAL_HEIGHT, (t - 0.5f) * 2f);

        if (t >= 1f)
        {
            isSliding = false;
            controller.height = NORMAL_HEIGHT;
            InitXAxisRotation();
        }
    }


  
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}