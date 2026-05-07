using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

using Impl;

[BurstCompile]
public struct PhoneticProcessor : IDisposable
{
    // Made to basically throw all the data into two giant buffers
    private NativeArray<ushort> standardSignData;
    private NativeArray<ushort> compoundSignData; // Under Combined String

    // Ranges into contiguous memory (basically, using pooled strings)
    private NativeArray<SignData> standardData;
    private NativeArray<SignData> compoundData;

    // Nested structure, contains a mapping between the first unicode character in a...
    // Compound sign to all of the characters & the index to compound data
    [NativeDisableContainerSafetyRestriction]
    private NativeHashMap<ushort, NativeList<CompoundTable>> compoundPrefixMap;

    public static PhoneticProcessor Create(in ReadOnlySpan<StandardSign> standardSigns, in ReadOnlySpan<CompoundSign> compoundSigns, Allocator allocator)
    {
        PhoneticProcessor output = new();

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
        output.standardSignData = new NativeArray<ushort>(standardStrLength, allocator);
        output.compoundSignData = new NativeArray<ushort>(compoundStrLength, allocator);

        output.standardData = new NativeArray<SignData>(standardSigns.Length, allocator);
        output.compoundData = new NativeArray<SignData>(compoundSigns.Length, allocator);

        output.compoundPrefixMap = new NativeHashMap<ushort, NativeList<CompoundTable>>(compoundSigns.Length, allocator);

        Span<ushort> standardSpan = output.standardSignData.AsSpan();
        Span<ushort> compoundSpan = output.compoundSignData.AsSpan();

        output.InitStandardSigns(standardSpan, standardSignSort);
        output.InitCompoundSigns(compoundSpan, compoundSignSort, allocator);

        return output;
    }

    public readonly bool IsValid => standardSignData.IsCreated && compoundSignData.IsCreated &&
        standardData.IsCreated && compoundData.IsCreated && compoundPrefixMap.IsCreated;

    /// <summary>
    /// Populating standard sign backing arrays.
    /// </summary>
    /// <param name="standardSpan"></param>
    /// <param name="standardSigns"></param>
    [BurstDiscard]
    private void InitStandardSigns(in Span<ushort> standardSpan, in ReadOnlySpan<StandardSign> standardSigns)
    {
        int standardOffset = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly StandardSign sign   = ref standardSigns[i];
            ReadOnlySpan<ushort> phoneticStr = sign.phonetics.AsSpan().ConvertU16();

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[range]);

            standardData[i] = new SignData(range, (ushort) sign.mappedChar);
            standardOffset += phoneticStr.Length;
        }
    }

    /// <summary>
    /// Populating compound sign backing arrays + populating prefix map.
    /// </summary>
    /// <param name="compoundSpan"></param>
    /// <param name="compoundSigns"></param>
    [BurstDiscard]
    private void InitCompoundSigns(in Span<ushort> compoundSpan, in ReadOnlySpan<CompoundSign> compoundSigns, Allocator allocator)
    {
        int compoundOffset = 0;
        for (int i = 0; i < compoundSigns.Length; i++)
        {
            ref readonly CompoundSign sign   = ref compoundSigns[i];
            ReadOnlySpan<ushort> phoneticStr = sign.combinedString.AsSpan().ConvertU16();

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
    private readonly bool TryGetCompound(ushort unicodeChar, in ReadOnlySpan<ushort> input, out CompoundTable compoundTableOut)
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
                ushort inputSign   = input[elementIdx];
                ushort compareSign = compoundTable.signData[elementIdx];
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

    public readonly UnsafeList<ushort> Translate(in ReadOnlySpan<ushort> span, Allocator allocator) => CompoundPass(span, allocator);

    [BurstDiscard]
    public readonly string TranslateManaged(in ReadOnlySpan<char> span)
    {
        unsafe
        {
            UnsafeList<ushort> compoundPass = CompoundPass(span.ConvertU16(), Allocator.Temp);
            return new string((char*) compoundPass.Ptr, 0, compoundPass.Length);
        }
    }


    /// <summary>
    /// Translates a delimited string of phonetics characters into their corresponding Unicode.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A string with all valid phonetics converted to the specified mapped Unicode characters..</returns>
    [BurstDiscard]
    public readonly string TranslateManaged(string input) => TranslateManaged(input.AsSpan());

    private readonly UnsafeList<ushort> CompoundPass(in ReadOnlySpan<ushort> span, Allocator allocator)
    {
        // Deliberate probable overestimate of size
        UnsafeList<ushort> addedChars = new(initialCapacity: span.Length, allocator);
        for (int i = 0; i < span.Length; i++)
        {
            ushort mappedChar = span[i];
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