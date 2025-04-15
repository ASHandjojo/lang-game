using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } // Allows access to PlayerController everywhere else
    [SerializeField] private float movementSpeed = 2f;

    private Rigidbody2D rb;

    private Vector2 movementDirection;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    
    void Update()
    {
        // Trigger for interact input
        if(Input.GetKeyDown(KeyCode.E))
            {

                // Prompts any listeners to execute their Interact method
                Actions.OnInteract?.Invoke(this);
            }
        movementDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }


    void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed;
    }

}
