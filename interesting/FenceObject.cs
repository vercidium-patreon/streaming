namespace streaming;

public class FenceObject
{
    nint fence;
    public bool signaled;

    public void OnFrameEnd(GL GLContext = null)
    {
        fence = (GLContext ?? Gl).FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public bool CheckSignal(GL GLContext = null)
    {
        GLContext ??= Gl;

        var result = GLContext.ClientWaitSync(fence, SyncObjectMask.Bit, 1);

        if (result == GLEnum.TimeoutExpired || result == GLEnum.WaitFailed)
            return signaled = false;

        GLContext.DeleteSync(fence);
        return signaled = true;
    }
}