using System;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;

using Impl;

namespace Impl
{
    [BurstCompile]
    public struct ParseMixedResult : IDisposable
    {
        public NativeArray<WordNode> words;
        // Should be used to write to disk.
        public NativeList<ushort> unicodeOutput;
        // Should be used to display from keyboard. Is formatted.
        public NativeList<ushort> displayOutput;

        public readonly bool IsValid => words.IsCreated && unicodeOutput.IsCreated && displayOutput.IsCreated;

        public ParseMixedResult(in NativeArray<WordNode> words, in NativeList<ushort> unicodeOutput, in NativeList<ushort> displayOutput)
        {
            Debug.Assert(words.IsCreated);
            Debug.Assert(unicodeOutput.IsCreated);
            Debug.Assert(displayOutput.IsCreated);

            this.words = words;
            this.unicodeOutput = unicodeOutput;
            this.displayOutput = displayOutput;
        }

        public void Dispose()
        {
            words.Dispose();
            words = default;

            unicodeOutput.Dispose();
            unicodeOutput = default;
            displayOutput.Dispose();
            displayOutput = default;
        }
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
        }
        var prefixSum = SortEntries(entries, maxLength);

        encoder.unicodePool = LexPool.Create<UnicodeStrSelector>(entries, prefixSum, allocator);
        encoder.rawPool     = StringPool.Create<RawPhoneticsSelector>(entries, allocator);
        encoder.englishPool = StringPool.Create<EnglishTransSelector>(entries, allocator);

        for (int i = 0; i < entries.Length; i++)
        {
            encoder.wordTypes[i] = entries[i].WordType;
        }
        return encoder;
    }

    private readonly bool TryFind(in ReadOnlySpan<ushort> str, out WordNode node)
    {
        node = default;
        return true;
    }

    public readonly WordNode ParseSingle(in ReadOnlySpan<ushort> str)
    {
        bool isPresent = unicodePool.IsPresent(str, out int strIndex);
        return isPresent ? WordNode.Create(str, wordTypes[strIndex], strIndex) : WordNode.Unknown;
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
        SplitIterator iter          = SplitIterator.Create(str, ' ');

        int index = 0;
        while (iter.MoveNext())
        {
            ReadOnlySpan<ushort> span = iter.Current;
            if (span.IsEmpty)
            {
                continue;
            }
            nodes[index++] = ParseSingle(span);
        }
        return nodes;
    }

    public unsafe readonly ParseMixedResult ParseMixed(in ReadOnlySpan<ushort> phoneticsStr, in PhoneticProcessor processor, Allocator allocator, ushort engSeparator = '|')
    {
        const ushort WordSeparator = ' ';
        if (phoneticsStr.Length == 0)
        {
            return default;
        }
        int wordCount = phoneticsStr.WordCount(WordSeparator);
        int charCount = phoneticsStr.CharCount(engSeparator);

        NativeArray<WordNode> nodes = new(math.max(wordCount - charCount, 0), allocator);
        SplitIterator wordIter      = SplitIterator.Create(phoneticsStr, WordSeparator);

        NativeList<ushort> unicodeOutput = new(phoneticsStr.Length, allocator);
        NativeList<ushort> displayOutput = new(phoneticsStr.Length, allocator);
        int wordIdx = 0, nodeIdx = 0;
        while (wordIter.MoveNext())
        {
            ReadOnlySpan<ushort> word = wordIter.Current;
            if (word.IsEmpty)
            {
                continue;
            }

            if (wordIdx > 0)
            {
                displayOutput.Add(' ');
                unicodeOutput.Add(' ');
            }
            if (word[0] != engSeparator)
            {
                var wordConv = processor.Translate(word, Allocator.Temp);
                unicodeOutput.AddRange(wordConv.Ptr, wordConv.Length);
                // Display
                var convSpan = new ReadOnlySpan<ushort>(wordConv.Ptr, wordConv.Length);

                nodes[nodeIdx++] = ParseSingle(convSpan);
                displayOutput.AddRange(wordConv.Ptr, wordConv.Length);
            }
            else
            {
                fixed (ushort* wordPtr = word) { unicodeOutput.AddRange(wordPtr, word.Length); }
                // Display
                // NOTE: WATCH STRING LITERAL CONV FOR BURST CORRECTNESS
                ReadOnlySpan<ushort> lhsTag = "<font=\"Harmony SDF\">".AsSpan().ConvertU16();
                ReadOnlySpan<ushort> rhsTag = "</font>".AsSpan().ConvertU16();

                fixed (ushort* lhsPtr  = lhsTag) { displayOutput.AddRange(lhsPtr,      lhsTag.Length);   }
                fixed (ushort* wordPtr = word)   { displayOutput.AddRange(wordPtr + 1, word.Length - 1); }
                fixed (ushort* rhsPtr  = rhsTag) { displayOutput.AddRange(rhsPtr,      rhsTag.Length);   }
            }
            wordIdx++;
        }
        return new ParseMixedResult(nodes, unicodeOutput, displayOutput);
    }

    public readonly bool TryGetEnglish(in WordNode node, out ReadOnlySpan<ushort> english)
    {
        if (Hint.Unlikely(!node.IsValid))
        {
            english = default;
            return false;
        }
        english = englishPool[node.WordIndex];
        return true;
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