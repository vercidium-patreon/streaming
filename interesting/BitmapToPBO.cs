using System.Threading.Tasks;

namespace streaming;

public unsafe class BitmapToPBO
{
    BitmapLoader loader;
    public PBO pbo;

    Task copyTask;
    public ReturnOnce JustCompleted;

    public BitmapToPBO(BitmapLoader loader)
    {
        this.loader = loader;

        pbo = PBOPool.GetOrCreatePBO(loader.width, loader.height);
        copyTask = Task.Run(t_Copy);

        JustCompleted = new(() =>
        {
            if (copyTask.IsCompleted)
            {
                pbo.FlushOrUnmap();

                if (pbo.FlushComplete)
                    return true;
            }

            return false;
        });
    }


    void t_Copy()
    {
        try
        {
            Helper.CopyMemory((IntPtr)pbo.mappedPtr, (IntPtr)loader.data, (uint)loader.bytes_data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to copy bitmap to PBO: {loader.name}\n{e}");
        }
    }

    public void Recycle()
    {
        // Skip if already disposed
        if (pbo == null)
            return;

        PBOPool.Store(ref pbo);
    }
}