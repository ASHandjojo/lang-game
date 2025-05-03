using UnityEngine;

public class GenericInteractor : Interactable
{
    public override void Interact(PlayerController player)
    {
        Debug.Log("interacted");
    }
}
