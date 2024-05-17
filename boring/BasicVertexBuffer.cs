namespace streaming;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BasicVertex
{
    public void Reset(Vector3 position, Vector2 uv)
    {
        positionX = position.X;
        positionY = position.Y;
        positionZ = position.Z;

        uvX = uv.X;
        uvY = uv.Y;
    }

    public float positionX;
    public float positionY;
    public float positionZ;
    public float uvX;
    public float uvY;
}

public unsafe class BasicVertexBuffer : VertexBuffer<BasicVertex>
{
    public BasicVertexBuffer() : base(sizeof(float) * 5) { }

    protected override void SetupVAO()
    {
        Gl.EnableVertexAttribArray(0);
        Gl.EnableVertexAttribArray(1);

        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, null);
        Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)12);
    }
}