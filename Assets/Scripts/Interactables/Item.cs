using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Item : Interactable
{
    [Header("Sprites")]
    public Sprite open;
    public Sprite closed;

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    void Start()
    {
        // Initialize components added in Inspector
        spriteRenderer = GetComponent<SpriteRenderer>();
        soundHandler   = GetComponent<SoundHandler>();

        Debug.Assert(open != null && closed != null);

        // Default sprite
        spriteRenderer.sprite = closed;
    }

    // Generic test item. Plays the sound effect and switches sprites.
    protected override IEnumerator InteractLogic(PlayerController player)
    {
        if (soundHandler.TryGet(out SoundHandler sh))
        {
            sh.PlaySound(interactClip);
        }

        spriteRenderer.sprite = isOpen ? closed : open;
        isOpen = !isOpen;

        // Pick it up
        // Add to player array of items to show they have one?
        // Disable instance from world (unless dropped)?
        return null;
    }
}
