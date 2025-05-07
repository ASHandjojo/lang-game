using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
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
        if(Input.GetKeyDown(Keybinds.instance.getIntersKey()))
            {
                // Prompts listeners to execute their Interact method
                Actions.OnInteract?.Invoke(this);
            }
        
        // Trigger Dictionary/Settings menu
        if(Input.GetKeyDown(Keybinds.instance.getDictKey()))
            {
                Actions.OnDictionaryMenuCalled?.Invoke();
            }

        if(Input.GetKeyDown(Keybinds.instance.getSettingsKey()))
            {
                Actions.OnSettingsMenuCalled?.Invoke();
            }

        // Trigger next line of dialogue
        if(Input.GetMouseButtonDown(0))
        {
            Actions.OnClick?.Invoke(this);
        }
            
        // Trigger movement
        if (Input.GetKey(Keybinds.instance.getRightKey()) && Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(0f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getRightKey())) {
            movementDirection = new Vector2(1f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(-1f, 0f);
        } else {
            movementDirection = new Vector2(0f, 0f);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed;
    }


}
