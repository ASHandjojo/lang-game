using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewItem : Interactable
{
    [Header("Sprites")]
    public Sprite open;
    public Sprite closed;
    [SerializeField] public AudioClip clip;

    private SpriteRenderer sr;
    private InteractableSoundHandler sh;
    private bool isOpen = false;

    
    // Generic test item. Plays the sound effect and switches sprites.
    public override void Interact(PlayerController player)
    {
        if(open == null || closed == null)
        {
            Debug.LogWarning("Missing sprite references.");
            return;
        }

        sh.PlaySound(clip);

        sr.sprite = isOpen ? closed : open;

        isOpen = !isOpen;

        // Pick it up
        // Add to player array of items to show they have one?
        // Disable instance from world (unless dropped)?
    }

    private void Start()
    {
        // Initialize components added in Inspector
        sr = GetComponent<SpriteRenderer>();
        sh = GetComponent<InteractableSoundHandler>();

        if(sr == null)
        {
            Debug.LogWarning("Missing Spriterenderer.");
            return;
        }

        if(closed == null)
        {
            Debug.LogWarning("No assigned closed sprite.");
            return;
        }

        // default sprite
        sr.sprite = closed;
    }

}
