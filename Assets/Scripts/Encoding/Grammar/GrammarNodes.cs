using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

/// <summary>
/// Gives part of speech to a given Unicode representation of a word (terminal symbol).
/// </summary>
[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
public struct WordNode
{
    [NativeDisableUnsafePtrRestriction]
    private unsafe ushort* ptr;
    private ushort         length;

    private int wordIndex;

    private WordType type;

    public unsafe readonly bool IsValid => ptr != null && length > 0 && wordIndex >= 0;
    public readonly WordType WordType   => type;
    public readonly int WordIndex       => wordIndex;

    public static WordNode Unknown => new()
    {
        type      = WordType.Unknown,
        wordIndex = -1
    };

    [BurstDiscard]
    public static unsafe WordNode Create(in ReadOnlySpan<char> span, WordType wordType, int wordIndex) =>
        Create(span.ConvertU16(), wordType, wordIndex);

    public static unsafe WordNode Create(in ReadOnlySpan<ushort> span, WordType wordType, int wordIndex)
    {
        Debug.Assert(!span.IsEmpty);
        Debug.Assert(wordType  != WordType.Unknown);
        Debug.Assert(wordIndex >= 0);

        WordNode node = new()
        {
            type      = wordType,
            length    = unchecked((ushort) span.Length),
            wordIndex = wordIndex
        };
        fixed (ushort* ptr = span) node.ptr = ptr;
        return node;
    }
}

/// <summary>
/// A node that allows for the optimal storage of either 1 or multiple nodes into a phrase.
/// </summary>
[BurstCompile, StructLayout(LayoutKind.Explicit, Size = 16)]
public struct PhraseNode : IDisposable
{
    // Single Case
    [FieldOffset(0)]  private WordNode wordNode;
    // Multiple Case
    [FieldOffset(0)]  private unsafe WordNode* wordNodes;

    [FieldOffset(8)]  private ushort   length;
    [FieldOffset(10)] private int      padding;
    [FieldOffset(14)] private byte     allocator;
    [FieldOffset(15)] private WordType phraseType;

    public unsafe readonly bool IsValid => IsSingle ? wordNode.IsValid : wordNodes != null;

    public readonly bool IsSingle       => wordNode.WordIndex >= 0;
    public readonly bool IsMulti        => padding == -1;
    public readonly WordType PhraseType => phraseType;

    public PhraseNode(in WordNode wordNode, WordType phraseType) : this()
    {
        Debug.Assert(wordNode.IsValid);
        this.wordNode   = wordNode;
        this.phraseType = phraseType;
    }

    public unsafe PhraseNode(in ReadOnlySpan<WordNode> nodes, WordType phraseType, Allocator allocator) : this()
    {
        Debug.Assert(nodes.Length > 0);
        for (int i = 0; i < nodes.Length; i++)
        {
            Debug.Assert(wordNodes[i].IsValid);
        }
        length  = (ushort) nodes.Length;
        padding = -1;

        this.allocator  = (byte) allocator;
        this.phraseType = phraseType;

        wordNodes = (WordNode*) UnsafeUtility.MallocTracked(nodes.Length * sizeof(WordNode), UnsafeUtility.AlignOf<WordNode>(), allocator, 0);
    }

    public unsafe readonly ReadOnlySpan<WordNode> GetWords()
    {
        Debug.Assert(IsValid);
        if (IsMulti)
        {
            return new ReadOnlySpan<WordNode>(wordNodes, length);
        }
        fixed (WordNode* nodePtr = &wordNode) // Single case
        {
            return new ReadOnlySpan<WordNode>(nodePtr, 1);
        }
    }

    public unsafe void Dispose()
    {
        if (padding == -1)
        {
            UnsafeUtility.FreeTracked(wordNodes, (Allocator) allocator);
            wordNodes = null;
            padding   = 0;
        }
    }
}