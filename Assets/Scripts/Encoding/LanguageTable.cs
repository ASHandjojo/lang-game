using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

[DisallowMultipleComponent]
public sealed class LanguageTable : MonoBehaviour
{
    [SerializeField] private StandardSignTable signTable;
    [SerializeField] private LigatureSub ligatureSub;

    private static LanguageTable Instance { get; set; }

    public static StandardSign[] StandardSigns => Instance.signTable.entries;
    public static CompoundSign[] CompoundSigns => Instance.ligatureSub.entries;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(LanguageTable)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;
    }

}