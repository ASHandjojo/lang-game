using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[Flags]
public enum PlayerContext
{
    Default,
    Menu,
    Interacting,
    Dialogue,
    PlayerInput
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

    private PlayerContext context = PlayerContext.Default;

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
        // Trigger for interact input
        if (Input.GetKeyDown(Keybinds.Instance.getIntersKey()))
        {
            // Prompts listeners to execute their Interact method
            // NOTE: Very dirty
            Interactable[] interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            foreach (Interactable interactable in interactables)
            {
                if (interactable.InteractCollider.IsTouching(playerCollider))
                {
                    interactable.Interact(this);
                    break;
                }
            }
        }
        
        // Trigger Dictionary/Settings menu
        if (Input.GetKeyDown(Keybinds.Instance.getDictKey()))
        {
            //Actions.OnDictionaryMenuCalled?.Invoke();
        }

        if (Input.GetKeyDown(Keybinds.Instance.getSettingsKey()))
        {
            //Actions.OnSettingsMenuCalled?.Invoke();
        }

        // Trigger next line of dialogue
        if (Input.GetMouseButtonDown(0))
        {
            //Actions.OnClick?.Invoke(this);
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
