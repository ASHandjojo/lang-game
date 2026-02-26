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

    // Ranges into contiguous memory (basically, using pooled strings)
    private NativeArray<SignData> standardData;
    private NativeArray<SignData> compoundData; // Strategy/usage is subject to change! Could add other part for 

    private NativeArray<PackedStandard> standardMulti;
    // Nested structure, contains a mapping between the first unicode character in a...
    // Compound sign to all of the characters & the index to compound data
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<char, NativeList<CompoundTable>> compoundPrefixMap;

    public Processor(in ReadOnlySpan<StandardSign> standardSigns, in ReadOnlySpan<CompoundSign> compoundSigns, Allocator allocator)
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
        Debug.Log($"Multi Standard Length: {multiStandardLength}");

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            compoundStrLength += compoundSigns[i].combinedString.Length;
        }

        // Initializing structures
        standardSignData = new NativeArray<char>(standardStrLength, allocator);
        compoundSignData = new NativeArray<char>(compoundStrLength, allocator);

        standardData = new NativeArray<SignData>(standardSigns.Length, allocator);
        compoundData = new NativeArray<SignData>(compoundSigns.Length, allocator);

        standardMulti     = new NativeArray<PackedStandard>(multiStandardLength, allocator);
        compoundPrefixMap = new NativeHashMap<char, NativeList<CompoundTable>>(compoundSigns.Length, allocator);

        Span<char> standardSpan = standardSignData.AsSpan();
        Span<char> compoundSpan = compoundSignData.AsSpan();

        InitStandardSigns(standardSpan, standardSignSort);
        InitCompoundSigns(compoundSpan, compoundSignSort, allocator);
    }

    /// <summary>
    /// Populating standard sign backing arrays.
    /// </summary>
    /// <param name="standardSpan"></param>
    /// <param name="standardSigns"></param>
    private void InitStandardSigns(in Span<char> standardSpan, in ReadOnlySpan<StandardSign> standardSigns)
    {
        int standardOffset = 0;
        int multiIndex     = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly StandardSign sign = ref standardSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.phonetics;

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[range]);
            standardData[i] = new SignData(range, (char) sign.mappedChar);

            if (phoneticStr.Length > 1) 
            {
                standardMulti[multiIndex++] = new PackedStandard(phoneticStr[0], phoneticStr[1], index: i);
            }
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
    /// A safe method of finding SignData structs based off of a sign character input.
    /// </summary>
    /// <param name="signChars">The character input to search for.</param>
    /// <param name="sign">If found, the corresponding SignData struct.</param>
    /// <returns>Whether the standard sign exists.</returns>
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

    private readonly bool TryFind(char mappedChar, out SignData sign)
    {
        for (int i = 0; i < standardData.Length; i++)
        {
            if (mappedChar == standardData[i].unicodeChar)
            {
                sign = standardData[i];
                return true;
            }
        }

        sign = default;
        return false;
    }

    /// <summary>
    /// Finds based off of input Unicode character.
    /// </summary>
    /// <param name="signChar"></param>
    /// <returns></returns>
    public readonly bool TryGetStringStandard(char signChar, out ReadOnlySpan<char> signChars)
    {
        for (int i = 0; i < standardData.Length; i++)
        {
            if (signChar == standardData[i].unicodeChar)
            {
                signChars = standardSignData.AsReadOnlySpan()[standardData[i].range];
                return true;
            }
        }

        signChars = default;
        return false;
    }

    public readonly bool TryGetStringStandard(char signChar, out string signChars) => TryGetStringStandard(signChar, out signChars);

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
            bool isEqual  = true;
            int signCount = compoundTable.signData.Length;

            // If the current sign position + the sign data count of the compound sign @ compoundIdx is out of bounds, skip
            if (signCount > input.Length || maxMatchSize >= signCount)
            {
                continue;
            }
            Debug.Log($"Sign Count: {signCount} | Max Match Size: {maxMatchSize}");
            for (int elementIdx = 1; elementIdx < signCount && isEqual; elementIdx++)
            {
                char inputSign   = input[elementIdx];
                char compareSign = compoundTable.signData[elementIdx];
                Debug.Log($"Chars at {elementIdx}: Input: {inputSign} | Compare: {compareSign}");
                isEqual = isEqual && inputSign == compareSign;
            }

            if (isEqual)
            {
                Debug.Log("Found!");
                currentTable = compoundTable;
                maxMatchSize = signCount;
            }
        }
        compoundTableOut = currentTable;
        return maxMatchSize > 0;
    }

    /// <summary>
    /// Translates a delimited string of phonetics characters into their corresponding Unicode.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A string with all valid phonetics converted to the specified mapped Unicode characters..</returns>
    public readonly string Translate(string input)
    {
        ReadOnlySpan<char> span = input.ToLower(); // Converts to span (much faster)
        unsafe
        {
            UnsafeList<char> basePass     = BasePass(span);
            UnsafeList<char> compoundPass = CompoundPass(new ReadOnlySpan<char>(basePass.Ptr, basePass.Length));
            return new string(compoundPass.Ptr, 0, compoundPass.Length);
        }
    }

    /// <summary>
    /// Handles base pass of unicode conversion for all base characters; including single and multiple character phonetics.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    private readonly UnsafeList<char> BasePass(in ReadOnlySpan<char> span)
    {
        // Deliberate probable overestimate of size, fine lol
        UnsafeList<char> addedChars = new(initialCapacity: span.Length, Allocator.Temp);
        for (int i = 0; i < span.Length; i++)
        {
            char mappedChar    = span[i];
            bool possibleMulti = false; // Find if any match the first characters (could be multi)
            // Compare first character
            for (int j = 0; j < standardMulti.Length && !possibleMulti && i < span.Length - 1; j++)
            {
                possibleMulti = mappedChar == standardMulti[j][0];
            }
            if (possibleMulti) // Could be multi
            {
                int index = -1;
                PackedStandard packed = new(mappedChar, span[i + 1], 0); // Index is dummy
                for (int j = 0; j < standardMulti.Length; j++)
                {
                    if (standardMulti[j].Equals(packed))
                    {
                        index = j;
                        break;
                    }
                }
                if (index >= 0) // If key is found
                {
                    int standardIndex = standardMulti[index].Index;
                    char unicodeStr   = standardData[standardIndex].unicodeChar;
                    addedChars.AddNoResize(unicodeStr);
                    i++;
                }
                else // Otherwise, insert as normal
                {
                    addedChars.AddNoResize(mappedChar);
                }
            }
            else // Standard case, copy unchanged
            {
                addedChars.AddNoResize(mappedChar);
            }
        }
        return addedChars;
    }

    private readonly UnsafeList<char> CompoundPass(in ReadOnlySpan<char> span)
    {
        // Deliberate probable overestimate of size, fine lol
        UnsafeList<char> addedChars = new(initialCapacity: span.Length, Allocator.Temp);
        for (int i = 0; i < span.Length; i++)
        {
            char mappedChar = span[i];
            // Continues (skips current iter) if it is an invalid character
            // (This shouldn't happen with the on-screen keyboard)
            bool hasCompound = compoundPrefixMap.ContainsKey(mappedChar);
            Debug.Log($"Has Compound: {hasCompound}");
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

            addedChars.AddNoResize(mappedChar);
        }

        return addedChars;
    }

    public void Dispose()
    {
        standardSignData.Dispose();
        compoundSignData.Dispose();

        standardData.Dispose();
        compoundData.Dispose();

        standardMulti.Dispose();

        // Disposes nested lists first before disposing hash map
        foreach (var arr in compoundPrefixMap.GetValueArray(Allocator.Temp))
        {
            arr.Dispose();
        }
        compoundPrefixMap.Dispose();
    }
}