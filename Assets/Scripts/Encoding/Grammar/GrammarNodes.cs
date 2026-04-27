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
    private WordType       type;

    private int wordIndex;

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