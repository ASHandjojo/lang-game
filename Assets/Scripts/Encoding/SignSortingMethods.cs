using System;
using System.Linq;

using Unity.Collections;

using UnityEngine;

namespace Impl
{
    public static class ProcessorExtMethods
    {
        // fuck it, counting sort time ;)
        /// <summary>
        /// Does a copied (immutable) counting sort over all signs.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="standardSigns"></param>
        /// <returns></returns>
        public static T[] Sort<T>(in ReadOnlySpan<T> standardSigns) where T : struct, ISign
        {
            T[] output = new T[standardSigns.Length];

            int[] unicodeChars = new int[standardSigns.Length];
            for (int i = 0; i < standardSigns.Length; i++)
            {
                unicodeChars[i] = standardSigns[i].MappedChar;
            }

            int min = unicodeChars.Min();
            int max = unicodeChars.Max();

            int extent      = max - min;
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
    }
}