using UnityEngine;
using UnityEngine.InputSystem;

[Flags]
public enum PlayerContext : int
{
    Default      = 0,
    Menu         = 1,
    Interacting  = 2,
    Dialogue     = 4,
    PlayerInput  = 8,
    InDictionary = 16
}

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D)),
    RequireComponent(typeof(Animator))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2.0f;

    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private InputAction moveAction;
    private InputAction interactAction;

    private Vector2 movementDirection;
    private bool facingRight = true, canMove = true;

    [HideInInspector] public PlayerContext context = PlayerContext.Default;
    [HideInInspector] public OptionalComponent<Interactable> currentInteraction;

    public static PlayerController Instance { get; private set; }
    public bool CanMove
    {
        get => canMove;
        set
        {
            if (!value)
            {
                rb.linearVelocity = Vector2.zero;
                movementDirection = Vector2.zero;
                anim.SetFloat("horizontal", 0.0f);
            }
            canMove = value;
        }
    }

    // Dictionary
    public Dictionary dictionary { get; set; }

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(PlayerController)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;

        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        playerCollider = GetComponent<Collider2D>();

        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        if (File.Exists(savePath))
        {
            GameState.LoadPlayerData();
        }
        moveAction     = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    private void Start()
    {

    }

    void Update()
    {
        bool isInteracting  = (context & PlayerContext.Interacting) != 0;
        bool isInInputMode  = (context & PlayerContext.PlayerInput) != 0;
        bool isInMenu       = (context & PlayerContext.Menu) != 0;
        bool useInteractKey = interactAction.WasPerformedThisFrame();
        // Trigger for interact input
        if (!isInteracting && !isInInputMode && !isInMenu && useInteractKey)
        {
            // Prompts listeners to execute their Interact method
            // NOTE: Very dirty
            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            foreach (Interactable interactable in interactables)
            {
                if (interactable.InteractCollider.IsTouching(playerCollider))
                {
                    StartCoroutine(interactable.Interact(this));
                    break;
                }
            }
        }
        else if (currentInteraction.TryGet(out Interactable obj) && (obj.TargetContext & PlayerContext.Dialogue) != 0 && !isInInputMode)
        {
            if (useInteractKey)
            {
                (obj as NpcDialogue).Advance();
            }
        }
        if (canMove)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue.x > 0) // Right movement
        {
            movementDirection = new Vector2(1.0f, 0.0f);
            anim.SetFloat("horizontal", Mathf.Abs(movementDirection.x));
            if (!facingRight && movementDirection.x > 0)
            {
                Flip();
            }
        }
        else if (moveValue.x < 0) // Left movement
        {
            movementDirection = new Vector2(-1.0f, 0.0f);
            anim.SetFloat("horizontal", Mathf.Abs(movementDirection.x));
            if (facingRight && movementDirection.x < 0)
            {
                Flip();
            }
        }
        else // Not moving
        {
            movementDirection = new Vector2(0.0f, 0.0f);
            anim.SetFloat("horizontal", Mathf.Abs(movementDirection.x));
        }
    }


    void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed;
    }

    void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
    }
}
