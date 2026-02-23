using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Settings : MonoBehaviour
{

    public static Settings Instance { get; private set; }


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(Settings)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;
    }

    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
