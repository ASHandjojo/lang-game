using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

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
    public WordType phraseType;
    public PhraseRuleManaged[] rules;
}

[BurstCompile]
public struct PhraseRule : IEquatable<PhraseRule>, IDisposable
{
    // S => N  * VP
    // S => NP * VP
    [NativeDisableContainerSafetyRestriction]
    private NativeArray<RuleEntry> entries;
    private int headIndex;
    private int hash; // Precomputes hash, as it is immutable

    public PhraseRule(in ReadOnlySpan<RuleEntry> entriesIn, int headIndex, Allocator allocator)
    {
        Debug.Assert(entriesIn.Length > 0);
        entries = new NativeArray<RuleEntry>(entriesIn.Length, allocator);
        hash    = entries[0].GetHashCode();
        for (int i = 1; i < entriesIn.Length; i++)
        {
            hash = HashCode.Combine(hash, entriesIn[i].GetHashCode());
        }

        Debug.Assert(headIndex >= 0 && headIndex < entriesIn.Length);
        this.headIndex = headIndex;
    }

    public readonly RuleEntry Head => entries[headIndex];

    [BurstDiscard]
    public override readonly bool Equals(object rhs) => rhs is PhraseRule entry && Equals(entry);
    public readonly bool Equals(PhraseRule rhs)      => hash == rhs.hash;

    public override readonly int GetHashCode() => hash;

    public void Dispose()
    {
        entries.Dispose();
        entries = default;

        headIndex = -1;
        hash      = ~0;
    }
}

[BurstCompile]
public struct MemoValue
{
    public int position;
    public int matchNum;

    public MemoValue(int position)
    {
        this.position = position;
        matchNum      = 0;
    }
}

[BurstCompile]
public struct Memoizer : IDisposable
{
    private NativeParallelMultiHashMap<PhraseRule, MemoValue> rules;

    public Memoizer(Allocator allocator)
    {
        rules = new NativeParallelMultiHashMap<PhraseRule, MemoValue>(8, allocator);
    }

    public readonly bool TryGetCached(in PhraseRule rule, int position)
    {
        if (rules.TryGetFirstValue(rule, out MemoValue keyFirst, out var iterator))
        {
            if (keyFirst.position == position)
            {
                return true;
            }
            while (rules.TryGetNextValue(out MemoValue key, ref iterator))
            {
                if (key.position == position)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void Dispose()
    {
        rules.Dispose();
        rules = default;
    }
}

[BurstCompile]
public struct Parser
{
    public NativeArray<WordNode> nodes;

    public void Complete()
    {

    }
}