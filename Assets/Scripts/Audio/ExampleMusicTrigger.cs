using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [Header("BGM Value")]
    [SerializeField] private BGMValue bgmValue;  

    void Update()
    {
        // Set the parameter value every frame
        AudioManager.instance.SetMusic(bgmValue);
    }
}
