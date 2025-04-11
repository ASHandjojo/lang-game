using UnityEngine;

public class Interactor : MonoBehaviour
{

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
        currentInteractable = GetComponent<IInteractable>();
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.GetComponent<IInteractable>() == currentInteractable) {
            currentInteractable = null;
        }
    }
}
