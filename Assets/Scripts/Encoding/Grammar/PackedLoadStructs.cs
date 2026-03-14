using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;

/// <summary>
/// Is a burst compilable version of DictEntry. Uses contiguous packing for heap :)
/// </summary>
[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
public struct DictEntryUnmanaged : IDisposable, IComparable<DictEntryUnmanaged>
{
    private unsafe ushort* ptr;
    private byte allocator; // Packed down to a byte ;)
    private unsafe fixed byte StrOffsetsRaw[3];

    private ushort   length;
    private WordType wordType;

    private unsafe readonly Span<ushort> FullSpan => new(ptr, length);

    private unsafe readonly Span<byte> StrOffsets
    {
        get
        {
            fixed (byte* strOffsets = StrOffsetsRaw) return new Span<byte>(strOffsets, length);
        }
    }

    private readonly int Length          => length;
    public readonly WordType WordType   => wordType;
    public readonly Allocator Allocator => (Allocator) allocator;

    public unsafe readonly bool IsValid => ptr != null && length > 0 && Allocator != Allocator.Invalid;

    [BurstDiscard]
    public static unsafe DictEntryUnmanaged Create(in DictEntry dictEntry, Allocator allocator)
    {
        int totalLength = dictEntry.rawString.Length + dictEntry.unicodeString.Length + dictEntry.englishTranslation.Length;
        DictEntryUnmanaged output = new()
        {
            ptr       = (ushort*) UnsafeUtility.MallocTracked(totalLength * sizeof(ushort), UnsafeUtility.AlignOf<ushort>(), allocator, 0),
            wordType  = dictEntry.wordType,
            allocator = (byte)   allocator,
            length    = (ushort) totalLength
        };
        // Via exclusive prefix sum
        var offsetSpan = output.StrOffsets;
        offsetSpan[0]  = 0;
        offsetSpan[1]  = (byte) dictEntry.rawString.Length;
        offsetSpan[2]  = (byte) (dictEntry.rawString.Length + dictEntry.unicodeString.Length);

        return output;
    }

    private unsafe readonly Span<ushort> RawPhoneticsMut  => FullSpan[StrOffsetsRaw[0]..StrOffsetsRaw[1]];
    private unsafe readonly Span<ushort> UnicodeStringMut => FullSpan[StrOffsetsRaw[1]..StrOffsetsRaw[2]];
    private unsafe readonly Span<ushort> EnglishTransMut  => FullSpan[StrOffsetsRaw[2]..];

    public readonly ReadOnlySpan<ushort> RawPhonetics  => RawPhoneticsMut;
    public readonly ReadOnlySpan<ushort> UnicodeString => UnicodeStringMut;
    public readonly ReadOnlySpan<ushort> EnglishTrans  => EnglishTransMut;

    public unsafe void Dispose()
    {
        if (Hint.Likely(IsValid))
        {
            UnsafeUtility.FreeTracked(ptr, Allocator);
            ptr = null;
        }
        allocator = 0;
        length    = 0;
    }

    public readonly int CompareTo(DictEntryUnmanaged rhs)
    {
        var lhsUnicode = UnicodeString;
        var rhsUnicode = rhs.UnicodeString;
        for (int i = 0; i < math.min(lhsUnicode.Length, rhsUnicode.Length); i++)
        {
            int compare = lhsUnicode[i].CompareTo(rhsUnicode[i]);
            if (compare != 0)
            {
                return compare;
            }
        }
        return 0;
    }
}

public static class PackedLoadStructExts
{
    public static NativeArray<DictEntryUnmanaged> Convert(in this ReadOnlySpan<DictEntry> entries, Allocator allocator)
    {
        NativeArray<DictEntryUnmanaged> output = new(entries.Length, allocator, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < entries.Length; i++)
        {
            output[i] = DictEntryUnmanaged.Create(entries[i], allocator);
        }
        return output;
    }

    public static NativeArray<DictEntryUnmanaged> Convert<T>(this T entries, Allocator allocator) where T : IList<DictEntry>
    {
        NativeArray<DictEntryUnmanaged> output = new(entries.Count, allocator, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < entries.Count; i++)
        {
            output[i] = DictEntryUnmanaged.Create(entries[i], allocator);
        }
        return output;
    }
}