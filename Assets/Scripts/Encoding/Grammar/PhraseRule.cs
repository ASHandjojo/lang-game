using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

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
        hash = entries[0].GetHashCode();
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
    public readonly bool Equals(PhraseRule rhs) => hash == rhs.hash;

    public override readonly int GetHashCode() => hash;

    public void Dispose()
    {
        entries.Dispose();
        entries = default;

        headIndex = -1;
        hash = ~0;
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
        matchNum = 0;
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

    public readonly bool Evaluate(in PhraseRule rule, in ReadOnlySpan<WordNode> nodes, int position)
    {
        if (Hint.Unlikely(nodes.Length == 0 || position < 0 || position >= nodes.Length)) // NOTE: Watch
        {
            return false;
        }


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