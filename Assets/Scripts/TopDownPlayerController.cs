using System;
using System.IO;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public sealed class TopDownPlayerController : MonoBehaviour
{
    Rigidbody2D body;

    private InputAction moveAction;
    private Vector2 movementDirection;

    public float runSpeed = 20.0f;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        moveAction = InputSystem.actions.FindActionMap("TopDown").FindAction("Move");
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue.x > 0) // Right movement
        {
            movementDirection = new Vector2(1.0f, 0.0f);
        }
        else if (moveValue.x < 0) // Left movement
        {
            movementDirection = new Vector2(-1.0f, 0.0f);
        }
        else // Not moving horizontally
        {
            movementDirection = new Vector2(0.0f, 0.0f);
        }

        if (moveValue.y > 0) // Up movement
        {
            movementDirection = movementDirection + new Vector2(0.0f, 1.0f);
        }
        else if (moveValue.y < 0) // Down movement
        {
            movementDirection = movementDirection + new Vector2(0.0f, -1.0f);
        }
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movementDirection * runSpeed;
    }
}
