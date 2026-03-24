using System;

using Unity.Burst;
using Unity.Collections;

using UnityEngine;

[BurstCompile]
public static class SpanExts
{
    public static unsafe int IndexOf<T, U>(in this ReadOnlySpan<T> span, in U value)
        where T : unmanaged, IEquatable<U>
        where U : unmanaged
    {
        fixed (T* ptr = span)
        {
            return NativeArrayExtensions.IndexOf<T, U>(ptr, span.Length, value);
        }
    }
}
