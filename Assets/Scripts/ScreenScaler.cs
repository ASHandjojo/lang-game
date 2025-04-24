using UnityEngine;

public class ScreenScaler : MonoBehaviour
{
    [Header("Background Scaling")]
    public Transform backgroundRoot;
    private float BACKGROUND_NATIVE_HEIGHT;
    private float BACKGROUND_NATIVE_WIDTH;

    Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        scaleScreen();
    }


    // Scales the background and game objects to the screen size
    private void scaleScreen()
    {
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = mainCamera.aspect * cameraHeight;

        // Adjust background size to fill the screen
        if (backgroundRoot != null)
        {
            BACKGROUND_NATIVE_WIDTH = backgroundRoot.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            BACKGROUND_NATIVE_HEIGHT = backgroundRoot.GetComponent<SpriteRenderer>().sprite.bounds.size.y;

            float bgX = cameraWidth / BACKGROUND_NATIVE_WIDTH;
            float bgY = cameraHeight / BACKGROUND_NATIVE_HEIGHT;
            backgroundRoot.localScale = new Vector3(bgX, bgY, 1f);
        }

        // Adjust world objects to fit screen, 
        int worldLayer = LayerMask.NameToLayer("Default");
        GameObject[] worldObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in worldObjects)
        {
            // Only scale world objects
            if (obj.layer == worldLayer)
            {
                Transform objTransform = obj.transform;
                float referenceHeight = obj.GetComponent<SpriteRenderer>().sprite.bounds.size.y;
                float referenceWidth = obj.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                objTransform.localScale = new Vector3(cameraWidth / referenceWidth, cameraHeight / referenceHeight, 1f);
            }
        }
    }
}
