using System;

using Unity.Collections;

using UnityEngine;

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

namespace Impl
{
    public struct SignData
    {
        public Range range;
        public char unicodeChar;

        public SignData(in Range range, char unicodeChar)
        {
            this.range = range;
            this.unicodeChar = unicodeChar;
        }
    }

    public struct CompoundTable
    {
        public FixedList32Bytes<char> signData;
        public int compoundIndex; // Index to compoundData array

        public CompoundTable(in CompoundSign sign, int index) : this(sign.mappedChars, index) { }

        public CompoundTable(int[] mappedChars, int compoundIndex)
        {
            Debug.Assert(mappedChars.Length > 1);
            signData = new();
            for (int i = 0; i < mappedChars.Length; i++)
            {
                signData.Add((char)mappedChars[i]);
            }

            this.compoundIndex = compoundIndex;
        }
    }
}