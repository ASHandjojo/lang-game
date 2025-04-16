using System.Linq;
using System;
using UnityEngine;

namespace Impl
{
    public static class ProcessorExtMethods
    {
        // fuck it, counting sort time :)
        public static StandardSign[] Sort(in ReadOnlySpan<StandardSign> standardSigns)
        {
            StandardSign[] output = new StandardSign[standardSigns.Length];

            int[] unicodeChars = new int[standardSigns.Length];
            for (int i = 0; i < standardSigns.Length; i++)
            {
                unicodeChars[i] = standardSigns[i].mappedChar;
            }

            int min = unicodeChars.Min();
            int max = unicodeChars.Max();

            int extent = max - min;
            int[] histogram = new int[extent + 2];
            foreach (int unicodeChar in unicodeChars)
            {
                histogram[(unicodeChar - min) + 1]++;
            }

            // Exclusive Prefix Sum
            for (int i = 1; i < histogram.Length; i++)
            {
                histogram[i] += histogram[i - 1];
            }

            for (int i = 0; i < standardSigns.Length; i++)
            {
                int histPos   = unicodeChars[i] - min;
                int index     = histogram[histPos]++;
                output[index] = standardSigns[i];
            }

            return output;
        }

        public static CompoundSign[] Sort(in ReadOnlySpan<CompoundSign> compoundSigns)
        {
            CompoundSign[] output = new CompoundSign[compoundSigns.Length];

            int[] unicodeChars = new int[compoundSigns.Length];
            for (int i = 0; i < compoundSigns.Length; i++)
            {
                unicodeChars[i] = compoundSigns[i].mappedChar;
            }

            int min = unicodeChars.Min();
            int max = unicodeChars.Max();

            int extent = max - min;
            int[] histogram = new int[extent + 2];
            foreach (int unicodeChar in unicodeChars)
            {
                histogram[(unicodeChar - min) + 1]++;
            }

            // Exclusive Prefix Sum
            for (int i = 1; i < histogram.Length; i++)
            {
                histogram[i] += histogram[i - 1];
            }

            for (int i = 0; i < compoundSigns.Length; i++)
            {
                int histPos   = unicodeChars[i] - min;
                int index     = histogram[histPos]++;
                output[index] = compoundSigns[i];
            }

            return output;
        }
    }
}