using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;

using UnityEngine;

[Flags]
public enum GrammarProperties : byte
{
    Required  = 1,
    Optional  = 2,
    Repeating = 4
}

[BurstCompile]
public static class GrammarPropsExtMethods
{
    public static bool IsValid(this GrammarProperties props)
    {
        // Uses XOR, because one or the other has to be here.
        return ((props & GrammarProperties.Required) ^ (props & GrammarProperties.Optional)) != 0;
    }
}

public enum ConstituentType : byte
{
    Word   = 0,
    Phrase = 1,
}

[Serializable, BurstCompile, StructLayout(LayoutKind.Sequential, Size = 4)]
public struct RuleEntry : IEquatable<RuleEntry>
{
    public WordType          wordType;    // For either phrase type or word type
    public ConstituentType   constituent; // This determines whether the rule entry is a word or phrase
    public GrammarProperties properties;

    public readonly bool Equals(RuleEntry rhs) => constituent == rhs.constituent && properties == rhs.properties;
}

[Serializable]
public struct PhraseRuleManaged
{
    public RuleEntry[] entries;

    public readonly bool IsValid()
    {
        if (entries.Length == 0) // Not valid if empty
        {
            return false;
        }
        int requiredCount = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (!entries[i].properties.IsValid()) // Has to have valid properties
            {
                return false;
            }
            requiredCount += ((entries[i].properties & GrammarProperties.Required) != 0).CastAsInt32();
        }
        return requiredCount > 0; // Must have at least one required property
    }
}

[Serializable]
public struct PhraseRulesManaged
{
    public WordType phraseType;
    public PhraseRuleManaged[] rules;
}

[BurstCompile]
public struct PhraseRule
{
    // S => N * VP
    // S => NP * VP
}