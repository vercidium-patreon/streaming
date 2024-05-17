This repository is Part 3 of Vercidium's [Free Friday](https://www.patreon.com/posts/100857028) series.

---

This demo uses multiple threads to stream PNG files from the disk to textures on the graphics card. It covers many aspects of OpenGL:
- Multithreading
- Fences
- PBOs
- Textures
- Pools
- Shared contexts

This project uses [Silk.NET](https://github.com/dotnet/Silk.NET) so it *should* be cross-platform, but I've only tested it on Windows.

There are two folders:
- `boring` contains code that's common across each of these Free Friday posts
- `interesting` contains the code you're interested in

Key files:
- `Client.cs` creates the window and contains the render loop
- `AsyncTextureLoader.cs` orchestrates the texture streaming process from disk to GPU. It does this in 3 steps:
  - Step 1: `BitmapLoader.cs` loads PNGs from disk, decodes them using SkiaSharp, and stores the raw image data in an RGBA array
  - Step 2: `BitmapToPBO` copies the raw image data to a PBO and flushes it
  - Step 3:  Copies from the PBO to the texture on a shared background OpenGL context

TODO:
- Add bandwidth logs (MB/s from disk, MB/s from disk to PBO, etc)
- Add additional info here about how the background OpenGL context works

References:
- [songho's PBO article and demo](https://www.songho.ca/opengl/gl_pbo.html)