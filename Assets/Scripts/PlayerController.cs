using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[Flags]
public enum PlayerContext : int
{
    Default     = 0,
    Menu        = 1,
    Interacting = 2,
    Dialogue    = 4,
    PlayerInput = 8
}

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D)),
    RequireComponent(typeof(Animator))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2.0f;

    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

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
    }
    
    void Update()
    {
        bool isInteracting  = (context & PlayerContext.Interacting) != 0;
        bool isInInputMode  = (context & PlayerContext.PlayerInput) != 0;
        bool useInteractKey = Input.GetKeyDown(Keybinds.Instance.getIntersKey());
        // Trigger for interact input
        if (!isInteracting && !isInInputMode && useInteractKey)
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
            if (useInteractKey || Input.GetMouseButtonDown(0))
            {
                (obj as NpcDialogue).Advance();
            }
        }
        else if (Input.GetKeyDown(Keybinds.Instance.getDictKey()))
        {
            var events = FindFirstObjectByType<GameHUDEvents>(); // NOTE: Very dirty
            events.OpenDictionary();
        }
        else if (Input.GetKeyDown(Keybinds.Instance.getSettingsKey()))
        {
            var events = FindFirstObjectByType<GameHUDEvents>(); // NOTE: Very dirty
            events.OpenSettings();
        }

        if (canMove)
        {
            Move();
        }
    }

    private void Move()
    {
        if (Input.GetKey(Keybinds.Instance.getRightKey()) && !Input.GetKey(Keybinds.Instance.getLeftKey())) // Right movement
        {
            movementDirection = new Vector2(1.0f, 0.0f);
            anim.SetFloat("horizontal", Mathf.Abs(movementDirection.x));
            if (!facingRight && movementDirection.x > 0)
            {
                Flip();
            }
        }
        else if (Input.GetKey(Keybinds.Instance.getLeftKey()) && !Input.GetKey(Keybinds.Instance.getRightKey())) // Left movement
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
