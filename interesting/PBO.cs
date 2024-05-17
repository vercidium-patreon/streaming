namespace streaming;

public unsafe class PBO
{
    uint handle;
    public uint byteCount;

    const int BYTES_PER_PIXEL = 4; // 4 bytes for RGBA8

    public byte* mappedPtr;
    public bool Mapped => mappedPtr != null;

    BufferTargetARB target = BufferTargetARB.PixelUnpackBuffer;

    // Always use persistent buffers if they're available
    public bool Persistent => GLFeatures.BufferStorageAvailable;
    MapBufferAccessMask MapFlags => Persistent ? (MapBufferAccessMask.WriteBit | MapBufferAccessMask.PersistentBit | MapBufferAccessMask.FlushExplicitBit) : MapBufferAccessMask.WriteBit;
    BufferStorageMask BufferFlags => Persistent ? (BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit) : BufferStorageMask.MapWriteBit;

    FenceObject flushFence;
    public bool FlushStarted => flushFence != null;
    public bool FlushComplete => flushFence != null && flushFence.signaled;

    public PBO(int width, int height)
    {
        handle = Gl.GenBuffer();
        Bind();


        // Allocate memory
        byteCount = (uint)(width * height * BYTES_PER_PIXEL);
        Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);


        // Persistent buffers use newer OpenGL features and are always mapped
        if (Persistent)
        {
            Gl.BufferStorage((BufferStorageTarget)target, byteCount, null, BufferFlags);
            Map();
        }
        else
            Gl.BufferData(target, byteCount, null, BufferUsageARB.StreamDraw);


        // Clean up
        Unbind();
    }

    public void Map()
    {
        if (Mapped)
        {
            // It's okay to re-map if it's persistent
            Assert(Persistent);
            return;
        }


        Bind();

        if (!Persistent)
            Gl.BufferData(target, byteCount, null, BufferUsageARB.StreamDraw); // Orphan

        mappedPtr = (byte*)Gl.MapBufferRange(target, IntPtr.Zero, byteCount, MapFlags);
        Unbind();


        // Validate
        Assert(mappedPtr != null);
    }

    public void FlushOrUnmap()
    {
        if (FlushStarted)
            return;

        if (Persistent)
            Flush();
        else
            Unmap();

        flushFence = FenceManager.GetCurrentFence();
    }

    void Flush()
    {
        // Flush if persistent
        Assert(Persistent);

        Bind();
        Gl.FlushMappedBufferRange(target, IntPtr.Zero, byteCount);
        Unbind();
    }

    public void Unmap(bool deleting = false)
    {
        // Should never unmap a persistent PBO, unless we're deleting it
        if (!deleting)
            Assert(!Persistent);

        Bind();
        var success = Gl.UnmapBuffer(target);
        Unbind();


        // Clean up + validate
        mappedPtr = null;
        Assert(success);
    }

    public void CopyData(IntPtr bitmapData)
    {
        Assert(Mapped);
        Helper.CopyMemory((IntPtr)mappedPtr, bitmapData, byteCount);
    }

    // We can bind this PBO from any OpenGL Context
    public void Bind(GL GLContext = null) => (GLContext ?? Gl).BindBuffer(target, handle);
    public void Unbind(GL GLContext = null) => (GLContext ?? Gl).BindBuffer(target, 0);

    public void Reset()
    {
        flushFence = null;
    }

    public void Dispose()
    {
        if (Mapped)
            Unmap(true);

        Gl.DeleteBuffer(handle);
        handle = 0;
    }
}
