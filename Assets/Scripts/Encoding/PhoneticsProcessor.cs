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
        this.range = range;
        this.unicodeChar = unicodeChar;
    }
}

public static class StringExts
{
    public static Range[] RangeSplit(this string str, char delimiter)
    {
        int count = 1;
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
    public NativeArray<SignData> standardData;
    public NativeArray<SignData> compoundData;

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

        Span<char> standardSpan = standardSignData.AsSpan();
        Span<Char> compoundSpan = compoundSignData.AsSpan();

        int standardOffset = 0, compoundOffset = 0;
        for (int i = 0; i < standardSigns.Length; i++)
        {
            ref readonly var sign = ref standardSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.phonetics;

            Range range = standardOffset..(standardOffset + phoneticStr.Length);
            phoneticStr.CopyTo(standardSpan[standardOffset..]);
            standardData[i] = new SignData(range, sign.unicodeChar);

            standardOffset += phoneticStr.Length;
        }

        for (int i = 0; i < compoundSigns.Length; i++)
        {
            ref readonly var sign = ref compoundSigns[i];
            ReadOnlySpan<char> phoneticStr = sign.combinedString;

            Range range = compoundOffset..(compoundOffset + phoneticStr.Length);
            phoneticStr.CopyTo(compoundSpan[range]);
            compoundData[i] = new SignData(range, sign.unicodeChar);

            compoundOffset += phoneticStr.Length;
        }
    }

    public readonly string Translate(string input)
    {
        ReadOnlySpan<char> span = input;
        Range[] ranges    = input.RangeSplit(';'); // Maybe?
        //Span<char> output = stackalloc char[span.Length];
        UnsafeList<char> temp = new(0, Allocator.Temp);
        for (int i = 0; i < ranges.Length; i++)
        {
            //output[i] = span[i];
            unsafe
            {
                var subspan = span[ranges[i]];
                fixed (char* ptr = subspan)
                {
                    temp.AddRange(ptr, subspan.Length);
                }
            }
        }

        //return new string(output);
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
    }
}