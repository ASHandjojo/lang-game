using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactionPrompt;
    [SerializeField] protected AudioClip interactClip;

    protected OptionalComponent<SoundHandler> soundHandler;
    protected SpriteRenderer worldPromptIcon;
    protected Texture2D keybindIcon;

    protected Collider2D interactCollider;
    public Collider2D InteractCollider => interactCollider;

    public virtual PlayerContext TargetContext { get => PlayerContext.Interacting; }

    void Awake()
    {
        interactCollider = GetComponent<Collider2D>();
        Debug.Assert(interactCollider.isTrigger);

        Debug.Assert((TargetContext & PlayerContext.Interacting) != 0,
            $"Invalid context for Interactable. Must include {nameof(PlayerContext.Interacting)} in context tags."
        );
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

    protected Sprite ConvertToSprite(Texture2D image)
    {
        return Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f));
    }
}
