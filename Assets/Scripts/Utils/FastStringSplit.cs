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
    private ushort prevOffset;

    private ushort splitChar;

    private unsafe readonly ReadOnlySpan<ushort> Span => new(ptr, strLength);

    public static unsafe SplitIterator Create(in ReadOnlySpan<ushort> span, ushort splitChar)
    {
        Debug.Assert(!span.IsEmpty);
        SplitIterator output = new()
        {
            strLength  = (ushort) span.Length,
            offset     = 0,
            prevOffset = 0,
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
            return SplitEntry.Create(Span[prevOffset..offset]);
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
        int startIdx = offset == 0 ? 0 : offset + 1;
        var subSpan  = Span[startIdx..];

        int splitIndex = subSpan.IndexOf(splitChar);
        if (splitIndex == -1) // If cannot find, set iterator to last iteration.
        {
            prevOffset = (ushort) startIdx;
            offset     = (ushort) strLength;
            return true;
        }
        prevOffset = (ushort) startIdx;
        offset     = (ushort) (startIdx + splitIndex);
        return true;
    }

    public void Reset()
    {
        offset = 0;
    }

    public readonly void Dispose() {}
}

/// <summary>
/// Used for splitting but allows for multiple (up to 8) split options.
/// </summary>
[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 32)]
public struct MultiSplitIterator : IEnumerator<SplitEntry>
{
    private unsafe ushort* ptr;
    private ushort strLength;

    private ushort offset;
    private ushort prevOffset;

    private const int MaxSplitCount = 8;
    private unsafe fixed ushort SplitCharsRaw[MaxSplitCount];
    private ushort charCount;

    private unsafe readonly ReadOnlySpan<ushort> Span => new(ptr, strLength);
    private unsafe readonly Span<ushort> SplitCharsMut
    {
        get { fixed (ushort* charPtr = SplitCharsRaw) return new Span<ushort>(charPtr, strLength); }
    }
    private unsafe readonly ReadOnlySpan<ushort> SplitChars => SplitCharsMut;

    public static unsafe MultiSplitIterator Create(in ReadOnlySpan<ushort> span, in ReadOnlySpan<ushort> splitChars)
    {
        Debug.Assert(!span.IsEmpty);
        Debug.Assert(!splitChars.IsEmpty && splitChars.Length >= MaxSplitCount);
        MultiSplitIterator output = new()
        {
            strLength  = (ushort) span.Length,
            offset     = 0,
            prevOffset = 0,
            charCount  = (ushort) splitChars.Length
        };

        fixed (ushort* ptr = span) output.ptr = ptr;

        var splitCharSpan = output.SplitCharsMut;
        splitChars.CopyTo(splitCharSpan);
        return output;
    }

    [BurstDiscard]
    public static unsafe MultiSplitIterator Create(in ReadOnlySpan<char> span, in ReadOnlySpan<char> splitChars) =>
        Create(span.ConvertU16(), splitChars.ConvertU16());

    public readonly SplitEntry Current
    {
        get
        {
            return SplitEntry.Create(Span[prevOffset..offset]);
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
        int startIdx = offset == 0 ? 0 : offset + 1;
        var subSpan  = Span[startIdx..];

        int splitIndex = int.MaxValue;
        foreach (ushort splitter in SplitCharsMut)
        {
            int currentSplitIdx = subSpan.IndexOf(splitter);
            if (currentSplitIdx != -1)
            {
                splitIndex = math.min(splitIndex, currentSplitIdx);
            }
        }
        if (splitIndex == int.MaxValue) // If cannot find, set iterator to last iteration.
        {
            prevOffset = (ushort) startIdx;
            offset     = (ushort) strLength;
            return true;
        }
        prevOffset = (ushort) startIdx;
        offset     = (ushort) (startIdx + splitIndex);
        return true;
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

    // Single Iterator Word Count

    [BurstCompile]
    public static int WordCount(in this ReadOnlySpan<ushort> str, ushort target)
    {
        if (Hint.Unlikely(str.IsEmpty))
        {
            return 0;
        }
        SplitIterator iter = SplitIterator.Create(str, target);
        int count = 0;
        while (iter.MoveNext())
        {
            ReadOnlySpan<ushort> span = iter.Current;
            count += (span.Length > 0 && span[0] != target).CastAsInt32();
        }
        return count;
    }

    public static SplitIterator FastSplit(this string str, char target) => SplitIterator.Create(str, target);

    // Multi Iterator Word Count

    [BurstCompile]
    public static int WordCount(in this ReadOnlySpan<ushort> str, in ReadOnlySpan<ushort> splitChars)
    {
        if (Hint.Unlikely(str.IsEmpty))
        {
            return 0;
        }
        MultiSplitIterator iter = MultiSplitIterator.Create(str, splitChars);
        int count = 0;
        while (iter.MoveNext())
        {
            ReadOnlySpan<ushort> span = iter.Current;
            if (span.Length > 0)
            {
                bool isNotSplit = true;
                for (int i = 0; i < splitChars.Length && isNotSplit; i++)
                {
                    isNotSplit = span[0] != splitChars[i];
                }
                count += isNotSplit.CastAsInt32();
            }
        }
        return count;
    }
}