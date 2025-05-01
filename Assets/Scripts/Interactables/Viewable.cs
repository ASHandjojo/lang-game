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

    private SpriteRenderer worldPromptIcon;
    private Texture2D documentPromptIcon;



    void Awake()
    {
        document = GetComponent<UIDocument>();
        worldPromptIcon = GetComponentsInChildren<SpriteRenderer>(true)[1];
        sh = GetComponent<SoundHandler>();
    }

    private void Start()
    {
        // Initialize UI images, while hiding UI screen until interacted with
        document.rootVisualElement.Q("ViewImage").style.backgroundImage = new StyleBackground(zoomImage);
        document.rootVisualElement.Q("PromptImage").style.backgroundImage = new StyleBackground(Keybinds.instance.getKeyImage(Keybinds.instance.getIntersKey()));
        document.rootVisualElement.style.display = DisplayStyle.None;
        isZoomed = false;
    }

    // Zooms in for a closeup view of the object
    public override void Interact(PlayerController player)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null) {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        sh.PlaySound(interactClip);

        if(isZoomed)
        {
            StartCoroutine(CamDetransition(mainCamera, player));
        }
        else
        {
            StartCoroutine(CamTransition(mainCamera, player));
        }

        isZoomed = !isZoomed;
        
    }

    // Zoom in
    IEnumerator CamTransition(Camera mainCamera, PlayerController player)
    {
        hudDocument.rootVisualElement.style.display = DisplayStyle.None;
        // Disable box collider to prevent further interaction & freeze position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;
        worldPromptIcon.enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        mainCamera.GetComponent<Camera_Movement>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        cameraPos = startPos;

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

        mainCamera.orthographicSize = 1;
        mainCamera.transform.position = endPos;

        document.rootVisualElement.style.display = DisplayStyle.Flex;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
    }

    // Zoom out
    IEnumerator CamDetransition(Camera mainCamera, PlayerController player)
    {
        // Disable box collider to prevent further interaction & position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = cameraPos;
        timeElapsed = 0;

        document.rootVisualElement.style.display = DisplayStyle.None;
        // Change camera focus from viewable to player
        while (timeElapsed < transitionTime)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, timeElapsed / transitionTime);
            mainCamera.orthographicSize = Mathf.Lerp(1, 5, timeElapsed / transitionTime);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.orthographicSize = 5;
        mainCamera.transform.position = endPos;

        mainCamera.GetComponent<Camera_Movement>().enabled = true;

        // Restore movement
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
        worldPromptIcon.enabled = true;

        hudDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }
}
