using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class Hud : MonoBehaviour
{
    [SerializeField] private GameObject dictionaryCanvas;
    [SerializeField] private GameObject hudCanvas;

    PlayerController player;

    [Header("Animators")]
    [SerializeField] private Animator dictionaryAnimator;
    [SerializeField] private Animator backgroundAnimator;

    [Header("Audio")]
    [SerializeField] public AudioClip openClip;
    [SerializeField] public AudioClip closeClip;
    private InteractableSoundHandler sh;
    

    private void Awake()
    {
        player = PlayerController.Instance;
        sh = GetComponent<InteractableSoundHandler>();
    }

    public void OnDictionaryButtonClicked()
    {
        sh.PlaySoundUI(openClip);

        // Disable box collider to prevent further interaction & freeze position to prevent movement
        player.GetComponent<BoxCollider2D>().enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
    }

    public void OnBackButtonClicked()
    {
        StartCoroutine(ExitDictionary());
    }

    IEnumerator ExitDictionary()
    {
        // Play transition animations
        sh.PlaySoundUI(closeClip);
        dictionaryAnimator.SetTrigger("ExitDictionary");
        backgroundAnimator.SetTrigger("ExitDictionary");

        // Wait for animations to finish
        yield return new WaitForSeconds(1.2f);
        
        // Restore movement
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // Re-enable box collider to restore interaction
        player.GetComponent<BoxCollider2D>().enabled = true;

        // Switch UI
        hudCanvas.SetActive(true);
        dictionaryCanvas.SetActive(false);
    }

}