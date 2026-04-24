using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Test Sound")]
    [field: SerializeField] public EventReference testSound { get; private set;}

    [field: Header("Music")]
    [field: SerializeField] public EventReference testBGM { get; private set;}

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
