using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D)),
    RequireComponent(typeof(Animator))]
public sealed class PlayerController : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private float movementSpeed = 2.0f;

    private Rigidbody2D rb;

    private Vector2 movementDirection;
    private bool facingRight = true, canMove = true;

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
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(PlayerController)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;
    }
    
    void Update()
    {
        // Trigger for interact input
        if (Input.GetKeyDown(Keybinds.instance.getIntersKey()))
        {
            // Prompts listeners to execute their Interact method
            Actions.OnInteract?.Invoke(this);
        }
        
        // Trigger Dictionary/Settings menu
        if (Input.GetKeyDown(Keybinds.instance.getDictKey()))
        {
            Actions.OnDictionaryMenuCalled?.Invoke();
        }

        if (Input.GetKeyDown(Keybinds.instance.getSettingsKey()))
        {
            Actions.OnSettingsMenuCalled?.Invoke();
        }

        // Trigger next line of dialogue
        if (Input.GetMouseButtonDown(0))
        {
            Actions.OnClick?.Invoke(this);
        }

        if (canMove)
        {
            Move();
        }
    }

    private void Move()
    {
        if (Input.GetKey(Keybinds.instance.getRightKey()) && !Input.GetKey(Keybinds.instance.getLeftKey())) // Right movement
        {
            movementDirection = new Vector2(1.0f, 0.0f);
            anim.SetFloat("horizontal", Mathf.Abs(movementDirection.x));
            if (!facingRight && movementDirection.x > 0)
            {
                Flip();
            }
        }
        else if (Input.GetKey(Keybinds.instance.getLeftKey()) && !Input.GetKey(Keybinds.instance.getRightKey())) // Left movement
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
