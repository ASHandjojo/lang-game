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

    void Awake()
    {
        interactCollider = GetComponent<Collider2D>();
        Debug.Assert(interactCollider.isTrigger);
    }

    // All will have their own behavior
    public abstract void Interact(PlayerController player);

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
