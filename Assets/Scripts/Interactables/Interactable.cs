using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactionPrompt;

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

    // Get rid of E pop-up and stop listening for interaction when Player is not close
    private void OnTriggerExit2D(Collider2D collider)
    {
        if(collider.CompareTag("Player"))
        {
            DenotifyInteractable();
            Actions.OnInteract -= Interact;
        }
    }

    // E prompt shows up
    protected virtual void NotifyInteractable()
    {
        interactionPrompt.SetActive(true);
    }

    // E prompt disappears
    protected virtual void DenotifyInteractable()
    {
        interactionPrompt.SetActive(false);
    }

}
