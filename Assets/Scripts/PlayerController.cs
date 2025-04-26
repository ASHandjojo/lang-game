using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] private float movementSpeed = 2f;

    private Rigidbody2D rb;

    private Vector2 movementDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(Keybinds.instance.getRightKey()) && Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(0f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getRightKey())) {
            movementDirection = new Vector2(1f, 0f);
        } else if (Input.GetKey(Keybinds.instance.getLeftKey())) {
            movementDirection = new Vector2(-1f, 0f);
        } else {
            movementDirection = new Vector2(0f, 0f);
        }
        //movementDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }


    void FixedUpdate()
    {
        rb.linearVelocity = movementDirection * movementSpeed * Time.deltaTime;
    }

}
