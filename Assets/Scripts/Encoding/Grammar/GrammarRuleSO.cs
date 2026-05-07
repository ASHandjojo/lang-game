using System;

using UnityEngine;

[CreateAssetMenu(menuName = "Linguistics/Grammar Rules")]
public sealed class GrammarRuleSO : ScriptableObject
{
    [SerializeField] private PhraseRulesManaged[] rules;

    public ReadOnlySpan<PhraseRulesManaged> Rules => rules;
}