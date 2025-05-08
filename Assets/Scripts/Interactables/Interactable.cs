using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactionPrompt;
    protected SoundHandler sh;
    [SerializeField] protected AudioClip interactClip;
    protected SpriteRenderer worldPromptIcon;
    protected Texture2D keybindIcon;

    // All will have their own behavior
    public abstract void Interact(PlayerController player);

    private void Reset()
    {
        // Make sure all Interactables have a trigger type collider
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Know when Player has entered trigger area => Show prompt & listen for interact key
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.CompareTag("Player"))
        {
            NotifyInteractable();
            Actions.OnInteract += Interact;
        }
    }

    // Get rid of Interact key pop-up and stop listening for interaction when Player is not close
    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider.CompareTag("Player"))
        {
            DenotifyInteractable();
            Actions.OnInteract -= Interact;
        }
    }

    // Interact key prompt shows up
    protected virtual void NotifyInteractable()
    {
        interactionPrompt.SetActive(true);
    }

    // Interact key prompt disappears
    protected virtual void DenotifyInteractable()
    {
        interactionPrompt.SetActive(false);
    }

    protected Sprite ConvertToSprite(Texture2D image)
    {
        return Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2(0.5f, 0.5f));
    }
}
