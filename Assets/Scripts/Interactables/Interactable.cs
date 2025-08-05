using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Avoids expensive null checks for Unity objects. Only checks on assignment.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class OptionalComponent<T> where T : Object
{
    private T obj;
    private bool isInitialized = false;

    private OptionalComponent() { }

    public OptionalComponent(T obj) => SetObject(obj);

    public void SetObject(T objIn)
    {
        obj = objIn;
        isInitialized = objIn != null;
    }

    public bool TryGet(out T objectOut)
    {
        objectOut = obj;
        return isInitialized;
    }

    public static implicit operator OptionalComponent<T>(T obj)
    {
        return new OptionalComponent<T>(obj);
    }
}

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactionPrompt;
    [SerializeField] protected AudioClip interactClip;

    protected OptionalComponent<SoundHandler> soundHandler;
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
        if (collider.CompareTag("Player"))
        {
            NotifyInteractable();
            Actions.OnInteract += Interact;
        }
    }

    // Get rid of Interact key pop-up and stop listening for interaction when Player is not close
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
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
