using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class Viewable : Interactable
{
    private UIDocument document;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private float transitionTime = 1f;
    [SerializeField] private Texture2D zoomImage;

    private bool isZoomed;
    private float timeElapsed;

    private Vector3 cameraPos;

    void Start()
    {
        document        = GetComponent<UIDocument>();
        worldPromptIcon = GetComponentsInChildren<SpriteRenderer>(true)[1];
        keybindIcon     = Keybinds.Instance.getKeyImage(Keybinds.Instance.getIntersKey());

        soundHandler = GetComponent<SoundHandler>();

        // Initialize UI images, while hiding UI screen until interacted with
        document.rootVisualElement.Q("ViewImage").style.backgroundImage   = new StyleBackground(zoomImage);
        document.rootVisualElement.Q("PromptImage").style.backgroundImage = new StyleBackground(keybindIcon);

        worldPromptIcon.sprite = ConvertToSprite(keybindIcon);
        document.rootVisualElement.style.visibility = Visibility.Hidden;
        isZoomed = false;
    }

    // Zooms in for a closeup view of the object
    public override void Interact(PlayerController player)
    {
        Camera mainCamera = Camera.main;
        if (soundHandler.TryGet(out SoundHandler sh))
        {
            sh.PlaySound(interactClip);
        }

        if (isZoomed)
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
    private IEnumerator CamTransition(Camera mainCamera, PlayerController player)
    {
        hudDocument.rootVisualElement.style.visibility = Visibility.Hidden;
        worldPromptIcon.enabled = false;

        // Freeze Movement
        player.CanMove = false;
        mainCamera.GetComponent<Camera_Movement>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        cameraPos        = startPos;

        Vector3 endPos = new(transform.position.x, transform.position.y, -10.0f);
        timeElapsed    = 0.0f;

        // Change camera's focus from player to viewable
        while (timeElapsed < transitionTime)
        {
            float fac = timeElapsed / transitionTime;

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, fac);
            mainCamera.orthographicSize   = Mathf.Lerp(5.0f, 1.0f, fac);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.orthographicSize   = 1.0f;
        mainCamera.transform.position = endPos;

        document.rootVisualElement.style.visibility = Visibility.Visible;

        // Restore Movement
        player.CanMove = true;
    }

    // Zoom out
    IEnumerator CamDetransition(Camera mainCamera, PlayerController player)
    {
        // Disable box collider to prevent further interaction & position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos   = cameraPos;
        timeElapsed      = 0.0f;

        document.rootVisualElement.style.visibility = Visibility.Hidden;
        // Change camera focus from viewable to player
        while (timeElapsed < transitionTime)
        {
            float fac = timeElapsed / transitionTime;

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, fac);
            mainCamera.orthographicSize   = Mathf.Lerp(1.0f, 5.0f, fac);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        mainCamera.orthographicSize   = 5.0f;
        mainCamera.transform.position = endPos;

        mainCamera.GetComponent<Camera_Movement>().enabled = true;

        // Restore movement
        Rigidbody2D rb  = player.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;
        worldPromptIcon.enabled = true;

        hudDocument.rootVisualElement.style.visibility = Visibility.Visible;
    }
}
