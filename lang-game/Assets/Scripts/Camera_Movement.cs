using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Camera_Movement : MonoBehaviour
{
    [SerializeField] private Transform player_position;
    [SerializeField] private Transform terrain_image;

    [SerializeField] private float camera_offset = 9f;

    private float terrain_image_right;
    private float terrain_image_left;
    // Start is called before the first frame update
    void Start()
    {
        terrain_image_right = 9*terrain_image.localScale.x - camera_offset;
        terrain_image_left = -9*terrain_image.localScale.x + camera_offset;
    }

    // Update is called once per frame
    void Update()
    {
        if (player_position.position.x < terrain_image_right && player_position.position.x > terrain_image_left) {
          transform.position = new Vector3(player_position.position.x, player_position.position.y, -10);
        }
    }
}
