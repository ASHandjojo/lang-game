using UnityEngine;

public class Interactor : MonoBehaviour
{
    /* How to use the interaction system

    (I'm sorry if this is a terrible way to do it)

    In the Scripts folder, there is a file called IInteractable. It's an interface for all interactables.
    Idrk how interfaces work but basically how I understand it is that all objects that implement the 
    interface must define the methods that are listed in the interface, which currently for IInteractable is only void Interact().
    So every object the mc needs to interact with needs to start with something like:
    public class (object name here) : MonoBehavior, IInteractable
    and then it needs to define Interact() somewhere in the class. I think Interact might need to be public as well.
    If you want to add other functions to a specific interactable i believe you can do that
    If we need *all* interactables to have additional functions then add them to the IInteractable interface

    The object needs to have a BoxCollider2D with the IsTrigger option checked. I don't think it needs to have a RigidBody2D? But I'm stupid so who knows.

    Uh I think thats the basics

    Am I spelling interactable wrong? (pls pls pls let me be spelling it right)

    */

    private IInteractable currentInteractable;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable!=null) {
            currentInteractable.Interact();
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        currentInteractable = other.GetComponent<IInteractable>();
        Debug.Log("here");
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.GetComponent<IInteractable>() == currentInteractable) {
            currentInteractable = null;
        }
    }
}
