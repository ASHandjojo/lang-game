using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundEmitter : MonoBehaviour
{

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
    }
    private void Update()
    {
        bool useInteractKey = interactAction.WasPerformedThisFrame();
        // Triggers once the moment "K" key is pressed down
        if (useInteractKey)
        {
            Debug.Log("Sound played");
            AudioManager.instance.PlayOneShot(FMODEvents.instance.testSound, this.transform.position);
        }
    }
}
