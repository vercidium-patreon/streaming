namespace streaming;

public unsafe class Texture : IDisposable
{
    public int width;
    public int height;
    public uint Handle;
    public TextureTarget target = TextureTarget.Texture2D;

    public PixelFormat pixelFormat = PixelFormat.Rgba;
    public InternalFormat internalFormat = InternalFormat.Rgba8;


    public Texture(int width, int height)
    {
        this.width = width;
        this.height = height;

        Handle = Gl.GenTexture();

        Gl.BindTexture(TextureTarget.Texture2D, Handle);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height, 0, pixelFormat, PixelType.UnsignedByte, null);

        // Set filterint and wrap parameters
        var minFilter = GLEnum.LinearMipmapLinear;
        var magFilter = GLEnum.Linear;
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

        var wrap = GLEnum.Repeat;
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
    }

    public void Dispose()
    {
        Assert(Handle > 0);

        Gl.DeleteTexture(Handle);
        Handle = 0;
    }

    public void Bind(int textureUnit)
    {
        Gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        Gl.BindTexture(target, Handle);
    }

    public void Bind(GL GLContext = null)
    {
        (GLContext ?? Gl).BindTexture(target, Handle);
    }

    public void Unbind(GL GLContext = null)
    {
        (GLContext ?? Gl).BindTexture(target, 0);
    }
}
