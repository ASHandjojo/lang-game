using System;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;

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
    private NativeArray<int>  offsets; // Exclusive prefix sum

    public readonly bool IsValid => pool.IsCreated && offsets.IsCreated;

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
        get => Pool[offsets[index]..offsets[index + 1]];
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

        int totalCharLength = 0;
        for (int i = 0; i < lengthOffsets.Length - 1; i++)
        {
            totalCharLength += (lengthOffsets[i + 1] - lengthOffsets[i]) * (i + 1);
        }
        StringPool result = new()
        {
            offsets = new NativeArray<int>(lengthOffsets.Length, allocator, NativeArrayOptions.UninitializedMemory),
            pool    = new NativeArray<ushort>(totalCharLength,   allocator, NativeArrayOptions.UninitializedMemory)
        };
        lengthOffsets.CopyTo(result.offsets);

        T selectFunc = new();
        var poolSpan = result.pool.AsSpan();
        for (int x = 0, offset = 0; x < lengthOffsets.Length - 1; x++)
        {
            for (int i = lengthOffsets[x]; i < lengthOffsets[x + 1]; i++)
            {
                var dst = poolSpan[offset..(offset + x + 1)];
                var src = selectFunc.Select(entries[i]);
                src.CopyTo(dst);
                offset    += x + 1;
            }
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
        }
        var prefixSum = SortEntries(entries, maxLength);

        encoder.rawPool     = StringPool.Create<RawPhoneticsSelector>(entries, prefixSum, allocator);
        encoder.unicodePool = StringPool.Create<UnicodeStrSelector>(entries,   prefixSum, allocator);
        encoder.englishPool = StringPool.Create<EnglishTransSelector>(entries, prefixSum, allocator);
        return encoder;
    }

    public NativeArray<WordNode> Parse(in ReadOnlySpan<char> str, Allocator allocator)
    {
        if (str.IsEmpty) // Short-circuit if empty
        {
            return default;
        }
        int wordCount = str.ConvertU16().WordCount(' ');
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
            //nodes[index++] = new WordNode();
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