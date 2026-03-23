using System;

using Unity.Burst;
using Unity.Collections;

using UnityEngine;

using Impl;

[BurstCompile]
internal struct StringPool : IDisposable
{
    private NativeArray<ushort> pool;
    private NativeArray<int>    offsets;

    public static StringPool Create<T>(in ReadOnlySpan<DictEntryUnmanaged> entries, Allocator allocator) where T : unmanaged, ISelector<DictEntryUnmanaged>
    {
        Debug.Assert(entries.Length > 0);
        T selectFunc = new();

        int totalLength = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            totalLength += selectFunc.Select(entries[i]).Length;
        }

        StringPool output = new()
        {
            pool    = new NativeArray<ushort>(totalLength,     allocator),
            offsets = new NativeArray<int>(entries.Length + 1, allocator)
        };

        var poolSpan = output.pool.AsSpan();
        for (int i = 0, value = 0; i < entries.Length; i++)
        {
            var entryStrSpan  = selectFunc.Select(entries[i]);
            output.offsets[i] = value;

            int strLen = entryStrSpan.Length;
            entryStrSpan.CopyTo(poolSpan[value..(value + strLen)]);
            value += strLen;
        }
        output.offsets[^1] = totalLength;
        return output;
    }

    public unsafe readonly ReadOnlySpan<ushort> this[int index] => pool.AsReadOnlySpan()[offsets[index]..offsets[index + 1]];

    public void Dispose()
    {
        pool.Dispose();
        pool = default;

        offsets.Dispose();
        offsets = default;
    }
}