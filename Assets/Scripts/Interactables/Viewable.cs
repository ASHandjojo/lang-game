using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Viewable : Interactable
{
    private UIDocument document;
    [SerializeField] private UIDocument hudDocument;
    private bool isZoomed;
    private float timeElapsed;
    [SerializeField] private float transitionTime = 1f;
    [SerializeField] private Texture2D zoomImage;

    private Vector3 cameraPos;



    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void Start()
    {
        document.rootVisualElement.Q("ViewImage").style.backgroundImage = new StyleBackground(zoomImage);
        document.rootVisualElement.style.display = DisplayStyle.None;
        isZoomed = false;
    }

    public override void Interact(PlayerController player)
    {
        Camera mainCamera = Camera.main;
        cameraPos = mainCamera.transform.position;

        if (mainCamera == null) {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        if(isZoomed)
        {
            StartCoroutine(CamDetransition(mainCamera, player));
            document.rootVisualElement.style.display = DisplayStyle.None;
            hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }
        else
        {
            StartCoroutine(CamTransition(mainCamera, player));
            document.rootVisualElement.style.display = DisplayStyle.Flex;
            hudDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        isZoomed = !isZoomed;
        
    }

    IEnumerator CamTransition(Camera mainCamera, PlayerController player)
    {
        Debug.Log("Starting camera pos: (" + mainCamera.transform.position + ")");
        // Disable box collider to prevent further interaction & freeze position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        mainCamera.GetComponent<Camera_Movement>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(transform.position.x, transform.position.y, -10f);
        timeElapsed = 0;

        // Change camera's focus from player to viewable
        while (timeElapsed < transitionTime)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / transitionTime);
            mainCamera.orthographicSize = Mathf.Lerp(5, 1, timeElapsed / transitionTime);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = 1;

        Debug.Log("Ending camera pos: (" + mainCamera.transform.position + ")");

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
    }

    IEnumerator CamDetransition(Camera mainCamera, PlayerController player)
    {
        // Disable box collider to prevent further interaction & position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        timeElapsed = 0;

        // Change camera focus from viewable to player
        while (timeElapsed < transitionTime)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / transitionTime);
            mainCamera.orthographicSize = Mathf.Lerp(1, 5, timeElapsed / transitionTime);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = 5;

        mainCamera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

        Debug.Log("Final camera pos: (" + mainCamera.transform.position + ")");

        mainCamera.GetComponent<Camera_Movement>().enabled = true;

        // Restore movement
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
    }
}
