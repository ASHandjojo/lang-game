using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    // Scale the background to the screen
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = -10;

        if (sr == null || sr.sprite == null)
            return;

        // Get sprite world size
        float width  = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;

        // Get screen aspect ratio
        float worldScreenHeight = Camera.main.orthographicSize * 2f;

        // Fill screen
        transform.localScale = new Vector3(
            1,
            worldScreenHeight / height,
            1);
    }
}
