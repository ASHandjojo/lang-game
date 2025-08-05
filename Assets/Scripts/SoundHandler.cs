using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
public sealed class SoundHandler : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    public void PlaySoundUI(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}
