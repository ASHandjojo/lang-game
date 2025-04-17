using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

using Impl;

public struct Processor : IDisposable
{
    // Made to basically throw all the data into two giant buffers
    private NativeArray<char> standardSignData;
    private NativeArray<char> compoundSignData; // Under Combined String

    // Ranges into contiguous memory (basically, using pooled strings
    private NativeArray<SignData> standardData;
    private NativeArray<SignData> compoundData; // Strategy/usage is subject to change! Could add other part for 

    // Maps first unicode (char) value to all possible prefixes
    // Could make linear later (as a NativeArray) with prefix offsets (similar to counting sorts), eh tho
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<char, NativeList<CompoundTable>> prefixMap;

    public Processor(in ReadOnlySpan<StandardSign> standardSigns, in ReadOnlySpan<CompoundSign> compoundSigns)
    {
        StandardSign[] standardSignSort = ProcessorExtMethods.Sort(standardSigns);
        CompoundSign[] compoundSignSort = ProcessorExtMethods.Sort(compoundSigns);

        // Getting total lengths for string data
        int standardStrLength = 0, compoundStrLength = 0;

        for (int i = 0; i < standardSigns.Length; i++)
        {
            standardStrLength += standardSigns[i].phonetics.Length;
        }

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            compoundStrLength += compoundSigns[i].combinedString.Length;
        }

        // Initializing structures
        standardSignData = new NativeArray<char>(standardStrLength, Allocator.Persistent);
        compoundSignData = new NativeArray<char>(compoundStrLength, Allocator.Persistent);

        standardData = new NativeArray<SignData>(standardSigns.Length, Allocator.Persistent);
        compoundData = new NativeArray<SignData>(compoundSigns.Length, Allocator.Persistent);

        prefixMap = new NativeHashMap<char, NativeList<CompoundTable>>(compoundSigns.Length, Allocator.Persistent);

        Span<char> standardSpan = standardSignData.AsSpan();
        Span<char> compoundSpan = compoundSignData.AsSpan();

        // Populating standard sign backing arrays
        int standardOffset = 0, compoundOffset = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly var sign = ref standardSignSort[i];
            ReadOnlySpan<char> phoneticStr = sign.phonetics;

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[range]);
            standardData[i] = new SignData(range, (char) sign.mappedChar);

            standardOffset += phoneticStr.Length;
        }

        // Populating compound sign backing arrays + populating prefix map
        for (int i = 0; i < compoundSigns.Length; i++)
        {
            ref readonly var sign = ref compoundSignSort[i];
            ReadOnlySpan<char> phoneticStr = sign.combinedString;

            Range range = compoundOffset..(compoundOffset + phoneticStr.Length);
            phoneticStr.CopyTo(compoundSpan[range]);
            compoundData[i] = new SignData(range, (char) sign.mappedChar);

            char firstMappedChar = (char) sign.mappedChars[0];
            CompoundTable table  = new(sign, i);

            if (prefixMap.ContainsKey(firstMappedChar)) // If already has entry, add to list @ prefix map location
            {
                prefixMap[firstMappedChar].Add(table);
            }
            else // Otherwise, create list, add compound table to list, add list to table
            {
                NativeList<CompoundTable> tableList = new(1, Allocator.Persistent);
                tableList.AddNoResize(table);

                prefixMap.Add(firstMappedChar, tableList);
            }

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

    private readonly bool TryGetCompound(in SignData rootSign, in ReadOnlySpan<char> input, Range[] ranges, int rangeIndex, out CompoundTable compoundTableOut)
    {
        // Gets compound data
        NativeList<CompoundTable> compoundTables = prefixMap[rootSign.unicodeChar];
        // Iterate through prefixes
        for (int compoundIdx = 0; compoundIdx < compoundTables.Length; compoundIdx++)
        {
            // Iterate through compound sign
            ref readonly CompoundTable compoundTable = ref compoundTables.ElementAt(compoundIdx);
            bool isEqual  = true;
            int signCount = compoundTable.signData.Length;

            // If the current sign position + the sign data count of the compound sign @ compoundIdx is out of bounds, skip
            if (rangeIndex + signCount >= ranges.Length)
            {
                continue;
            }
            for (int elementIdx = 1; elementIdx < signCount && isEqual; elementIdx++)
            {
                ReadOnlySpan<char> currentSpan = input[ranges[rangeIndex + elementIdx]];
                bool isCurrentValid = TryFind(currentSpan, out SignData currentSign);

                isEqual = isEqual && isCurrentValid && currentSign.unicodeChar == compoundTable.signData[elementIdx];
            }

            if (isEqual)
            {
                compoundTableOut = compoundTable;
                return true;
            }
        }

        compoundTableOut = default;
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
            // Continues if it is an invalid character
            if (!TryFind(currentSpan, out SignData signData))
            {
                continue;
            }

            char mappedChar  = signData.unicodeChar; // Default value
            bool hasCompound = prefixMap.ContainsKey(mappedChar);
            // Checks whether the current key *could* be compound.
            if (hasCompound)
            {
                bool isCompound = TryGetCompound(signData, span, ranges, rangeIdx, out CompoundTable compoundTable);
                if (isCompound)
                {
                    // Changes the character to compound character if there is a valid compound representation
                    mappedChar = compoundData[compoundTable.compoundIndex].unicodeChar;
                    // Offsets by the number of characters a compound sign consumes
                    rangeIdx += compoundTable.signData.Length - 1;
                }
            }

            temp.Add(mappedChar);
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

        // Disposes nested lists first before disposing hash map
        foreach (var arr in prefixMap.GetValueArray(Allocator.Temp))
        {
            arr.Dispose();
        }

        prefixMap.Dispose();
    }
}