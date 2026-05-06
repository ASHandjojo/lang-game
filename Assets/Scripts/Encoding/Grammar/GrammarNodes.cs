using System;
using System.Linq;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

public enum NodeType : byte
{
    Single = 1,
    Multi  = 2,

    Word   = 4 | Single,
    Phrase = 8,

    SinglePhrase = Phrase | Single,
    MultiPhrase  = Phrase | Multi,

    Combined = 16
}

/// <summary>
/// Gives part of speech to a given Unicode representation of a word (terminal symbol).
/// </summary>
[BurstCompile, StructLayout(LayoutKind.Explicit, Size = 16)]
public struct WordNode
{
    [NativeDisableUnsafePtrRestriction]
    [FieldOffset(0)] private unsafe ushort* ptr;
    [FieldOffset(8)] private ushort         length;

    [FieldOffset(10)] private int wordIndex;

    [FieldOffset(14)] private WordType type;
    [FieldOffset(15)] private NodeType nodeType;

    public unsafe readonly bool IsValid => ptr != null && length > 0 && wordIndex >= 0;
    public readonly WordType WordType   => type;
    public readonly int WordIndex       => wordIndex;

    public static WordNode Unknown => new()
    {
        type      = WordType.Unknown,
        wordIndex = -1,
        nodeType  = NodeType.Word
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
            wordIndex = wordIndex,
            nodeType  = NodeType.Word
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
    [FieldOffset(9)]  private byte     allocator;

    [FieldOffset(10)] private int headIndex;
    [FieldOffset(14)] private WordType phraseType;
    [FieldOffset(15)] private NodeType type;

    public unsafe readonly bool IsValid => IsSingle ? wordNode.IsValid : wordNodes != null;

    public readonly bool IsSingle       => type == NodeType.SinglePhrase;
    public readonly bool IsMulti        => type == NodeType.MultiPhrase;
    public readonly WordType PhraseType => phraseType;

    public PhraseNode(in WordNode wordNode, WordType phraseType) : this()
    {
        Debug.Assert(wordNode.IsValid);
        this.wordNode   = wordNode;
        this.phraseType = phraseType;

        type = NodeType.SinglePhrase;
    }

    public unsafe PhraseNode(in ReadOnlySpan<WordNode> nodes, int headIndex, WordType phraseType, Allocator allocator) : this()
    {
        Debug.Assert(nodes.Length > 0);
        for (int i = 0; i < nodes.Length; i++)
        {
            Debug.Assert(wordNodes[i].IsValid);
        }
        length = (ushort) nodes.Length;
        type   = NodeType.MultiPhrase;

        this.allocator  = (byte) allocator;
        this.phraseType = phraseType;
        this.headIndex  = headIndex;

        wordNodes    = (WordNode*) UnsafeUtility.MallocTracked(nodes.Length * sizeof(WordNode), UnsafeUtility.AlignOf<WordNode>(), allocator, 0);
        var wordSpan = GetWordsMut();
        nodes.CopyTo(wordSpan);
    }

    private unsafe readonly Span<WordNode> GetWordsMut()
    {
        Debug.Assert(IsValid);
        if (IsMulti)
        {
            return new Span<WordNode>(wordNodes, length);
        }
        fixed (WordNode* nodePtr = &wordNode) // Single case
        {
            return new Span<WordNode>(nodePtr, 1);
        }
    }
    public readonly ReadOnlySpan<WordNode> GetWords() => GetWordsMut();

    public unsafe void Dispose()
    {
        if (IsMulti)
        {
            UnsafeUtility.FreeTracked(wordNodes, (Allocator) allocator);
            wordNodes = null;
        }
    }
}

[BurstCompile, StructLayout(LayoutKind.Explicit, Size = 16)]
public struct CombinedNode
{
    [FieldOffset(0)]  public WordNode   wordNode;
    [FieldOffset(0)]  public PhraseNode phraseNode;
    [FieldOffset(15)] public NodeType   nodeType;

    public readonly bool IsValid => (nodeType & NodeType.Word) != 0 ? wordNode.IsValid : phraseNode.IsValid;

    public CombinedNode(in WordNode wordNode) : this()
    {
        Debug.Assert(wordNode.IsValid);
        this.wordNode = wordNode;
    }

    public CombinedNode(in PhraseNode phraseNode) : this()
    {
        Debug.Assert(phraseNode.IsValid);
        this.phraseNode = phraseNode;
    }
}

[BurstCompile, StructLayout(LayoutKind.Explicit, Size = 16)]
public struct UnionPhraseNode : IDisposable
{
    // Single
    [FieldOffset(0)] private CombinedNode wordNode;
    // Multi
    [FieldOffset(0)] private unsafe CombinedNode* nodesRaw;
    // Constituent type only matters for single, as multi can ONLY be a phrase.

    [FieldOffset(8)] private byte   allocator;
    [FieldOffset(9)] private ushort length;

    [FieldOffset(14)] private WordType phraseType; // For multiple
    [FieldOffset(15)] private NodeType nodeType;

    private unsafe readonly Span<CombinedNode> NodesMut
    {
        get
        {
            if (IsMulti)
            {
                return new Span<CombinedNode>(nodesRaw, length);
            }
            fixed (CombinedNode* ptr = NodesMut)
            {
                return new Span<CombinedNode>(ptr, length);
            }
        }
    }

    public unsafe readonly bool IsValid => IsSingle ? wordNode.IsValid : nodesRaw != null && length > 0;

    public readonly bool IsSingle => length == 1;
    // Multi means multiple nodes; whether phrase or single
    public readonly bool IsMulti  => length > 1;

    public UnionPhraseNode(in WordNode node) : this()
    {
        Debug.Assert(node.IsValid);
        wordNode = new(node);
        length   = 1;
    }

    public UnionPhraseNode(in PhraseNode node) : this()
    {
        Debug.Assert(node.IsValid);
        wordNode = new(node);
        length   = 1;
    }

    public unsafe UnionPhraseNode(in ReadOnlySpan<CombinedNode> nodes, WordType phraseType, Allocator allocator) : this()
    {
        Debug.Assert(nodes.Length >= 1);
        Debug.Assert(phraseType   != WordType.Unknown);

        this.allocator  = (byte) allocator;
        this.phraseType = phraseType;

        length   = (ushort) nodes.Length;
        nodeType = NodeType.Combined;
        nodesRaw = (CombinedNode*) UnsafeUtility.MallocTracked(nodes.Length * sizeof(CombinedNode), UnsafeUtility.AlignOf<CombinedNode>(), allocator, 0);

        var nodeSpan = NodesMut;
        nodes.CopyTo(nodeSpan);
    }

    private unsafe readonly Span<CombinedNode> GetWordsMut()
    {
        Debug.Assert(IsValid);
        if (IsMulti)
        {
            return new Span<CombinedNode>(nodesRaw, length);
        }
        fixed (CombinedNode* nodePtr = &wordNode) // Single case
        {
            return new Span<CombinedNode>(nodePtr, 1);
        }
    }

    public readonly ReadOnlySpan<CombinedNode> GetWords() => GetWordsMut();

    public unsafe void Dispose()
    {
        if (nodeType == NodeType.Combined)
        {
            for (int i = 0; i < length; i++)
            {
                if ((nodesRaw[i].nodeType & NodeType.Multi) != 0)
                {
                    nodesRaw[i].phraseNode.Dispose();
                }
            }
            UnsafeUtility.FreeTracked(nodesRaw, (Allocator) allocator);
            nodesRaw = null;
            length   = 0;
        }
        else if (nodeType == NodeType.MultiPhrase)
        {
            wordNode.phraseNode.Dispose();
            wordNode = default;
        }
    }
}