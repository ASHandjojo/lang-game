using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]

// Do not reference this class! We're using FMOD now

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
