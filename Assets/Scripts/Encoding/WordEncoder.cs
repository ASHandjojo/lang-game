using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;

using UnityEngine;

public struct WordEncoder : IDisposable
{
    internal struct StringPool : IDisposable
    {
        private NativeArray<char> pool;
        private NativeArray<int>  offsets;

        public static StringPool Create<T>(in T strs, Allocator allocator) where T : IEnumerable<string>
        {
            int strCount = strs.Count();
            Debug.Assert(strCount > 0);

            StringPool result = new()
            {
                offsets = new NativeArray<int>(strCount + 1, allocator)
            };
            int length = 0; // Total length
            int value  = 0;
            int index  = 0;
            foreach (string str in strs) // Combined total count + exclusive prefix sum
            {
                result.offsets[index++] = value;
                Debug.Log(value);
                length += str.Length;
                value  += str.Length;
            }
            result.offsets[^1] = length;

            index = 0;
            result.pool  = new NativeArray<char>(length, allocator);
            var poolSpan = result.pool.AsSpan();
            foreach (string str in strs)
            {
                Range range = result.offsets[index]..result.offsets[index + 1];
                str.AsSpan().CopyTo(poolSpan[range]);
                index++;
            }

            return result;
        }

        public void Dispose()
        {
            pool.Dispose();
            pool = default;

            offsets.Dispose();
            offsets = default;
        }
    }

    private const int WordTypeOffsetSize = (int) WordType.TypeCount + 1;
    // Stores an exclusive prefix sum inline, no heap ;)
    private unsafe fixed int WordTypeOffsetsRaw[WordTypeOffsetSize];

    // Heap
    private StringPool rawPool;
    private StringPool englishPool;
    private StringPool unicodePool;

    private unsafe readonly Span<int> WordTypeOffsets
    {
        get { fixed (int* ptr = WordTypeOffsetsRaw) return new Span<int>(ptr, WordTypeOffsetSize); }
    }

    public static WordEncoder Create(in ReadOnlySpan<DictEntry> entries, Allocator allocator)
    {
        WordEncoder encoder = new();

        // Counting Sort (Prelim Pass, Over Word Type)
        Span<int> histogram = encoder.WordTypeOffsets;
        histogram.Fill(0); // Clearing memory
        for (int i = 0; i < entries.Length; i++)
        {
            int logIdx = ((int) entries[i].wordType).Log2();
            histogram[logIdx]++;
        }
        histogram[^1] = entries.Length; // Meant for iterators. Exclude from iteration.

        // Exclusive Prefix Sum
        int total = 0;
        for (int i = 0; i < (int) WordType.TypeCount; i++)
        {
            int value    = histogram[i];
            histogram[i] = total;
            total       += value;
        }

        DictEntry[] sortedEntries = new DictEntry[entries.Length];
        {
            // Make copy, do not want to actually mutate over WordTypeOffsets
            Span<int> exclusiveScan = stackalloc int[(int) WordType.TypeCount];
            histogram[..^1].CopyTo(exclusiveScan);
            for (int i = 0; i < entries.Length; i++)
            {
                int logIdx = ((int) entries[i].wordType).Log2();
                sortedEntries[exclusiveScan[logIdx]++] = entries[i];
            }
        }

        // Loop over each prefix sum range and does lexicographical sorting
        for (int i = 0; i < (int) WordType.TypeCount; i++)
        {
            Range range = histogram[i]..histogram[i + 1];
            ArraySegment<DictEntry> segment = sortedEntries[range];
            // Is not very efficient but eh, fine probably for now
            var sortedArr = segment.OrderBy(x => x.rawString).ThenBy(x => x.rawString.Length).ToArray();
            sortedArr.CopyTo(segment.AsSpan());
        }

        encoder.rawPool     = StringPool.Create(sortedEntries.Select(x => x.rawString),          allocator);
        encoder.englishPool = StringPool.Create(sortedEntries.Select(x => x.englishTranslation), allocator);
        encoder.unicodePool = StringPool.Create(sortedEntries.Select(x => x.unicodeString),      allocator);
        return encoder;
    }

    public void Dispose()
    {
        rawPool.Dispose();
        rawPool = default;

        englishPool.Dispose();
        englishPool = default;

        unicodePool.Dispose();
        unicodePool = default;
    }
}