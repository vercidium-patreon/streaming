using System.Collections.Generic;

namespace streaming;

public static class FenceManager
{
    static List<FenceObject> fences = new();
    static FenceObject currentFence = new();

    public static FenceObject GetCurrentFence()
    {
        Assert(!currentFence.signaled);
        return currentFence;
    }

    public static void OnFrameStart()
    {
        // Check if previous fences have signaled
        for (int i = 0; i < fences.Count; i++)
        {
            var f = fences[i];

            // If an older fence hasn't signaled then we can break, as there's no point checking the others
            if (!f.CheckSignal())
                break;

            fences.RemoveAt(i);
            i--;
        }

        Assert(!currentFence.signaled);
    }

    public static void OnFrameEnd()
    {
        currentFence.OnFrameEnd();
        fences.Add(currentFence);

        // Create a new fence for the next frame
        currentFence = new FenceObject();
    }
}