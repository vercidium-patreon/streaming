using Silk.NET.Windowing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace streaming;

public class SharedOpenGLContext
{
    IWindow loadingWindow;
    GL sharedGL;

    public BlockingCollection<Func<GL, FenceObject>> TransferActions = new();

    // Create a hidden window that shares the main window's OpenGL context.
    // This window is then used for transferring data from PBO to textures 
    public SharedOpenGLContext(IWindow mainWindow)
    {
        // Create an invisible window with a shared context
        loadingWindow = Window.Create(WindowOptions.Default with
        {
            SharedContext = mainWindow.GLContext,
            IsVisible = false,
            API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3)),
        });


        // Initialise the shared OpenGL context for the invisible window
        loadingWindow.Load += () =>
        {
            sharedGL = loadingWindow.CreateOpenGL();
        };
        loadingWindow.Initialize();


        // Make the main window's OpenGL context current again on this thread
        mainWindow.MakeCurrent();


        // Update the other window on another thread
        var thread = new Thread(new ThreadStart(Update));
        thread.Start();
    }

    List<FenceObject> fences = new();

    void Update()
    {
        loadingWindow.MakeCurrent();

        while (true)
        {
            // Since this context creates its own fences, this context needs to check if they're signaled, not the main context
            for (int i = 0; i < fences.Count; i++)
            {
                var f = fences[i];

                f.CheckSignal(sharedGL);

                if (f.signaled)
                {
                    fences.RemoveAt(i);
                    i--;
                }
            }

            // Copy from PBO -> Texture on this thread at 60 FPS (16ms timeout)
            if (TransferActions.TryTake(out var action, 16))
                fences.Add(action(sharedGL));
        }
    }
}