using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;

using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class LanguageTable : MonoBehaviour
{
    [SerializeField] private StandardSignTable signTable;
    [SerializeField] private LigatureSub ligatureSub;
    [SerializeField] private SynonymSystem synonymSystem;

    private PhoneticProcessor processor;

    private static LanguageTable Instance { get; set; }

    public static ReadOnlySpan<StandardSign> StandardSigns => Instance.signTable.entries;
    public static ReadOnlySpan<CompoundSign> CompoundSigns => Instance.ligatureSub.entries;
    public static ReadOnlySpan<Synonyms> Synonyms => Instance.synonymSystem.synonymLists;
    public static ref readonly PhoneticProcessor PhoneticProcessor => ref Instance.processor;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(LanguageTable)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }
    
        foreach (var synonym in Synonyms)
        {
            Debug.Assert((int)synonym.wordType <= 3, "Must be N, Adj, V, or Adv");
        }

        DontDestroyOnLoad(this);
        Instance = this;

        processor = new PhoneticProcessor(StandardSigns, CompoundSigns, Allocator.Persistent);
    }

    void OnDestroy()
    {
        processor.Dispose();
    }
}