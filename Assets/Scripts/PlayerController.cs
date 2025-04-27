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
            
        if (Input.GetKey(Keybinds.instance.getRightKey()) && Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(0f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getRightKey())) {
            movementDirection = new Vector2(1f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(-1f, 0f);
        } else {
            movementDirection = new Vector2(0f, 0f);
        }
        //movementDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }


    void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed;
    }

}
