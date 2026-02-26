using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;

using UnityEngine;

namespace Impl
{
    public struct SignData
    {
        public Range range;
        public char  unicodeChar;

        public SignData(in Range range, char unicodeChar)
        {
            this.range       = range;
            this.unicodeChar = unicodeChar;
        }
    }

    /// <summary>
    /// Made for comparing 2 character based standard characters (e.g. ng, sh).
    /// </summary>
    public struct PackedStandard : IEquatable<PackedStandard>, IComparable<PackedStandard>
    {
        private unsafe fixed char Chars[2];
        private int index;

        private unsafe readonly Span<char> AsSpanMut()
        {
            fixed (char* charPtr = Chars) return new Span<char>(charPtr, 2);
        }
        public unsafe readonly ReadOnlySpan<char> AsSpan() => AsSpanMut();

        public readonly int Index => index;

        public PackedStandard(char a1, char a2, int index)
        {
            Debug.Assert(index >= 0);
            this.index = index;

            var spanMut = AsSpanMut();
            spanMut[0]  = a1;
            spanMut[1]  = a2;
        }

        public readonly char this[int index]
        {
            get => AsSpan()[index];
        }

        public readonly bool Equals(PackedStandard rhs)
        {
            var lhsSpan = AsSpan();
            var rhsSpan = rhs.AsSpan();
            return lhsSpan[0] == rhsSpan[0] && lhsSpan[1] == rhsSpan[1];
        }
        
        public readonly int CompareTo(PackedStandard rhs)
        {
            var lhsSpan  = AsSpan();
            var rhsSpan  = rhs.AsSpan();
            int compare1 = lhsSpan[0].CompareTo(rhsSpan[0]);
            return compare1 == 0 ? lhsSpan[1].CompareTo(rhsSpan[1]) : compare1;
        }

        public readonly override string ToString()
        {
            var span = AsSpan();
            return $"{span[0]}{span[1]}";
        }
    }

    public struct CompoundTable : IEquatable<CompoundTable>
    {
        /// <summary>
        /// A packed structure for storing compound phonetics.
        /// </summary>
        public struct Phonetics : IEquatable<Phonetics>
        {
            private const int MaxCompoundSize = 4;
            private unsafe fixed char Data[MaxCompoundSize];
            private readonly ushort length;

            public unsafe Phonetics(in ReadOnlySpan<char> phonetics)
            {
                Debug.Assert(phonetics.Length > 0 && phonetics.Length <= MaxCompoundSize);
                length = (byte) phonetics.Length;

                for (int i = 0; i < length; i++)
                {
                    Data[i] = phonetics[i];
                }
            }

            public unsafe Phonetics(in ReadOnlySpan<int> phonetics)
            {
                Debug.Assert(phonetics.Length > 0);
                length = (byte) phonetics.Length;

                for (int i = 0; i < length; i++)
                {
                    Data[i] = (char) phonetics[i];
                }
            }

            public readonly int Length => length;

            public unsafe readonly ReadOnlySpan<char> CompoundSpan
            {
                get { fixed (char* ptr = Data) return new ReadOnlySpan<char>(ptr, length); }
            }

            public unsafe ref readonly char this[int index]
            {
                get => ref Data[index];
            }

            public readonly bool Equals(Phonetics other) => length == other.length && CompoundSpan.SequenceEqual(other.CompoundSpan);
        }

        public Phonetics signData;
        public ushort compoundIndex; // Index to compoundData array

        public CompoundTable(in CompoundSign sign, int index) : this(sign.mappedChars, index) { }

        public CompoundTable(int[] mappedChars, int compoundIndex)
        {
            Debug.Assert(mappedChars.Length > 1);
            signData = new Phonetics(mappedChars);

            this.compoundIndex = (ushort) compoundIndex;
        }

        public readonly bool Equals(CompoundTable other) => compoundIndex == other.compoundIndex && signData.Equals(other.signData);

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
                min = Math.Min(min, span[i].signData.Length);
                max = Math.Max(max, span[i].signData.Length);
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