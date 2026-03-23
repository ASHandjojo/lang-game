using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;

[BurstCompile]
public struct SplitEntry
{
    // Using ushort instead of chars, as chars are not supported in Burst.
    private unsafe ushort* ptr;
    private int length;

    public unsafe readonly bool IsValid => ptr != null && length > 0;

    public static unsafe SplitEntry Create(in ReadOnlySpan<ushort> src)
    {
        //Debug.Assert(!src.IsEmpty);
        SplitEntry output = new()
        {
            length = src.Length
        };
        fixed (ushort* ptr = src) output.ptr = ptr;
        return output;
    }

    [BurstDiscard]
    public static unsafe SplitEntry Create(in ReadOnlySpan<char> src)
    {
        fixed (char* ptr = src) return Create(new ReadOnlySpan<ushort>(ptr, src.Length));
    }

    public static unsafe implicit operator ReadOnlySpan<ushort>(in SplitEntry input) =>
        new(input.ptr, input.length);

    [BurstDiscard]
    public static unsafe implicit operator ReadOnlySpan<char>(in SplitEntry input) =>
        new(input.ptr, input.length);
}

[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
public struct SplitIterator : IEnumerator<SplitEntry>
{
    private unsafe ushort* ptr;
    private ushort strLength;

    private ushort offset;
    private ushort iterOffset;

    private ushort splitChar;

    private unsafe readonly ReadOnlySpan<ushort> Span => new(ptr, strLength);

    public static unsafe SplitIterator Create(in ReadOnlySpan<ushort> span, ushort splitChar)
    {
        Debug.Assert(!span.IsEmpty);
        SplitIterator output = new()
        {
            strLength  = (ushort) span.Length,
            offset     = 0,
            iterOffset = 0,
            splitChar  = splitChar
        };

        fixed (ushort* ptr = span) output.ptr = ptr;
        return output;
    }

    [BurstDiscard]
    public static unsafe SplitIterator Create(in ReadOnlySpan<char> span, char splitChar) => Create(span.ConvertU16(), splitChar);

    public readonly SplitEntry Current
    {
        get
        {
            return SplitEntry.Create(Span[offset..(offset + iterOffset)]);
        }
    }

    [BurstDiscard]
    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (Hint.Unlikely(offset >= strLength - 1))
        {
            return false;
        }
        var subSpan = Span[(offset)..];
        for (ushort i = 0; i < subSpan.Length; i++)
        {
            if (subSpan[i] == splitChar)
            {
                if (i > 0)
                {
                    Debug.Log("Base case");
                    offset    += iterOffset;
                    iterOffset = i;
                    return true;
                }
                // NOTE: WATCH
                else // If zero, this means a region of contiguous splitting characters.
                {
                    i++;
                    for (; i < subSpan.Length; i++)
                    {
                        if (i != splitChar)
                        {
                            offset    += iterOffset;
                            iterOffset = i;
                            return MoveNext();
                        }
                    }
                }
            }
        }
        return false;
    }

    public void Reset()
    {
        offset = 0;
    }

    public readonly void Dispose() {}
}

public static class FastStringExtMethods
{
    public static unsafe ReadOnlySpan<ushort> ConvertU16(in this ReadOnlySpan<char> span)
    {
        fixed (char* ptr = span) return new ReadOnlySpan<ushort>(ptr, span.Length);
    }
    public static unsafe ReadOnlySpan<char> ConvertChar(in this ReadOnlySpan<ushort> span)
    {
        fixed (ushort* ptr = span) return new ReadOnlySpan<char>(ptr, span.Length);
    }

    [BurstCompile]
    public static int CharCount(in this ReadOnlySpan<ushort> span, ushort target)
    {
        int count = 0;
        for (int i = 0; i < span.Length; i++)
        {
            count += (span[i] == target).CastAsInt32();
        }
        return count;
    }

    public unsafe static int CharCount(in this ReadOnlySpan<char> span, char target) => span.ConvertU16().CharCount(target);

    [BurstCompile]
    public static int WordCount(in this ReadOnlySpan<ushort> span, ushort target)
    {
        if (Hint.Unlikely(span.IsEmpty))
        {
            return 0;
        }

        int count  = 1;
        int offset = 0, iterOffset = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == target)
            {
                if (i > 0)
                {
                    offset    += iterOffset;
                    iterOffset = i;
                    count++;
                }
                // NOTE: WATCH
                else // If zero, this means a region of contiguous splitting characters.
                {
                    i++;
                    for (; i < span.Length; i++)
                    {
                        if (i != target)
                        {
                            offset    += iterOffset;
                            iterOffset = i;
                        }
                    }
                    count++;
                }
            }
        }
        return count;
    }

    public static SplitIterator FastSplit(this string str, char target) => SplitIterator.Create(str, target);
}