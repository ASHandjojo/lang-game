using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

using Impl;

public struct SignData
{
    public Range range;
    public int unicodeChar;

    public SignData(in Range range, int unicodeChar)
    {
        this.range       = range;
        this.unicodeChar = unicodeChar;
    }
}

public static class StringExts
{
    public static Range[] RangeSplit(this string str, char delimiter)
    {
        int count = 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == delimiter)
            {
                count++;
            }
        }

        Range[] output = new Range[count];
        int index = 0, prevOffset = 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == delimiter)
            {
                output[index++] = prevOffset..i;
                prevOffset = i + 1;
            }
        }

        return output;
    }
}

public struct Processor : IDisposable
{
    // Made to basically throw all the data into two giant buffers
    private NativeArray<char> standardSignData;
    private NativeArray<char> compoundSignData; // Under Combined String

    // Ranges into contiguous memory (basically, using pooled strings
    private NativeArray<SignData> standardData;
    private NativeArray<SignData> compoundData; // Strategy/usage is subject to change!

    private struct CompoundTable
    {
        public FixedList32Bytes<char> signData;

        public int compoundIndex; // Index to compoundData array

        public CompoundTable(in CompoundSign sign, int index) : this(sign.mappedChars, index) {}

        public CompoundTable(int[] mappedChars, int compoundIndex)
        {
            Debug.Assert(mappedChars.Length > 1);
            signData = new();
            for (int i = 0; i < mappedChars.Length; i++)
            {
                signData.Add((char) mappedChars[i]);
            }

            this.compoundIndex = compoundIndex;
        }
    }

    // Maps first unicode (char) value to all possible prefixes
    // Could make linear later (as a NativeArray) with prefix offsets (similar to counting sorts), eh tho
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<char, NativeList<CompoundTable>> prefixMap;

    public Processor(in ReadOnlySpan<StandardSign> standardSigns, in ReadOnlySpan<CompoundSign> compoundSigns)
    {
        StandardSign[] standardSignSort = ProcessorExtMethods.Sort(standardSigns);
        CompoundSign[] compoundSignSort = ProcessorExtMethods.Sort(compoundSigns);

        int standardStrLength = 0, compoundStrLength = 0;

        for (int i = 0; i < standardSigns.Length; i++)
        {
            standardStrLength += standardSigns[i].phonetics.Length;
        }

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            compoundStrLength += compoundSigns[i].combinedString.Length;
        }

        standardSignData = new NativeArray<char>(standardStrLength, Allocator.Persistent);
        compoundSignData = new NativeArray<char>(compoundStrLength, Allocator.Persistent);

        standardData = new NativeArray<SignData>(standardSigns.Length, Allocator.Persistent);
        compoundData = new NativeArray<SignData>(compoundSigns.Length, Allocator.Persistent);

        prefixMap = new NativeHashMap<char, NativeList<CompoundTable>>(compoundSigns.Length, Allocator.Persistent);

        Span<char> standardSpan = standardSignData.AsSpan();
        Span<char> compoundSpan = compoundSignData.AsSpan();

        int standardOffset = 0, compoundOffset = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly var sign = ref standardSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.phonetics;

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[range]);
            standardData[i] = new SignData(range, sign.mappedChar);

            standardOffset += phoneticStr.Length;
        }

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            ref readonly var sign = ref compoundSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.combinedString;

            Range range = compoundOffset..(compoundOffset + phoneticStr.Length);
            phoneticStr.CopyTo(compoundSpan[range]);
            compoundData[i] = new SignData(range, sign.mappedChar);

            if (prefixMap.ContainsKey((char) sign.mappedChars[0]))
            {
                prefixMap[(char) sign.mappedChars[0]].Add(new CompoundTable(sign, i));
            }
            else
            {
                NativeList<CompoundTable> tableList = new(1, Allocator.Persistent);
                tableList.AddNoResize(new CompoundTable(sign, i));

                prefixMap.Add((char) sign.mappedChars[0], tableList);
            }
            Debug.Log($"Adding {sign.mappedChar}");

            compoundOffset += phoneticStr.Length;
        }
    }

    private readonly bool TryFind(in ReadOnlySpan<char> signChars, out SignData sign)
    {
        Span<char> standardSpan = standardSignData.AsSpan();

        for (int i = 0; i < standardData.Length; i++)
        {
            ReadOnlySpan<char> compareStr = standardSpan[standardData[i].range];
            if (compareStr.SequenceEqual(signChars))
            {
                sign = standardData[i];
                return true;
            }
        }

        sign = default;
        return false;
    }

    private readonly bool TryGetCompound(in SignData rootSign, in ReadOnlySpan<char> input, Range[] ranges, int rangeIndex, out CompoundTable compoundSign)
    {
        // Gets compound data
        NativeList<CompoundTable> compoundTables = prefixMap[(char) rootSign.unicodeChar];
        // Iterate through prefixes
        for (int compoundIdx = 0; compoundIdx < compoundTables.Length; compoundIdx++)
        {
            // Iterate through compound sign
            ref readonly CompoundTable compoundTable = ref compoundTables.ElementAt(compoundIdx);
            bool isEqual = true;

            for (int elementIdx = 1; elementIdx < compoundTable.signData.Length && isEqual; elementIdx++)
            {
                ReadOnlySpan<char> currentSpan = input[ranges[rangeIndex + elementIdx]];
                bool isCurrentValid = TryFind(currentSpan, out SignData currentSign);

                isEqual = isEqual && isCurrentValid && currentSign.unicodeChar == compoundTable.signData[elementIdx];
            }

            if (isEqual)
            {
                compoundSign = compoundTables[compoundIdx];
                return true;
            }
        }

        compoundSign = default;
        return false;
    }

    public readonly string Translate(string input)
    {
        ReadOnlySpan<char> span = input;
        Range[] ranges          = input.RangeSplit(';'); // Maybe?
        UnsafeList<char> temp   = new(0, Allocator.Temp);

        for (int rangeIdx = 0; rangeIdx < ranges.Length; rangeIdx++)
        {
            ReadOnlySpan<char> currentSpan = span[ranges[rangeIdx]];
            if (!TryFind(currentSpan, out SignData signData))
            {
                continue;
            }

            // Checks whether the current key *could* be compound.
            bool hasCompound = prefixMap.ContainsKey((char) signData.unicodeChar);
            if (hasCompound)
            {
                bool isCompound = TryGetCompound(signData, span, ranges, rangeIdx, out CompoundTable compoundTable);
                if (isCompound)
                {
                    temp.Add((char) compoundData[compoundTable.compoundIndex].unicodeChar);
                    rangeIdx += compoundTable.signData.Length - 1;
                }
                else
                {
                    temp.Add((char) signData.unicodeChar);
                }
            }
            else
            {
                temp.Add((char) signData.unicodeChar);
            }
        }

        unsafe
        {
            return new string(temp.Ptr, 0, temp.Length);
        }
    }

    public void Dispose()
    {
        standardSignData.Dispose();
        compoundSignData.Dispose();

        standardData.Dispose();
        compoundData.Dispose();

        foreach (var arr in prefixMap.GetValueArray(Allocator.Temp))
        {
            arr.Dispose();
        }

        prefixMap.Dispose();
    }
}