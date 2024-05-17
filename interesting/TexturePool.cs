using System.Collections.Generic;

namespace streaming;

public static class TexturePool
{
    static List<Texture> storage = new();

    public static Texture GetOrCreateTexture(int width, int height)
    {
        // Re-use a texture with the same size
        for (int i = 0; i < storage.Count; i++)
        {
            var tex = storage[i];

            if (tex.width == width && tex.height == height)
            {
                storage.RemoveAt(i);
                return tex;
            }
        }

        // Allocate a new texture
        return new Texture(width, height);
    }

    public static void Store(Texture tex)
    {
        storage.Add(tex);
    }
}