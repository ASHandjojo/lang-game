using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision_Stopper : MonoBehaviour
{

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
