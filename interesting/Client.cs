using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Collections.Generic;

namespace streaming;

public unsafe partial class Client
{
    public Client()
    {
        // Create a Silk.NET window
        var options = WindowOptions.Default;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3));
        options.Position = new(200, 200);
        options.PreferredDepthBufferBits = 32;
        options.Title = "gl_VertexID";

        window = Window.Create(options);

        // Callback when the window is created
        window.Load += () =>
        {
            // Create an OpenGL Context
            Gl = window.CreateOpenGL();
            SilkOnDidCreateOpenGLContext();


            // Precalculate input stuff
            inputContext = window.CreateInput();
            keyboard = inputContext.Keyboards[0];
            mouse = inputContext.Mice[0];
            mouse.DoubleClickTime = 1;

            keyboard.KeyDown += SilkOnKeyDown;
        };

        window.Render += (_) => Render();

        window.Size = new(1920, 1080);
        window.FramesPerSecond = 2000;
        window.UpdatesPerSecond = 2000;
        window.VSync = false;
        window.FocusChanged += SilkOnFocusChanged;

        // Initialise OpenGL and input context
        window.Initialize();

        sharedContext = new(window);

        Initialise3D();
    }

    SharedOpenGLContext sharedContext;

    public void Run()
    {
        // Run forever
        window.Run();
    }

    List<AsyncTextureLoader> textures = new();

    void SilkOnDidCreateOpenGLContext()
    {
        var major = Gl.GetInteger(GetPName.MajorVersion);
        var minor = Gl.GetInteger(GetPName.MinorVersion);

        var version = major * 10 + minor;
        Console.WriteLine($"OpenGL Version: {version}");


        // Get all extensions
        var extensionCount = Gl.GetInteger(GetPName.NumExtensions);
        var  extensions = new List<string>();
        for (int i = 0; i < extensionCount; i++)
            extensions.Add(Gl.GetStringS(StringName.Extensions, (uint)i));


        GLFeatures.TexStorage2DAvailable = extensions.Contains("GL_ARB_texture_storage");
        GLFeatures.BufferStorageAvailable = extensions.Contains("GL_ARB_buffer_storage");

#if DEBUG
        // Set up the OpenGL debug message callback (NVIDIA only)
        debugDelegate = DebugCallback;

        Gl.Enable(EnableCap.DebugOutput);
        Gl.Enable(EnableCap.DebugOutputSynchronous);
        Gl.DebugMessageCallback(debugDelegate, null);
#endif
    }

    void SilkOnFocusChanged(bool focused)
    {
        if (!focused)
            captureMouse = false;
        else
            captureMouse = true;

        lastMouse = mouse.Position;
    }

    void Initialise3D()
    {
        buffer = new();
        var bytes_vertexData = Marshal.SizeOf<BasicVertex>() * 4;
        var vertexData = (BasicVertex*)Allocator.Alloc(bytes_vertexData);

        var scale = 10;

        var write = vertexData;
        write++->Reset(new Vector3(-scale, 0, -scale), new Vector2(0, 0));
        write++->Reset(new Vector3(scale, 0, -scale), new Vector2(1, 0));
        write++->Reset(new Vector3(-scale, 0, scale), new Vector2(0, 1));
        write++->Reset(new Vector3(scale, 0, scale), new Vector2(1, 1));

        buffer.BufferData(4, vertexData);
        Allocator.Free(ref vertexData, ref bytes_vertexData);
    }



    // Rendering
    void Render()
    {
        FenceManager.OnFrameStart();

        UpdateCamera();


        // Prepare OpenGL
        PreRenderSetup();

        
        // Load a texture from disk every frame. This will re-use recycled PBOs, textures and unmanaged memory
        textures.Add(new(sharedContext, "ground"));


        // Update all textures
        foreach (var t in textures)
            t.Update();


        // Render the newest texture
        bool anyRendered = false;

        for (int i = textures.Count - 1; i >= 0; i--)
        {
            var tex = textures[i];

            if (tex.Ready)
            {
                tex.Bind(1);
                anyRendered = true;
                break;
            }
        }


        // Dispose textures that aren't being used for anything
        if (anyRendered)
        {
            for (int i = textures.Count - 1; i >= 0; i--)
            {
                var tex = textures[i];

                if (tex.Ready && !tex.Rendering)
                {
                    tex.Recycle();
                    textures.RemoveAt(i);
                }
            }
        }

        // Prepare the shader
        BasicShader.UseProgram();
        BasicShader.mvp.Set(GetViewProjection());
        BasicShader.groundTexture.Set(1);

        Gl.FrontFace(FrontFaceDirection.Ccw);
        Gl.Disable(EnableCap.CullFace);

        // Render the texture
        buffer.BindAndDraw();


        FenceManager.OnFrameEnd();
    }

    void PreRenderSetup()
    {
        // Prepare rendering
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Gl.Enable(EnableCap.DepthTest);
        Gl.Disable(EnableCap.Blend);
        Gl.Disable(EnableCap.StencilTest);
        Gl.Enable(EnableCap.CullFace);
        Gl.FrontFace(FrontFaceDirection.CW);


        // Clear everything
        Gl.ClearDepth(1.0f);
        Gl.DepthFunc(DepthFunction.Less);

        Gl.ColorMask(true, true, true, true);
        Gl.DepthMask(true);

        Gl.ClearColor(25 / 255.0f, 33 / 255.0f, 44 / 255.0f, 0);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);


        // Set the viewport to the window size
        Gl.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);
    }

    Matrix4x4 GetViewProjection()
    {
        var view = Helper.CreateFPSView(cameraPos, cameraPitch, cameraYaw);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, Aspect, NearPlane, FarPlane);

        return view * proj;
    }


    // Input
    void SilkOnKeyDown(IKeyboard keyboard, Key key, int something)
    {
        if (key == Key.Escape)
        {
            captureMouse = !captureMouse;

            // Don't snip the camera when capturing the mouse
            lastMouse = mouse.Position;
        }
    }

    void UpdateCamera()
    {
        if (firstRender)
        {
            lastMouse = mouse.Position;
            firstRender = false;
        }


        // Mouse movement
        if (captureMouse)
        {
            var diff = lastMouse - mouse.Position;

            cameraYaw -= diff.X * 0.003f;
            cameraPitch += diff.Y * 0.003f;

            mouse.Position = new Vector2(window.Size.X / 2, window.Size.Y / 2);
            lastMouse = mouse.Position;
            mouse.Cursor.CursorMode = CursorMode.Hidden;
        }
        else
            mouse.Cursor.CursorMode = CursorMode.Normal;


        // Fly camera movement
        float movementSpeed = 0.15f;

        if (keyboard.IsKeyPressed(Key.W))
            cameraPos += Helper.FromPitchYaw(cameraPitch, cameraYaw) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.S))
            cameraPos -= Helper.FromPitchYaw(cameraPitch, cameraYaw) * movementSpeed;

        if (keyboard.IsKeyPressed(Key.A))
            cameraPos += Helper.FromPitchYaw(0, cameraYaw - MathF.PI / 2) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.D))
            cameraPos += Helper.FromPitchYaw(0, cameraYaw + MathF.PI / 2) * movementSpeed;

        if (keyboard.IsKeyPressed(Key.E))
            cameraPos += Helper.FromPitchYaw(cameraPitch + MathF.PI / 2, cameraYaw) * movementSpeed;
        else if (keyboard.IsKeyPressed(Key.Q))
            cameraPos += Helper.FromPitchYaw(cameraPitch - MathF.PI / 2, cameraYaw) * movementSpeed;
    }


#if DEBUG
    // Debug OpenGL callbacks (Works on NVIDIA, not sure about AMD/Intel)
    DebugProc debugDelegate;

    unsafe void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint messageInt, nint userParam)
    {
        // TODO
        var message = Marshal.PtrToStringAnsi(messageInt);

        // Skip our own notifications
        if (severity == GLEnum.DebugSeverityNotification)
            return;

        // Pixel-path performance warning: Pixel transfer is synchronized with 3D rendering
        if (id == 131154)
        {
            AssertFalse();
            return;
        }

        // Buffer detailed info
        if (id == 131185)
            return;

        // "Program/shader state performance warning: Vertex shader in program 69 is being recompiled based on GL state."
        if (id == 131218)
            return;

        // "Buffer performance warning: Buffer object 15 (bound to NONE, usage hint is GL_DYNAMIC_DRAW) is being copied/moved from VIDEO memory to HOST memory."
        if (id == 131186)
            return;

        AssertFalse();
        Console.WriteLine(message);
    }
#endif



    // Silk
    IWindow window;
    IMouse mouse;
    IKeyboard keyboard;
    IInputContext inputContext;


    // Camera
    Vector2 lastMouse;
    Vector3 cameraPos = new(-20.905869f, 16.690794f, -11.226549f);
    float cameraPitch = -0.7133f;
    float cameraYaw = 1.055f;

    bool captureMouse = true;


    // Rendering
    bool firstRender = true;

    float FieldOfView = 50.0f / 180.0f * MathF.PI;
    float Aspect => window.Size.X / (float)window.Size.Y;
    float NearPlane = 1.0f;
    float FarPlane = 256.0f;

    BasicVertexBuffer buffer;
}