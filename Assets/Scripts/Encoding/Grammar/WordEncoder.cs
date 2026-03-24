using System;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;

using Impl;

[BurstCompile, StructLayout(LayoutKind.Sequential, Size = 16)]
public struct WordNode
{
    [NativeDisableUnsafePtrRestriction]
    private unsafe ushort* ptr;
    private ushort         length;
    private WordType       type;

    public unsafe readonly bool IsValid => ptr != null && length > 0;

    public readonly WordType WordType => type;

    public static WordNode Unknown => new()
    {
        type = WordType.Unknown
    };

    [BurstDiscard]
    public static unsafe WordNode Create(in ReadOnlySpan<char> span, WordType wordType)
    {
        Debug.Assert(!span.IsEmpty);
        Debug.Assert(wordType != WordType.Unknown);

        WordNode node = new()
        {
            type   = wordType,
            length = unchecked((ushort) span.Length)
        };
        fixed (char* ptr = span) node.ptr = (ushort*) ptr;
        return node;
    }

    public static unsafe WordNode Create(in ReadOnlySpan<ushort> span, WordType wordType)
    {
        Debug.Assert(!span.IsEmpty);
        Debug.Assert(wordType != WordType.Unknown);

        WordNode node = new()
        {
            type   = wordType,
            length = unchecked((ushort) span.Length)
        };
        fixed (ushort* ptr = span) node.ptr = ptr;
        return node;
    }
}

[BurstCompile]
public struct WordEncoder : IDisposable
{
    // Heap
    private LexPool unicodePool;

    private StringPool rawPool;
    private StringPool englishPool;

    private NativeArray<WordType> wordTypes;

    private static NativeArray<int> SortEntries(in NativeArray<DictEntryUnmanaged> entries, int maxLength)
    {
        // First, sort by length with a counting sort :)
        NativeArray<DictEntryUnmanaged> temp = new(entries.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        Span<int> lengthHist = stackalloc int[maxLength];
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
        NativeArray<int> sumCopy = new(maxLength + 1, Allocator.Temp);
        sumCopy[^1] = entries.Length;
        lengthHist.CopyTo(sumCopy.AsSpan());
        // Scatter back
        for (int i = 0; i < entries.Length; i++)
        {
            int key = entries[i].UnicodeString.Length - 1;
            temp[lengthHist[key]++] = entries[i];
        }

        // Then, sort by lexicographical
        for (int i = 0; i < maxLength; i++)
        {
            int count = sumCopy[i + 1] - sumCopy[i];
            if (count == 0)
            {
                continue;
            }
            var subArray = temp.GetSubArray(sumCopy[i], count);
            subArray.Sort();
        }
        temp.CopyTo(entries);
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

        encoder.unicodePool = LexPool.Create<UnicodeStrSelector>(entries, prefixSum, allocator);
        encoder.rawPool     = StringPool.Create<RawPhoneticsSelector>(entries, allocator);
        encoder.englishPool = StringPool.Create<EnglishTransSelector>(entries, allocator);

        for (int i = 0; i < entries.Length; i++)
        {
            Debug.Log(new string(encoder.englishPool[i].ConvertChar()));
        }
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
            ReadOnlySpan<ushort> span = iter.Current;
            Debug.Log($"Word: {span.ConvertChar().ToString()}");
            bool isPresent = unicodePool.IsPresent(span, out int strIndex);
            nodes[index++] = isPresent ? WordNode.Create(span, wordTypes[strIndex]) : default;
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