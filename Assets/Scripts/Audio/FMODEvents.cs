using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Test Sound")]
    [field: SerializeField] public EventReference testSound { get; private set;}

    [field: Header("Train BGM")]
    [field: SerializeField] public EventReference trainBGM { get; private set;}

    [field: Header("Train Ambience")]
    [field: SerializeField] public EventReference trainAmbience { get; private set;}

    [field: Header("Marketplace Ambience")]
    [field: SerializeField] public EventReference marketplaceAmbience { get; private set;}

    [field: Header("Keyboard SFX")]
    [field: SerializeField] public EventReference keyboardSFX { get; private set;}

    [field: Header("Dialogue Forward SFX")]
    [field: SerializeField] public EventReference dialogueForwardSFX { get; private set;}

    [field: Header("Dictionary Open SFX")]
    [field: SerializeField] public EventReference dictionaryOpenSFX { get; private set;}

    [field: Header("Dictionary Close SFX")]
    [field: SerializeField] public EventReference dictionaryCloseSFX { get; private set;}

    [field: Header("Page Turn SFX")]
    [field: SerializeField] public EventReference pageTurnSFX { get; private set;}

    [field: Header("Pass Language Check SFX")]
    [field: SerializeField] public EventReference passLangCheckSFX { get; private set;}

    [field: Header("Fail Language Check SFX")]
    [field: SerializeField] public EventReference failLangCheckSFX { get; private set;}

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Audio Manager in the scene.");
        }
        instance = this;
    }

}
