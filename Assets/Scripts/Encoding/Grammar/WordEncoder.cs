using System;
using System.Runtime.InteropServices;

using Unity.Burst;
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

[BurstCompile]
internal struct StringPool : IDisposable
{
    private NativeArray<ushort> pool;
    // Exclusive Prefix Sum (for Lengths)
    private NativeArray<int> lengthOffsets;
    // For Prefix Lookup (Exclusive Prefix Sum per length, stored contiguously)
    private NativeArray<int> prefixOffsets;
    // Gives the proper offsets for each prefix
    private NativeArray<int> prefixLocations;
    // Gives the minimum shift values for lookup
    private NativeArray<ushort> minPrefixChars;

    public readonly bool IsValid => pool.IsCreated && lengthOffsets.IsCreated;

    private unsafe readonly char* PoolPtr      => (char*) GetUnsafeBufferPointerWithoutChecks(pool);
    private readonly Span<ushort> PoolMut      => pool.AsSpan();
    private readonly ReadOnlySpan<ushort> Pool => pool.AsReadOnlySpan();

    /// <summary>
    /// Get readonly substring.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public unsafe readonly ReadOnlySpan<ushort> this[int index]
    {
        get => Pool[lengthOffsets[index]..lengthOffsets[index + 1]];
    }

    /// <summary>
    /// Takes a sorted list of 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="strs"></param>
    /// <param name="allocator"></param>
    /// <returns></returns>
    public static StringPool Create<T>(in ReadOnlySpan<DictEntryUnmanaged> entries, in ReadOnlySpan<int> lengthOffsets, Allocator allocator) where T : unmanaged, ISelector<DictEntryUnmanaged>
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
            NativeSlice<int> prefixHist = prefixOffsets.Slice();
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
            prefixHist[^1] = lengthOffsets[x + 1] - lengthOffsets[x]; // Assign length last
        }
        // Calculating total memory usage for string pool
        int totalCharLength = 0;
        for (int i = 0; i < lengthOffsets.Length - 1; i++)
        {
            totalCharLength += (lengthOffsets[i + 1] - lengthOffsets[i]) * (i + 1);
        }
        StringPool result = new()
        {
            lengthOffsets = new NativeArray<int>(lengthOffsets.Length, allocator, NativeArrayOptions.UninitializedMemory),
            pool          = new NativeArray<ushort>(totalCharLength,   allocator, NativeArrayOptions.UninitializedMemory),

            prefixOffsets   = prefixOffsets,
            prefixLocations = prefixLocations,
            minPrefixChars  = minPrefixChars
        };
        lengthOffsets.CopyTo(result.lengthOffsets);
        // Copies selected string to contiguous pool
        var poolSpan = result.pool.AsSpan();
        for (int x = 0, offset = 0; x < lengthOffsets.Length - 1; x++)
        {
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                var dst = poolSpan[offset..(offset + x + 1)];
                var src = selectFunc.Select(entries[i]);
                src.CopyTo(dst);
                offset += x + 1;
            }
        }

        return result;
    }

    public void Dispose()
    {
        pool.Dispose();
        pool = default;

        lengthOffsets.Dispose();
        lengthOffsets = default;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct WordNode
{
    [NativeDisableUnsafePtrRestriction]
    private unsafe char* ptr;
    private ushort       length;
    private WordType     type;

    public unsafe readonly bool IsValid => ptr != null && length > 0;

    public unsafe WordNode(in ReadOnlySpan<char> span, WordType wordType)
    {
        Debug.Assert(!span.IsEmpty);
        Debug.Assert(wordType != WordType.Unknown);

        fixed (char* ptr = span) this.ptr = ptr;
        type   = wordType;
        length = unchecked((ushort) span.Length);
    }

    public static WordNode Unknown => new()
    {
        type = WordType.Unknown
    };
}

[BurstCompile]
public struct WordEncoder : IDisposable
{
    // Heap
    private StringPool rawPool;
    private StringPool englishPool;

    private StringPool unicodePool;

    private NativeArray<WordType> wordTypes;

    private static NativeArray<int> SortEntries(in NativeArray<DictEntryUnmanaged> entries, int maxLength)
    {
        // First, sort by length with a counting sort :)
        NativeArray<DictEntryUnmanaged> temp = new(entries.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        Span<int> lengthHist = stackalloc int[maxLength + 1];
        lengthHist.Fill(0);
        for (int i = 0; i < entries.Length; i++)
        {
            int key = entries[i].UnicodeString.Length - 1;
            lengthHist[key]++;
        }
        // To exclusive prefix sum
        for (int i = 0, value = 0; i < maxLength; i++)
        {
            int countAtIdx = lengthHist[i];
            lengthHist[i]  = value;
            value         += countAtIdx; 
        }
        NativeArray<int> sumCopy = new(maxLength, Allocator.Temp);
        lengthHist[..^1].CopyTo(sumCopy.AsSpan());
        // Scatter back
        for (int i = 0; i < entries.Length; i++)
        {
            int key = entries[i].UnicodeString.Length - 1;
            temp[lengthHist[key]++] = entries[i];
        }
        // Then, sort by lexicographical
        for (int i = 0; i < maxLength; i++)
        {
            int count = lengthHist[i + 1] - lengthHist[i];
            if (count == 0)
            {
                continue;
            }
            entries.GetSubArray(lengthHist[i], lengthHist[i + 1]).Sort();
        }
        return sumCopy;
    }

    public static WordEncoder Create(in NativeArray<DictEntryUnmanaged> entries, Allocator allocator)
    {
        WordEncoder encoder = new()
        {
            wordTypes = new NativeArray<WordType>(entries.Length, allocator)
        };

        int maxLength = 0, minLength = int.MaxValue;
        for (int i = 0; i < entries.Length; i++)
        {
            int strLen = entries[i].UnicodeString.Length;
            minLength  = math.min(minLength, strLen);
            maxLength  = math.max(maxLength, strLen);

            encoder.wordTypes[i] = entries[i].WordType;
        }
        var prefixSum = SortEntries(entries, maxLength);

        encoder.rawPool     = StringPool.Create<RawPhoneticsSelector>(entries, prefixSum, allocator);
        encoder.unicodePool = StringPool.Create<UnicodeStrSelector>(entries,   prefixSum, allocator);
        encoder.englishPool = StringPool.Create<EnglishTransSelector>(entries, prefixSum, allocator);
        return encoder;
    }

    private readonly bool TryFind(in ReadOnlySpan<ushort> str, out WordNode node)
    {
        node = default;
        return true;
    }

    public readonly NativeArray<WordNode> Parse(in ReadOnlySpan<ushort> str, Allocator allocator)
    {
        if (str.IsEmpty) // Short-circuit if empty
        {
            return default;
        }
        int wordCount = str.WordCount(' ');
        if (wordCount == 0) // Short-circuit if all whitespace
        {
            return default;
        }
        NativeArray<WordNode> nodes = new(wordCount, allocator);
        using SplitIterator iter    = SplitIterator.Create(str, ' ');
        int index = 0;
        while (iter.MoveNext())
        {
            ReadOnlySpan<char> span = iter.Current;
            nodes[index++] = new WordNode();
        }
        return nodes;
    }

    public void Dispose()
    {
        rawPool.Dispose();
        rawPool = default;

        englishPool.Dispose();
        englishPool = default;

        unicodePool.Dispose();
        unicodePool = default;

        wordTypes.Dispose();
        wordTypes = default;
    }
}