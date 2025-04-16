using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSoundHandler : MonoBehaviour
{

    private AudioSource audioSource;

    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

   public void PlaySound(AudioClip clip)
   {
    if(clip != null)
    {
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }
   }

   public void PlaySoundUI(AudioClip clip)
   {
    if(clip != null)
    {
        audioSource.PlayOneShot(clip);
    }
   }

   
}
