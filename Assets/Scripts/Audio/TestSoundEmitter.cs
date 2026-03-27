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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Update()
    {
        bool useInteractKey = interactAction.WasPerformedThisFrame();
        // Triggers once the moment the Space bar is pressed down
        if (useInteractKey)
        {
            Debug.Log("Sound played");
            AudioManager.instance.PlayOneShot(FMODEvents.instance.testSound, this.transform.position);
        }
    }
}
