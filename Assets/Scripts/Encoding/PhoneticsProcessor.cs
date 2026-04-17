using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

using Impl;

public struct PhoneticProcessor : IDisposable
{
    // Made to basically throw all the phonetics into two giant buffers
    private NativeArray<char> standardSignData;
    private NativeArray<char> compoundSignData; // Under Combined String

    // Ranges into contiguous memory (basically, using pooled strings)
    private NativeArray<SignData> standardData;
    private NativeArray<SignData> compoundData;

    // Nested structure, contains a mapping between the first unicode character in a...
    // Compound sign to all of the characters & the index to compound data
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<char, NativeList<CompoundTable>> compoundPrefixMap;

    public PhoneticProcessor(in ReadOnlySpan<StandardSign> standardSigns, in ReadOnlySpan<CompoundSign> compoundSigns, Allocator allocator)
    {
        StandardSign[] standardSignSort = ProcessorExtMethods.Sort(standardSigns);
        CompoundSign[] compoundSignSort = ProcessorExtMethods.Sort(compoundSigns);

        // Getting total lengths for string data
        int standardStrLength = 0, compoundStrLength = 0, multiStandardLength = 0;

        for (int i = 0; i < standardSigns.Length; i++)
        {
            int phoneticsLength  = standardSigns[i].phonetics.Length;
            standardStrLength   += phoneticsLength;
            multiStandardLength += (phoneticsLength > 1).CastAsInt32();
        }

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            compoundStrLength += compoundSigns[i].combinedString.Length;
        }

        // Initializing structures
        standardSignData = new NativeArray<char>(standardStrLength, allocator);
        compoundSignData = new NativeArray<char>(compoundStrLength, allocator);

        standardData = new NativeArray<SignData>(standardSigns.Length, allocator);
        compoundData = new NativeArray<SignData>(compoundSigns.Length, allocator);

        compoundPrefixMap = new NativeHashMap<char, NativeList<CompoundTable>>(compoundSigns.Length, allocator);

        Span<char> standardSpan = standardSignData.AsSpan();
        Span<char> compoundSpan = compoundSignData.AsSpan();

        InitStandardSigns(standardSpan, standardSignSort);
        InitCompoundSigns(compoundSpan, compoundSignSort, allocator);
    }

    public readonly bool IsValid => standardSignData.IsCreated && compoundSignData.IsCreated &&
        standardData.IsCreated && compoundData.IsCreated && compoundPrefixMap.IsCreated;

    /// <summary>
    /// Populating standard sign backing arrays.
    /// </summary>
    /// <param name="standardSpan"></param>
    /// <param name="standardSigns"></param>
    private void InitStandardSigns(in Span<char> standardSpan, in ReadOnlySpan<StandardSign> standardSigns)
    {
        int standardOffset = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly StandardSign sign = ref standardSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.phonetics;

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[range]);

            standardData[i] = new SignData(range, (char) sign.mappedChar);
            standardOffset += phoneticStr.Length;
        }
    }

    /// <summary>
    /// Populating compound sign backing arrays + populating prefix map.
    /// </summary>
    /// <param name="compoundSpan"></param>
    /// <param name="compoundSigns"></param>
    private void InitCompoundSigns(in Span<char> compoundSpan, in ReadOnlySpan<CompoundSign> compoundSigns, Allocator allocator)
    {
        int compoundOffset = 0;
        for (int i = 0; i < compoundSigns.Length; i++)
        {
            ref readonly CompoundSign sign = ref compoundSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.combinedString;

            Range range = compoundOffset..(compoundOffset + phoneticStr.Length);
            phoneticStr.CopyTo(compoundSpan[range]);
            compoundData[i] = new SignData(range, (char) sign.mappedChar);

            char firstMappedChar = (char) sign.mappedChars[0];
            CompoundTable table  = new(sign, i);
            if (compoundPrefixMap.ContainsKey(firstMappedChar)) // If already has entry, add to list @ prefix map location
            {
                compoundPrefixMap[firstMappedChar].Add(table);
            }
            else // Otherwise, create list, add compound table to list, add list to table
            {
                NativeList<CompoundTable> tableList = new(1, allocator);
                tableList.AddNoResize(table);

                compoundPrefixMap.Add(firstMappedChar, tableList);
            }

            compoundOffset += phoneticStr.Length;
        }

        foreach (var pair in compoundPrefixMap)
        {
            ref NativeList<CompoundTable> tableList = ref pair.Value;
            unsafe
            {
                Span<CompoundTable> tableSpan = new(tableList.GetUnsafePtr(), tableList.Length);
                PhoneticsSortingMethods.Sort(tableSpan);
            }
        }
    }

    /// <summary>
    /// A safe method of determining, if any; the corresponding compound sign.
    /// </summary>
    /// <param name="rootSign"></param>
    /// <param name="input"></param>
    /// <param name="ranges"></param>
    /// <param name="compoundTableOut"></param>
    /// <returns>Whether there is a compound present.</returns>
    private readonly bool TryGetCompound(char unicodeChar, in ReadOnlySpan<char> input, out CompoundTable compoundTableOut)
    {
        // Target should want to find the *longest* compound match
        int maxMatchSize = 0;
        CompoundTable currentTable = default;

        // Gets compound data
        NativeList<CompoundTable> compoundTables = compoundPrefixMap[unicodeChar];
        // Iterate through prefixes
        for (int compoundIdx = 0; compoundIdx < compoundTables.Length; compoundIdx++)
        {
            // Iterate through compound sign
            ref readonly CompoundTable compoundTable = ref compoundTables.ElementAt(compoundIdx);
            int signCount = compoundTable.signData.Length;

            // If the current sign position + the sign data count of the compound sign @ compoundIdx is out of bounds, skip;
            // or if the size is smaller than the current maximum, skip
            if (signCount > input.Length || maxMatchSize >= signCount)
            {
                continue;
            }

            // Does a character-wise comparison over the compound characters.
            bool isEqual = true;
            for (int elementIdx = 1; elementIdx < signCount && isEqual; elementIdx++)
            {
                char inputSign   = input[elementIdx];
                char compareSign = compoundTable.signData[elementIdx];
                isEqual = isEqual && inputSign == compareSign;
            }

            if (isEqual)
            {
                currentTable = compoundTable;
                maxMatchSize = signCount;
            }
        }
        compoundTableOut = currentTable;
        return maxMatchSize > 0;
    }

    public readonly string Translate(in ReadOnlySpan<char> span)
    {
        unsafe
        {
            UnsafeList<char> compoundPass = CompoundPass(span);
            return new string(compoundPass.Ptr, 0, compoundPass.Length);
        }
    }


    /// <summary>
    /// Translates a delimited string of phonetics characters into their corresponding Unicode.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A string with all valid phonetics converted to the specified mapped Unicode characters..</returns>
    public readonly string Translate(string input) => Translate(input.AsSpan());

    private readonly UnsafeList<char> CompoundPass(in ReadOnlySpan<char> span)
    {
        // Deliberate probable overestimate of size
        UnsafeList<char> addedChars = new(initialCapacity: span.Length, Allocator.Temp);
        for (int i = 0; i < span.Length; i++)
        {
            char mappedChar = span[i];
            // Continues (skips current iter) if it is an invalid character
            // (This shouldn't happen with the on-screen keyboard)
            bool hasCompound = compoundPrefixMap.ContainsKey(mappedChar);
            // Checks whether the current key *could* be compound.
            if (hasCompound)
            {
                // Checks with respect to the current delimited string to the end of the array.
                bool isCompound = TryGetCompound(mappedChar, span[i..], out CompoundTable compoundTable);
                if (isCompound)
                {
                    // Changes the character to compound character if there is a valid compound representation
                    mappedChar = compoundData[compoundTable.compoundIndex].unicodeChar;
                    // Offsets by the number of characters a compound sign consumes
                    i += compoundTable.signData.Length - 1;
                }
            }
            // Push back found mapped char (whether standard or compound)
            if (mappedChar != '\u000a')
            {
                addedChars.AddNoResize(mappedChar);
            }
        }
        return addedChars;
    }

    public void Dispose()
    {
        if (!IsValid)
        {
            Debug.LogWarning($"Dispose was called on an invalid {nameof(PhoneticProcessor)} instance.");
            return;
        }
        standardSignData.Dispose();
        compoundSignData.Dispose();

        standardData.Dispose();
        compoundData.Dispose();

        // Disposes nested lists first before disposing hash map
        foreach (var arr in compoundPrefixMap.GetValueArray(Allocator.Temp))
        {
            arr.Dispose();
        }
        compoundPrefixMap.Dispose();
    }
}