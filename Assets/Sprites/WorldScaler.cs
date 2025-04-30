using UnityEngine;

public class WorldScaler : MonoBehaviour
{

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null || sr.sprite == null)
            return;

        // Get native pixel size of sprite
        float pixelsPerUnit = sr.sprite.pixelsPerUnit;
        float spriteWidth = sr.sprite.rect.width / pixelsPerUnit;
        float spriteHeight = sr.sprite.rect.height / pixelsPerUnit;

        // Get screen aspect ratio
        float worldScreenHeight = Camera.main.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * Screen.width / Screen.height;

        // Calculate scale factor that fits the sprite on screen
        float scaleFactor = Mathf.Min(worldScreenWidth / spriteWidth, worldScreenHeight / spriteHeight);

        // Apply uniform scale
        transform.localScale = Vector3.one * scaleFactor;
    }

}


