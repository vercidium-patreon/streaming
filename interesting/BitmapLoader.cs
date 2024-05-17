using SkiaSharp;
using System.IO;
using System.Threading.Tasks;

namespace streaming;

public unsafe class BitmapLoader
{
    static int elapsed;

    public int width;
    public int height;
    public string name;

    public int bytes_data;
    public byte* data;

    Task loadingTask;
    public ReturnOnce JustCompleted;

    public BitmapLoader(string name)
    {
        this.name = name;
        loadingTask = Task.Run(t_Load);

        JustCompleted = new(() => loadingTask.IsCompleted);
    }

    // The t_ prefix means this code runs on another thread
    void t_Load()
    {
        try
        {
            using var stm = File.OpenRead($"{name}.png");
            using var bitmap = SKBitmap.Decode(stm);

            // Determine pixel count
            width = bitmap.Width;
            height = bitmap.Height;

            var pixelCount = width * height;

            bytes_data = pixelCount * sizeof(uint);
            data = UnmanagedPool.GetOrCreateUnmanagedMemory(bytes_data);


            // Determine read and write bounds
            var skData = (byte*)bitmap.GetPixels();
            var read = (uint*)skData;
            var end = read + pixelCount;

            var write = (uint*)data;


            float brightness = (MathF.Sin(elapsed / 200.0f) + 1) / 2;
            elapsed++;

            // Swap RGBA to BGRA
            if (bitmap.ColorType == SKColorType.Bgra8888)
            {
                while (read < end)
                {
                    var value = *read++;

                    var r = value & 255;
                    var g = value >> 8 & 255;
                    var b = value >> 16 & 255;
                    var a = value >> 24;

                    r = (byte)(r * brightness);
                    g = (byte)(g * brightness);
                    b = (byte)(b * brightness);
                    a = (byte)(a * brightness);

                    *write++ = b | (g << 8) | (r << 16) | (a << 24);
                }
            }
            else if (bitmap.ColorType == SKColorType.Gray8)
            {
                while (read < end)
                {
                    var value = *read++;
                    value = (byte)(value * brightness);

                    *write++ = value | (value << 8) | (value << 16) | (value << 24);
                }
            }
            else
                AssertFalse();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load bitmap: {name}\n{e}");
        }
}

    public void Recycle()
    {
        // Skip if already disposed
        if (data == null)
        {
            AssertFalse();
            return;
        }

        UnmanagedPool.Store(ref data, ref bytes_data);
    }
}