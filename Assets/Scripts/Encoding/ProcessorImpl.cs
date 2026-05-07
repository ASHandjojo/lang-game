using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;

namespace Impl
{
    [BurstCompile]
    public struct SignData
    {
        public Range  range;
        public ushort unicodeChar;

        public SignData(in Range range, ushort unicodeChar)
        {
            this.range       = range;
            this.unicodeChar = unicodeChar;
        }
    }

    [BurstCompile]
    public struct CompoundTable : IEquatable<CompoundTable>
    {
        /// <summary>
        /// A packed structure for storing compound phonetics.
        /// </summary>
        public struct Phonetics : IEquatable<Phonetics>
        {
            private const int MaxCompoundSize = 4;

            private unsafe fixed ushort Data[MaxCompoundSize];
            private readonly ushort length;

            public unsafe Phonetics(in ReadOnlySpan<ushort> phonetics)
            {
                Debug.Assert(phonetics.Length > 0 && phonetics.Length <= MaxCompoundSize);
                length = (ushort) phonetics.Length;

                for (int i = 0; i < length; i++)
                {
                    Data[i] = phonetics[i];
                }
            }

            public unsafe Phonetics(in ReadOnlySpan<int> phonetics)
            {
                Debug.Assert(phonetics.Length > 0);
                length = (ushort) phonetics.Length;

                for (int i = 0; i < length; i++)
                {
                    Data[i] = (ushort) phonetics[i];
                }
            }

            public readonly int Length => length;

            public unsafe readonly ReadOnlySpan<ushort> CompoundSpan
            {
                get { fixed (ushort* ptr = Data) return new ReadOnlySpan<ushort>(ptr, length); }
            }

            public unsafe ref readonly ushort this[int index]
            {
                get => ref Data[index];
            }

            public readonly bool Equals(Phonetics rhs) => length == rhs.length && CompoundSpan.SequenceEqual(rhs.CompoundSpan);
        }

        public Phonetics signData;
        public ushort compoundIndex; // Index to compoundData array

        public CompoundTable(in CompoundSign sign, int index) : this(sign.mappedChars, index) { }

        public CompoundTable(in ReadOnlySpan<int> mappedChars, int compoundIndex)
        {
            Debug.Assert(mappedChars.Length > 1);
            signData = new Phonetics(mappedChars);

            this.compoundIndex = (ushort) compoundIndex;
        }

        public readonly bool Equals(CompoundTable other) => compoundIndex == other.compoundIndex && signData.Equals(other.signData);

        [BurstDiscard]
        public override readonly string ToString()
        {
            string output = $"Compound Index: {compoundIndex}\n";
            foreach (char sign in signData.CompoundSpan)
            {
                output += $"{sign} ";
            }

            return output;
        }
    }

    [BurstCompile]
    public static class PhoneticsSortingMethods
    {
        /// <summary>
        /// Sorts based on length (highest to lowest order).
        /// </summary>
        /// <param name="span"></param>
        /// <param name="length"></param>
        public static void Sort(in Span<CompoundTable> span)
        {
            Span<CompoundTable> result = stackalloc CompoundTable[span.Length];

            int min = span[0].signData.Length, max = span[0].signData.Length;
            for (int i = 0; i < span.Length; i++)
            {
                min = math.min(min, span[i].signData.Length);
                max = math.max(max, span[i].signData.Length);
            }

            int extent = max - min;
            Span<int> histogram = stackalloc int[extent + 2];
            for (int i = 0; i < span.Length; i++)
            {
                histogram[(span[i].signData.Length - min) + 1]++;
            }

            // Exclusive Prefix Sum
            for (int i = 1; i < histogram.Length; i++)
            {
                histogram[i] += histogram[i - 1];
            }

            for (int i = 0; i < span.Length; i++)
            {
                int histPos   = span[i].signData.Length - min;
                int index     = histogram[histPos]++;
                result[index] = span[i];
            }

            // Copies Back
            result.Reverse();
            result.CopyTo(span);
        }
    }
}