using System;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

/// <summary>
/// Also known as operators if you like parsing expression grammars (PEGs).
/// </summary>
[Flags]
public enum GrammarProperties : byte
{
    Required  = 1,
    Optional  = 2,
    Repeating = 4 | Optional
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

    [BurstDiscard]
    public override readonly bool Equals(object rhs) => rhs is RuleEntry entry && Equals(rhs);
    public readonly bool Equals(RuleEntry rhs)       => wordType == rhs.wordType && constituent == rhs.constituent && properties == rhs.properties;

    public unsafe override readonly int GetHashCode()
    {
        RuleEntry copy = this;
        return *(int*) &copy;
    }
}

[Serializable]
public struct PhraseRuleManaged
{
    public RuleEntry[] entries;
    public int headIndex;

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
    /// <summary>
    /// What kind of phrase do these rules contribute to.
    /// </summary>
    public WordType phraseType;
    public PhraseRuleManaged[] rules;
}

[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
public struct PhraseRulesUnmanaged : IDisposable
{
    private unsafe byte* data;

    private WordType phraseType;
    private byte     length;
    private ushort   prefixSumOffset;
    private ushort   headIndexOffset;

    private ushort allocator;

    public readonly WordType PhraseType => phraseType;

    public unsafe readonly bool IsValid => data != null && length > 0 && prefixSumOffset > 0;

    private unsafe readonly Span<int> PrefixMut      => new(data, length + 1);
    private unsafe readonly Span<int> HeadIndicesMut => new(data + prefixSumOffset, length);
    private unsafe readonly Span<RuleEntry> RulesMut => new(data + headIndexOffset, PrefixMut[^1]);

    private unsafe readonly Span<RuleEntry> GetRulesMut(int index)
    {
        var prefix = PrefixMut;
        return RulesMut[prefix[index]..prefix[index + 1]];
    }

    public unsafe readonly ReadOnlySpan<RuleEntry> GetRules(int index) => GetRulesMut(index);

    [BurstDiscard]
    public static unsafe PhraseRulesUnmanaged Create(WordType phraseType, in ReadOnlySpan<PhraseRuleManaged> rules, Allocator allocator)
    {
        Debug.Assert(phraseType != WordType.Unknown && phraseType != WordType.TypeCount);
        Debug.Assert(rules.Length > 0);
        int prefixSumBytes = (rules.Length + 1) * sizeof(int);
        int headIndexBytes = rules.Length * sizeof(int);

        PhraseRulesUnmanaged output = new()
        {
            phraseType      = phraseType,
            length          = (byte)   rules.Length,

            prefixSumOffset = (ushort) prefixSumBytes,
            headIndexOffset = (ushort) (prefixSumBytes + headIndexBytes),

            allocator = (ushort) allocator
        };
        int ruleEntryCount = 0;
        foreach (PhraseRuleManaged rule in rules)
        {
            Debug.Assert(rule.entries.Length > 0);
            ruleEntryCount += rule.entries.Length;
        }
        int ruleEntryBytes = ruleEntryCount * sizeof(RuleEntry);

        int totalBytes = prefixSumBytes + ruleEntryBytes + headIndexBytes;
        output.data    = (byte*) UnsafeUtility.MallocTracked(totalBytes, UnsafeUtility.AlignOf<RuleEntry>(), allocator, 0);
        var prefixMut  = output.PrefixMut;
        // Calculating prefix sum
        for (int i = 0, value = 0; i < rules.Length; i++)
        {
            prefixMut[i] = value;
            value       += rules[i].entries.Length;
        }
        prefixMut[^1] = ruleEntryCount;

        var headIndices = output.HeadIndicesMut;
        // Init rules
        for (int i = 0; i < rules.Length; i++)
        {
            var ruleSpan = output.GetRulesMut(i);
            rules[i].entries.CopyTo(ruleSpan);

            headIndices[i] = rules[i].headIndex;
        }

        return output;
    }

    public unsafe void Dispose()
    {
        UnsafeUtility.FreeTracked(data, (Allocator) allocator);
        data      = null;
        allocator = default;

        length          = 0;
        prefixSumOffset = 0;
    }
}