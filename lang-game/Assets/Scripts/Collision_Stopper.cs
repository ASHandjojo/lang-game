using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision_Stopper : MonoBehaviour
{

    public PlayerController playerController;
    public Camera_Movement camera_movement;

    [SerializeField] private float x_offset;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Contains("Player")) {
            collision.gameObject.GetComponent<PlayerController>().enabled = false;
            Debug.Log("Player Stopped");
        }

        if (collision.gameObject.tag.Contains("Camera_Collider")) {
            collision.gameObject.transform.parent.gameObject.GetComponent<Camera_Movement>().enabled = false;
            Debug.Log("Camera Stopped");
        }
    }


}
