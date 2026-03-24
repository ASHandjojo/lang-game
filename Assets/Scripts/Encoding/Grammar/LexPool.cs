using System;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;

using static Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;

using Impl;

namespace Impl
{
    internal interface ISelector<T> where T : unmanaged
    {
        public ReadOnlySpan<ushort> Select(in T entry);
    }

    [BurstCompile]
    internal readonly struct RawPhoneticsSelector : ISelector<DictEntryUnmanaged>
    {
        public readonly ReadOnlySpan<ushort> Select(in DictEntryUnmanaged entry) => entry.RawPhonetics;
    }

    [BurstCompile]
    internal readonly struct UnicodeStrSelector : ISelector<DictEntryUnmanaged>
    {
        public readonly ReadOnlySpan<ushort> Select(in DictEntryUnmanaged entry) => entry.UnicodeString;
    }

    [BurstCompile]
    internal readonly struct EnglishTransSelector : ISelector<DictEntryUnmanaged>
    {
        public readonly ReadOnlySpan<ushort> Select(in DictEntryUnmanaged entry) => entry.EnglishTrans;
    }
}

[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct LexOffsets : IDisposable
{
    [NativeDisableParallelForRestriction]
    private unsafe int* prefixOffsets;
    private ushort length;

    private ushort minPrefixChar, maxPrefixChar;

    private byte strLength;
    private byte allocator;

    public readonly int Length           => length;
    public readonly ushort MinPrefixChar => minPrefixChar;
    public readonly ushort MaxPrefixChar => maxPrefixChar;

    public readonly Allocator Allocator => (Allocator) allocator;
    public readonly int StrLength       => strLength;

    public unsafe readonly bool IsValid => prefixOffsets != null && length > 0 && strLength > 0;

    public static unsafe LexOffsets Create(in ReadOnlySpan<ushort> prefixes, int strLength, Allocator allocator)
    {
        Debug.Assert(strLength > 0);
        if (Hint.Unlikely(prefixes.IsEmpty))
        {
            return default;
        } 

        int minPrefixChar = int.MaxValue, maxPrefixChar = 0;
        for (int i = 0; i < prefixes.Length; i++)
        {
            minPrefixChar = math.min(minPrefixChar, prefixes[i]);
            maxPrefixChar = math.max(maxPrefixChar, prefixes[i]);
        }
        int offsetLength = maxPrefixChar - minPrefixChar;
        if (Hint.Unlikely(offsetLength == 0))
        {
            return default;
        }
        int  prefixLength  = (offsetLength + 1) * sizeof(int);
        int* prefixOffsets = (int*) UnsafeUtility.MallocTracked(prefixLength, UnsafeUtility.AlignOf<int>(), allocator, 0);
        UnsafeUtility.MemClear(prefixOffsets, prefixLength);
        // Histogram Calculation
        for (int i = 0; i < prefixes.Length; i++)
        {
            prefixOffsets[prefixes[i] - minPrefixChar]++;
        }
        // Exclusive Prefix Sum
        for (int i = 0, value = 0; i < offsetLength; i++)
        {
            int temp         = prefixOffsets[i];
            prefixOffsets[i] = value;
            value           += temp;
        }
        prefixOffsets[offsetLength] = prefixes.Length;

        LexOffsets output = new()
        {
            prefixOffsets = prefixOffsets,
            allocator     = (byte) allocator,

            length    = (ushort) offsetLength,
            strLength = (byte)   strLength,

            minPrefixChar = (ushort) minPrefixChar,
            maxPrefixChar = (ushort) maxPrefixChar
        };

        return output;
    }

    /// <summary>
    /// Checks whether the prefix character could be in this structure and has entries.
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public unsafe readonly bool IsInRange(ushort prefix)
    {
        ushort shiftedChar = (ushort) (prefix - minPrefixChar);
        if (shiftedChar < length)
        {
            int prefixLen = prefixOffsets[shiftedChar + 1] - prefixOffsets[shiftedChar];
            return prefixLen > 0;
        }
        return false;
    }

    /// <summary>
    /// Gets the range of strings that a prefix has in memory. Does not incorporate bounds checking, so only use when you know a character is included.
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public unsafe readonly Range this[ushort prefix]
    {
        get
        {
            ushort shiftedChar = (ushort) (prefix - minPrefixChar);
            return new Range(prefixOffsets[shiftedChar], prefixOffsets[shiftedChar + 1]);
        }
    }

    public unsafe void Dispose()
    {
        if (Hint.Likely(IsValid))
        {
            UnsafeUtility.FreeTracked(prefixOffsets, Allocator);
            prefixOffsets = null;
        }
    }
}

[BurstCompile]
internal struct LexPool : IDisposable
{
    private NativeArray<ushort> pool;
    // Exclusive Prefix Sum (for Lengths)
    private NativeArray<int> lengthOffsets;
    // Memory Offsets for Length Chunks (Exclusive Prefix Sum)
    private NativeArray<int> charOffsets;

    private NativeArray<LexOffsets> lexOffsets;

    public readonly bool IsValid => pool.IsCreated && lengthOffsets.IsCreated;

    private unsafe readonly char* PoolPtr      => (char*) GetUnsafeBufferPointerWithoutChecks(pool);
    private readonly Span<ushort> PoolMut      => pool.AsSpan();
    private readonly ReadOnlySpan<ushort> Pool => pool.AsReadOnlySpan();

    public readonly int MaxStrLength => lengthOffsets.Length - 1;

    /// <summary>
    /// Takes a sorted list of 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="strs"></param>
    /// <param name="allocator"></param>
    /// <returns></returns>
    public static LexPool Create<T>(in ReadOnlySpan<DictEntryUnmanaged> entries, in ReadOnlySpan<int> lengthOffsets, Allocator allocator) where T : unmanaged, ISelector<DictEntryUnmanaged>
    {
        Debug.Assert(entries.Length       > 0);
        Debug.Assert(lengthOffsets.Length > 0);
        T selectFunc = new();

        // Used to cache prefix accesses (this is otherwise cache incineration lol)
        NativeArray<ushort> prefixChars    = new(entries.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        NativeArray<LexOffsets> lexOffsets = new(lengthOffsets.Length - 1, allocator);
        for (int x = 0; x < lengthOffsets.Length - 1; x++)
        {
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                var selectStr  = selectFunc.Select(entries[i]);
                prefixChars[i] = selectStr[0];
            }
            var localPrefixes = prefixChars.AsReadOnlySpan()[lengthOffsets[x]..lengthOffsets[x + 1]];
            lexOffsets[x]     = LexOffsets.Create(localPrefixes, x + 1, allocator);
        }

        // Calculating total memory usage for string pool
        NativeArray<int> charOffsets = new(lengthOffsets.Length, allocator);
        int totalCharLength = 0;
        for (int i = 0, value = 0; i < lengthOffsets.Length - 1; i++)
        {
            int currCharLen  = (lengthOffsets[i + 1] - lengthOffsets[i]) * (i + 1);
            totalCharLength += currCharLen;

            charOffsets[i] = value;
            value         += currCharLen;
        }
        charOffsets[^1] = totalCharLength * sizeof(ushort);

        LexPool result = new()
        {
            pool          = new NativeArray<ushort>(totalCharLength,   allocator, NativeArrayOptions.UninitializedMemory),
            lengthOffsets = new NativeArray<int>(lengthOffsets.Length, allocator, NativeArrayOptions.UninitializedMemory),
            charOffsets   = charOffsets,

            lexOffsets = lexOffsets
        };
        lengthOffsets.CopyTo(result.lengthOffsets);
        // Copies selected string to contiguous pool
        var poolSpan = result.pool.AsSpan();
        for (int x = 0, offset = 0; x < lengthOffsets.Length - 1; x++)
        {
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                Span<ushort> dst         = poolSpan[offset..];
                ReadOnlySpan<ushort> src = selectFunc.Select(entries[i]);

                src.CopyTo(dst);
                offset += x + 1;
            }
        }

        return result;
    }

    public readonly bool IsPresent(in ReadOnlySpan<ushort> str, out int strIndex)
    {
        if (Hint.Unlikely(str.IsEmpty)) // Short-circuit when length of zero.
        {
            strIndex = -1;
            return false;
        }
        // Length check
        if (Hint.Likely(MaxStrLength >= str.Length)) // Max length bounds check (the min is 1 implicitly for LexPool)
        {
            int lengthIndex       = str.Length - 1;
            LexOffsets lexOffsets = this.lexOffsets[lengthIndex];

            ushort prefix = str[0];
            if (Hint.Likely(lexOffsets.IsInRange(prefix))) // If the bucket is not empty
            {
                var poolSpan = pool.AsReadOnlySpan();

                int charLenOffset = charOffsets[lengthIndex];
                Range lexRange    = lexOffsets[prefix];

                strIndex  = lengthOffsets[lengthIndex] + lexRange.Start.Value;
                int start = charLenOffset + (lexRange.Start.Value * str.Length);
                for (int i = start; strIndex < lengthOffsets[lengthIndex] + lexRange.End.Value; i += str.Length, strIndex++)
                {
                    ReadOnlySpan<ushort> rhs = poolSpan[i..(i + str.Length)];
                    bool isEqual = true;
                    for (int j = 0; j < rhs.Length && isEqual; j++)
                    {
                        isEqual = str[j] == rhs[j];
                    }
                    if (isEqual)
                    {
                        return true;
                    }
                }
            }
        }

        strIndex = -1;
        return false;
    }

    public void Dispose()
    {
        pool.Dispose();
        pool = default;

        lengthOffsets.Dispose();
        lengthOffsets = default;

        charOffsets.Dispose();
        charOffsets = default;

        foreach (var lexOffset in lexOffsets)
        {
            lexOffset.Dispose();
        }
        lexOffsets.Dispose();
        lexOffsets = default;
    }
}