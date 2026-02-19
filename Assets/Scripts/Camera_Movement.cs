using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Camera_Movement : MonoBehaviour
{
    [SerializeField] private Transform player_position;
    [SerializeField] private Transform terrain_image;

    [SerializeField] private float camera_offset       = 9.0f;
    [SerializeField] private float camera_offset_16x9  = 9.0f;
    [SerializeField] private float camera_offset_16x10 = 8.1f;
    [SerializeField] private float camera_offset_other = 9.75f;

    private float terrainImageLeft;
    private float terrainImageRight;


    void Start()
    {
        terrainImageRight = 9.0f  * terrain_image.localScale.x - camera_offset;
        terrainImageLeft  = -9.0f * terrain_image.localScale.x + camera_offset;
    }

    void Update()
    {
        if (player_position.position.x < terrainImageRight && player_position.position.x > terrainImageLeft)
        {
            transform.position = new Vector3(player_position.position.x, Camera.main.transform.position.y, -10.0f);
        }

        Vector2 resolution = new(Screen.width, Screen.height);
        float ratio = resolution.x / resolution.y;
        if (ratio - 16.0f / 10.0f <= 0.001f)
        {
            camera_offset = camera_offset_16x10;
            terrainImageRight = 9*terrain_image.localScale.x - camera_offset;
            terrainImageLeft = -9*terrain_image.localScale.x + camera_offset;
        }
        else if (ratio - 16.0f / 9.0f <= 0.001f)
        {
            camera_offset = camera_offset_16x9;
            terrainImageRight = 9.0f * terrain_image.localScale.x - camera_offset;
            terrainImageLeft = -9.0f * terrain_image.localScale.x + camera_offset;
        }
        else
        {
            camera_offset = camera_offset_other;
            terrainImageRight = 9.0f * terrain_image.localScale.x - camera_offset;
            terrainImageLeft = -9.0f * terrain_image.localScale.x + camera_offset;
        }
    }
}
