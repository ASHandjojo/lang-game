using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using static Unity.Collections.LowLevel.Unsafe.NativeSliceUnsafeUtility;

using UnityEngine;

/// <summary>
/// Contains a list of base rules, a head index-and a precomputed hash.
/// </summary>
[BurstCompile]
public struct PhraseRule : IEquatable<PhraseRule>
{
    // S => N  * VP
    // S => NP * VP
    [NativeDisableContainerSafetyRestriction]
    private NativeSlice<RuleEntry> entries;
    private int headIndex;
    private int hash; // Precomputes hash, as it is immutable

    public unsafe PhraseRule(in ReadOnlySpan<RuleEntry> entriesIn, int headIndex)
    {
        Debug.Assert(entriesIn.Length > 0, "Rules input must not be empty!");
        fixed (RuleEntry* ptr = entriesIn)
        {
            entries = ConvertExistingDataToNativeSlice<RuleEntry>(ptr, sizeof(RuleEntry), entriesIn.Length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SetAtomicSafetyHandle(ref entries, AtomicSafetyHandle.Create());
#endif
        }
        hash = entries[0].GetHashCode();
        for (int i = 1; i < entries.Length; i++)
        {
            hash = HashCode.Combine(hash, entriesIn[i].GetHashCode());
        }

        Debug.Assert(headIndex >= 0 && headIndex < entriesIn.Length);
        this.headIndex = headIndex;
    }

    public unsafe readonly ReadOnlySpan<RuleEntry> Rules => new(entries.GetUnsafeReadOnlyPtr(), entries.Length);

    public readonly RuleEntry Head => entries[headIndex];
    public readonly RuleEntry Last => entries[^1];

    public readonly RuleEntry this[int index]
    {
        get => entries[index];
    }

    [BurstDiscard]
    public override readonly bool Equals(object rhs) => rhs is PhraseRule entry && Equals(entry);
    public readonly bool Equals(PhraseRule rhs)      => hash == rhs.hash;

    public override readonly int GetHashCode() => hash;
}

[BurstCompile]
public struct MemoValue
{
    public int position;
    public int matchNum;

    public MemoValue(int position) : this(position, 0) { }
    public MemoValue(int position, int matchNum)
    {
        this.position = position;
        this.matchNum = matchNum;
    }

    public static MemoValue Failed(int position) => new(position, -1);

    public readonly bool HasFailed => matchNum == -1;
}

public enum MemoizeStatus : byte
{
    /// <summary>
    /// Flag for successful, properly formed parse; whether cache hit or evaluation.
    /// </summary>
    Successful = 1,

    CacheHit = 2,
    Evaluate = 4,
    /// <summary>
    /// Valid parse but not correct.
    /// </summary>
    FailParse = 8,
    /// <summary>
    /// Actual malformed parsing.
    /// </summary>
    InvalidParse = 16
}

[BurstCompile]
public static class MemoizeStatusExtMethods
{
    public static FixedString64Bytes ToFixedString(this MemoizeStatus status)
    {
        if (status == MemoizeStatus.InvalidParse)
        {
           return "Invalid Parse";
        }
        if ((status & MemoizeStatus.Successful) != 0)
        {
            return $"Successful, {(status == MemoizeStatus.CacheHit ? "Cache Hit" : "Evaluate")}";
        }
        if ((status & MemoizeStatus.InvalidParse) != 0)
        {
            return $"Invalid Parse, {(status == MemoizeStatus.CacheHit ? "Cache Hit" : "Evaluate")}";
        }
        throw new NotImplementedException();
    }
}

[BurstCompile]
public struct Memoizer : IDisposable
{
    // NOTE: Maybe have to change to RuleEntry
    private NativeParallelMultiHashMap<PhraseRule, MemoValue> rules;

    public Memoizer(Allocator allocator)
    {
        rules = new NativeParallelMultiHashMap<PhraseRule, MemoValue>(8, allocator);
    }

    public readonly bool TryGetCached(in PhraseRule rule, int position, out int elementNum)
    {
        if (rules.TryGetFirstValue(rule, out MemoValue keyFirst, out var iterator))
        {
            if (keyFirst.position == position)
            {
                elementNum = keyFirst.matchNum;
                return true;
            }
            while (rules.TryGetNextValue(out MemoValue key, ref iterator))
            {
                if (key.position == position)
                {
                    elementNum = key.matchNum;
                    return true;
                }
            }
        }
        elementNum = 0;
        return false;
    }

    public void AddOrModify(in PhraseRule rule, int position, int elementNum)
    {
        if (rules.TryGetFirstValue(rule, out MemoValue keyFirst, out var iterator))
        {
            if (keyFirst.position == position)
            {
                rules.SetValue(new MemoValue(position, elementNum), iterator);
            }
            while (rules.TryGetNextValue(out MemoValue key, ref iterator))
            {
                if (key.position == position)
                {
                    rules.SetValue(new MemoValue(position, elementNum), iterator);
                }
            }
        }
        // Otherwise, add rule
        rules.Add(rule, new MemoValue(position, elementNum));
    }

    public MemoizeStatus Process(in PhraseRule phraseRule, in ReadOnlySpan<WordNode> nodes, int position, out MemoValue value)
    {
        bool isCached = TryGetCached(phraseRule, position, out int elementNum);
        if (isCached)
        {
            value      = new MemoValue(position, elementNum);
            var status = value.HasFailed ? MemoizeStatus.FailParse : MemoizeStatus.Successful;
            return status | MemoizeStatus.CacheHit;
        }
        value.position   = position;
        bool parseStatus = Evaluate(phraseRule, nodes, position, out value.matchNum);
        if (!parseStatus) // For failed parse
        {
            return MemoizeStatus.InvalidParse;
        }
        else
        {
            var status = value.HasFailed ? MemoizeStatus.FailParse : MemoizeStatus.Successful;
            return status | MemoizeStatus.Evaluate;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Whether the parse was valid or not. Failed but properly formed parsing still returns true.</returns>
    public bool Evaluate(in PhraseRule phraseRule, in ReadOnlySpan<WordNode> nodes, int position, out int elementNum)
    {
        // Early exit attempt, allows for evaluation to change
        if (Hint.Unlikely(nodes.Length == 0 || position < 0 || position >= nodes.Length)) // NOTE: Watch
        {
            elementNum = -1; // Invalid parse
            return false;
        }
        foreach (RuleEntry choice in phraseRule.Rules) // Iterate through every choice per rule
        {
            for (int i = position; i < nodes.Length;) // Start from leftmost position specified
            {
                // Required Case
                if ((choice.properties & GrammarProperties.Required) != 0)
                {
                    // Terminal
                    if (choice.constituent == ConstituentType.Word) // NOTE: Check
                    {
                        // Checks valid part of speech (PoS)
                        if (nodes[i].WordType == choice.wordType) // NOTE: Check
                        {
                            i++;
                        }
                        else
                        {
                            elementNum = -1; // Valid parse but exit
                            return true;
                        }
                    }
                    // Non-terminal
                    else
                    {
                        // Non-terminal rule logic
                        var status = Process(phraseRule, nodes, position: i, out MemoValue memoValue);
                        elementNum = memoValue.matchNum;
                        if (status == MemoizeStatus.InvalidParse)
                        {
                            return false; // Is fine, as this should be set to -1
                        }
                        AddOrModify(phraseRule, position, elementNum);
                        if ((status & MemoizeStatus.FailParse) != 0)
                        {
                            return true; // Is fine, as this should be set to -1
                        }
                        i += memoValue.matchNum;
                    }
                    // Checking if matching symbol is last
                    if (i == nodes.Length - 1)
                    {
                        elementNum = i;
                        return true;
                    }
                }
            }
        }
        // Ran out, did not find match at all
        elementNum = -1;
        return true;
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