using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class LanguageTable : MonoBehaviour
{
    [SerializeField] private StandardSignTable signTable;
    [SerializeField] private LigatureSub ligatureSub;

    private Processor processor;

    private static LanguageTable Instance { get; set; }

    public static ReadOnlySpan<StandardSign> StandardSigns => Instance.signTable.entries;
    public static ReadOnlySpan<CompoundSign> CompoundSigns => Instance.ligatureSub.entries;
    public static ref readonly Processor Processor => ref Instance.processor;

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

        processor = new Processor(StandardSigns, CompoundSigns);
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}