namespace streaming;

public unsafe class AsyncTextureLoader
{
    SharedOpenGLContext sharedContext;

    BitmapLoader loader;
    BitmapToPBO bitmapToPBO;
    Texture texture;

    FenceObject transferFence;
    FenceObject renderFence;

    public bool Ready => transferFence != null && transferFence.signaled;
    public bool Rendering => renderFence != null && !renderFence.signaled;

    public AsyncTextureLoader(SharedOpenGLContext sharedContext, string name)
    {
        this.sharedContext = sharedContext;

        // Step 1 - load from disk to bitmap
        loader = new BitmapLoader(name);
    }

    public void Update()
    {
        if (Ready)
            return;

        // Step 2 - copy from bitmap to PBO
        if (loader.JustCompleted.Value)
        {
            bitmapToPBO = new(loader);
            texture = TexturePool.GetOrCreateTexture(loader.width, loader.height);
        }

        // Step 3 - copy from PBO to texture
        if (bitmapToPBO != null && bitmapToPBO.JustCompleted.Value)
        {
            sharedContext.TransferActions.Add(CopyFromPBOToTexture);
        }
    }

    FenceObject CopyFromPBOToTexture(GL sharedGL)
    {
        // Copy data from pbo to tex
        texture.Bind(sharedGL);
        bitmapToPBO.pbo.Bind(sharedGL);
        sharedGL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)loader.width, (uint)loader.height, texture.pixelFormat, PixelType.UnsignedByte, null);
        bitmapToPBO.pbo.Unbind(sharedGL);

        // Generate mipmaps
        Gl.GenerateMipmap(GLEnum.Texture2D);
        texture.Unbind(sharedGL);

        // Keep track of when the transfer completes
        transferFence = new FenceObject();
        transferFence.OnFrameEnd(sharedGL);
        return transferFence;
    }

    public void Bind(int unit)
    {
        if (texture == null)
            return;

        if (transferFence == null)
            return;

        if (!transferFence.signaled)
            return;

        texture.Bind(unit);

        // This fence is used to check if this texture is still being rendered or not
        renderFence = FenceManager.GetCurrentFence();
    }

    public void Recycle()
    {
        // Verify we've completely loaded
        Assert(Ready);
        Assert(texture != null);
        Assert(bitmapToPBO.pbo.FlushComplete);

        // Recycle
        TexturePool.Store(texture);
        loader.Recycle();
        bitmapToPBO.Recycle();
    }
}