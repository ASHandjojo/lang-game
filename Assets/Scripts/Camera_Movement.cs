using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Camera_Movement : MonoBehaviour
{
    [SerializeField] private Transform player_position;
    [SerializeField] private Transform terrain_image;

    [SerializeField] private float camera_offset = 9f;
    [SerializeField] private float camera_offset_16x9 = 9f;
    [SerializeField] private float camera_offset_16x10 = 8.1f;
    [SerializeField] private float camera_offset_other = 9.75f;

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

        Vector2 resolution = new Vector2(Screen.width, Screen.height);
        float ratio = resolution.x/resolution.y;
        if (ratio - 16f/10f <= 0.001) {
            camera_offset = camera_offset_16x10;
            terrain_image_right = 9*terrain_image.localScale.x - camera_offset;
            terrain_image_left = -9*terrain_image.localScale.x + camera_offset;
        } else if (ratio - 16f/9f <= 0.001) {
            camera_offset = camera_offset_16x9;
            terrain_image_right = 9*terrain_image.localScale.x - camera_offset;
            terrain_image_left = -9*terrain_image.localScale.x + camera_offset;
        } else {
            camera_offset = camera_offset_other;
            terrain_image_right = 9*terrain_image.localScale.x - camera_offset;
            terrain_image_left = -9*terrain_image.localScale.x + camera_offset;
        }
    }
}
