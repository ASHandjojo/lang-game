using System;

using Unity.Collections;

using UnityEngine;

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
        /// <summary>
        /// A packed structure for storing compound phonetics.
        /// </summary>
        public struct Phonetics
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
}