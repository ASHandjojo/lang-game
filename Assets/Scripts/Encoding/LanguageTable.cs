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
    [SerializeField] private LigatureSub       ligatureSub;
    [SerializeField] private GrammarRuleSO     grammarRules;

    private PhoneticProcessor processor;
    // NOTE: May be temp location
    private NativeArray<PhraseRulesUnmanaged> phraseRules;

    private static LanguageTable Instance { get; set; }

    public static ReadOnlySpan<StandardSign> StandardSigns         => Instance.signTable.entries;
    public static ReadOnlySpan<CompoundSign> CompoundSigns         => Instance.ligatureSub.entries;
    public static ref readonly PhoneticProcessor PhoneticProcessor => ref Instance.processor;

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

        processor   = PhoneticProcessor.Create(StandardSigns, CompoundSigns, Allocator.Persistent);
        phraseRules = new NativeArray<PhraseRulesUnmanaged>(grammarRules.Rules.Length, Allocator.Persistent);

        for (int i = 0; i < phraseRules.Length; i++)
        {
            phraseRules[i] = PhraseRulesUnmanaged.Create(grammarRules.Rules[i].phraseType, grammarRules.Rules[i].rules, Allocator.Persistent);
        }
    }

    void OnDestroy()
    {
        processor.Dispose();
        processor = default;

        for (int i = 0; i < phraseRules.Length; i++)
        {
            phraseRules[i].Dispose();
        }
        phraseRules.Dispose();
        phraseRules = default;
    }
}