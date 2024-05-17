using System.Collections.Generic;

namespace streaming;

public static unsafe class UnmanagedPool
{
    static List<(IntPtr ptr, int byteCount)> storage = new();

    public static byte* GetOrCreateUnmanagedMemory(int byteCount)
    {
        for (int i = 0; i < storage.Count; i++)
        {
            var d = storage[i];

            if (d.byteCount == byteCount)
            {
                storage.RemoveAt(i);
                return (byte*)d.ptr;
            }
        }

        return (byte*)Allocator.Alloc(byteCount);
    }

    public static void Store(ref byte* data, ref int byteCount)
    {
        storage.Add(((IntPtr)data, byteCount));

        data = null;
        byteCount = 0;
    }
}