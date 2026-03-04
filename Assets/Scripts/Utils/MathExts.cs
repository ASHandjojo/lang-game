using System;
using System.Runtime.CompilerServices;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;

[BurstCompile]
public static class MathExts
{
    /// <summary>
    /// Wraps number between a range of 0 (inclusive) and length (exclusive).
    /// </summary>
    /// <param name="value"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WrapRange(this int value, int length)
    {
        if (value > 0 && value < length)
        {
            return value;
        }
        else if (value > 0) // Positive
        {
            int wrapCount = (value / length) * length;
            return value - wrapCount;
        }
        else if (value < 0) // Negative
        {
            int wrapCount = (value / length) * length;
            int wrapDiff  = value - wrapCount;
            return wrapDiff == 0 ? wrapDiff : length + wrapDiff;
        }
        return value; // Fallback case
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WrapRange(this int value, int min, int max)
    {
        int extent = max - min;
        if (value > min && value < max)
        {
            return value;
        }
        else if (value > min)
        {
            int wrapCount = math.abs((value - min) / extent) * extent;
            int wrapDiff  = value - wrapCount;
            return wrapDiff;
        }
        else
        {
            int wrapCount = ((value - min) / extent) * extent;
            int wrapDiff  = value - wrapCount;
            return wrapDiff == max ? wrapDiff : extent + wrapDiff;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe sbyte CastAsInt8(this bool value)    => *(sbyte*) UnsafeUtility.AddressOf(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe short CastAsInt16(this bool value)   => *(byte*) UnsafeUtility.AddressOf(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int CastAsInt32(this bool value)     => *(byte*) UnsafeUtility.AddressOf(ref value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte CastAsUInt8(this bool value)    => *(byte*) UnsafeUtility.AddressOf(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ushort CastAsUInt16(this bool value) => *(byte*) UnsafeUtility.AddressOf(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint CastAsUInt32(this bool value)   => *(byte*) UnsafeUtility.AddressOf(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(this uint value)
    {
        if (X86.Popcnt.IsPopcntSupported) // Compile-time
        {
            return X86.Popcnt.popcnt_u32(value);
        }
        else if (Arm.Neon.IsNeonSupported)
        {
            return unchecked((int) Arm.Neon.vcnt_u8(new v64(value)).ULong0);
        }
        else // Fallback
        {
            uint count;
            count = value - ((value >> 1) & 0x55555555);
            count = ((count >> 2) & 0x33333333) + (count & 0x33333333);
            count = ((count >> 4)  + count) & 0x0F0F0F0F;
            count = ((count >> 8)  + count) & 0x00FF00FF;
            count = ((count >> 16) + count) & 0x0000FFFF;
            return unchecked((int) count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int PopCount(this int value) => PopCount(*(uint*) &value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte FindFirstSet(this uint value)
    {
        sbyte count = unchecked((sbyte) math.tzcnt(value));
        return count == 32 ? (sbyte) -1 : count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte FindNextSet(this uint value, int bitOffset)
    {
        if (Hint.Unlikely(bitOffset >= 32))
        {
            return -1;
        }
        value      &= ~0U << bitOffset;
        sbyte count = unchecked((sbyte) math.tzcnt(value));
        return count == 32 ? (sbyte) -1 : count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Log2(this int x) => 31 ^ math.lzcnt(x | 1);
}