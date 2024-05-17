using System.Collections.Generic;

namespace streaming;

public static class PBOPool
{
    static List<PBO> pbos = new();
    public const int BYTES_PER_PIXEL = 4; // For this demo, all textures are RGBA8

    public static PBO GetOrCreatePBO(int width, int height)
    {
        var byteCount = width * height * BYTES_PER_PIXEL;

        // Recycle an old PBO if possible
        for (int i = 0; i < pbos.Count; i++)
        {
            var pbo = pbos[i];

            if (pbo.byteCount == byteCount)
            {
                pbos.RemoveAt(i);
                return pbo;
            }
        }

        // Allocate a new PBO
        return new PBO(width, height);
    }

    public static void Store(ref PBO pbo)
    {
        pbo.Reset();
        pbos.Add(pbo);

        pbo = null;
    }
}