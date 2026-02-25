using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactionPrompt;
    [SerializeField] protected AudioClip interactClip;

    [SerializeField] protected RebindableInput worldPromptInput;

    protected OptionalComponent<SoundHandler> soundHandler;
    protected SpriteRenderer worldPromptIcon;

    protected Collider2D interactCollider;
    public Collider2D InteractCollider => interactCollider;

    public virtual PlayerContext TargetContext { get => PlayerContext.Interacting; }

    protected virtual void Awake()
    {
        interactCollider = GetComponent<Collider2D>();
        Debug.Assert(interactCollider.isTrigger);

        Debug.Assert((TargetContext & PlayerContext.Interacting) != 0,
            $"Invalid context for Interactable. Must include {nameof(PlayerContext.Interacting)} in context tags."
        );

        worldPromptIcon = GetComponentsInChildren<SpriteRenderer>(true)[1];
    }
    protected virtual void Start()
    {
        UpdateWorldPromptIconBinding();
    }

    public IEnumerator Interact(PlayerController player)
    {
        player.currentInteraction.SetNonNull(this);
        player.context = TargetContext;

        yield return InteractLogic(player);

        player.currentInteraction.Unset();
        player.context = PlayerContext.Default;
    }

    // All will have their own behavior
    protected abstract IEnumerator InteractLogic(PlayerController player);

    // Know when Player has entered trigger area => Show prompt & listen for interact key
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            interactionPrompt.SetActive(true);
        }
    }

    // Get rid of Interact key pop-up and stop listening for interaction when Player is not close
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            interactionPrompt.SetActive(false);
        }
    }

    //protected Sprite ConvertToSprite(Texture2D image)
    //{
    //    return Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f));
    //}

    protected virtual void OnEnable()
    {
        InputSystem.onActionChange += HandleActionChange;
    }
    protected virtual void OnDisable()
    {
        InputSystem.onActionChange -= HandleActionChange;
    }

    private void HandleActionChange(object actionOrMap, InputActionChange change)
    {
        // ignore if not a rebinding update
        if (change != InputActionChange.BoundControlsChanged) return;
        UpdateWorldPromptIconBinding();
    }

    private void UpdateWorldPromptIconBinding()
    {
        string controlPath = SettingsMenuEvents.RebindableInputPaths[worldPromptInput]
            .GetBinding().effectivePath;

        if (MenuToggler.BindingIcons.Icons.TryGetValue(controlPath, out Sprite iconSprite))
        {
            worldPromptIcon.sprite = iconSprite;
        }
        else
        {
            Debug.LogWarning("No icon found for control path '" + controlPath + "'");
        }
    }
}
