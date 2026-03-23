using System;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
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

[BurstCompile]
internal struct LexPool : IDisposable
{
    private NativeArray<ushort> pool;
    // Exclusive Prefix Sum (for Lengths)
    private NativeArray<int> lengthOffsets;
    // Memory Offsets for Length Chunks (Exclusive Prefix Sum)
    private NativeArray<int> charOffsets;

    // For Prefix Lookup (Exclusive prefix sum per length, stored contiguously, offsets are str length agnostic)
    private NativeArray<int> prefixOffsets;
    // Gives the proper offsets for each lexicographical prefix sum stored in prefixOffsets
    private NativeArray<int> prefixLocations;
    // Gives the minimum shift values for lexicographical lookup
    private NativeArray<ushort> minPrefixChars;

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

        int prefixOffsetLength = 0;
        NativeArray<int> prefixLocations   = new(lengthOffsets.Length,     allocator);
        NativeArray<ushort> minPrefixChars = new(lengthOffsets.Length - 1, allocator);
        // Used to cache prefix accesses (this is otherwise cache incineration lol)
        NativeArray<ushort> prefixChars = new(entries.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        // First, find minimums (for shifting) and the sizes of each 
        for (int x = 0; x < lengthOffsets.Length - 1; x++)
        {
            int minPrefixChar = int.MaxValue, maxPrefixChar = 0;
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                var selectStr  = selectFunc.Select(entries[i]);
                ushort current = selectStr[0];

                minPrefixChar  = math.min(minPrefixChar, current);
                maxPrefixChar  = math.max(maxPrefixChar, current);

                prefixChars[i] = current;
            }
            if (minPrefixChar != int.MaxValue)
            {
                minPrefixChars[x] = (ushort) minPrefixChar;
                // Added 1 to get last offset :)
                int prefixExtents   = (maxPrefixChar - minPrefixChar) + 1;
                prefixOffsetLength += prefixExtents;
                prefixLocations[x]  = prefixExtents;
            }
        }
        // Compute prefix sum over prefix locations
        for (int i = 0, value = 0; i < prefixLocations.Length - 1; i++)
        {
            int count          = prefixLocations[i];
            prefixLocations[i] = value;
            value             += count;
        }
        prefixLocations[^1] = prefixOffsetLength;

        NativeArray<int> prefixOffsets = new(prefixOffsetLength, allocator);
        for (int x = 0; x < lengthOffsets.Length - 1; x++)
        {
            int minPrefixChar = minPrefixChars[x];
            NativeSlice<int> prefixHist = prefixOffsets.Slice(prefixLocations[x], prefixLocations[x + 1] - prefixLocations[x]);
            // Compute histogram
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                int key = prefixChars[i] - minPrefixChar; // Shifts by min so min equals 0 on index :)
                prefixHist[key]++;
            }
            // Compute prefix sums locally
            for (int i = 0, value = 0; i < prefixHist.Length - 1; i++)
            {
                int count     = prefixHist[i];
                prefixHist[i] = value;
                value        += count;
            }
            if (Hint.Likely(lengthOffsets[x + 1] - lengthOffsets[x] > 0))
            {
                prefixHist[^1] = lengthOffsets[x + 1] - lengthOffsets[x]; // Assign length last
            }
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
        charOffsets[^1]   = totalCharLength * sizeof(ushort);

        LexPool result = new()
        {
            pool          = new NativeArray<ushort>(totalCharLength,   allocator, NativeArrayOptions.UninitializedMemory),
            lengthOffsets = new NativeArray<int>(lengthOffsets.Length, allocator, NativeArrayOptions.UninitializedMemory),
            charOffsets   = charOffsets,

            prefixOffsets   = prefixOffsets,
            prefixLocations = prefixLocations,
            minPrefixChars  = minPrefixChars,
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
            int lengthIndex   = str.Length - 1;
            int lenBucketSize = lengthOffsets[lengthIndex + 1] - lengthOffsets[lengthIndex];
            if (Hint.Likely(lenBucketSize > 0)) // If the bucket is not empty
            {
                int prefixIndex = str[0] - minPrefixChars[lengthIndex];
                // Lexicographical bounds check
                if (Hint.Likely(prefixIndex >= 0 && prefixIndex >= prefixLocations[lengthIndex + 1] - prefixLocations[lengthIndex]))
                {
                    var poolSpan = pool.AsReadOnlySpan();

                    int charLenOffset = charOffsets[lengthIndex];
                    int lexLenOffset  = prefixLocations[lengthIndex] + prefixIndex;
                    Debug.Log($"Char Len Offset: {charLenOffset}");

                    strIndex = charLenOffset + prefixOffsets[lexLenOffset];
                    for (int i = charLenOffset + prefixOffsets[lexLenOffset]; i < charLenOffset + prefixOffsets[lexLenOffset + 1]; i += str.Length, strIndex++)
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

        prefixOffsets.Dispose();
        prefixOffsets = default;

        prefixLocations.Dispose();
        prefixLocations = default;

        minPrefixChars.Dispose();
        minPrefixChars = default;
    }
}