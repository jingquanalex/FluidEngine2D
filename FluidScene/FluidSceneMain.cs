using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Reflection;

namespace FluidScene
{
    class FluidSceneMain : GameWindow
    {
        public static string ExeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string ExeName = AppDomain.CurrentDomain.FriendlyName;

        Camera2D camera;
        Text text;
        Metaballs metaballs;
        Lines lines;
        SPHClavet sph;

        public FluidSceneMain() : base(1280, 720, new GraphicsMode(32, 24, 8, 4), "SceneTest v0.2")
        {
            // Setup Nvidia Optimus Profile
            //SOP.SOP_SetProfile("SceneTest", ExeName);
            //SOP.SOP_RemoveProfile("SceneTest");
            Console.WriteLine("Working directory: " + ExeDirectory);
            //WindowBorder = WindowBorder.Fixed;

            Keyboard.KeyUp += KeyUpHandler;
        }

        protected override void OnLoad(EventArgs e)
        {
            VSync = VSyncMode.Off;

            GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.Blend);

            camera = new Camera2D()
            {
                Resolution = new Point(Width, Height),
                Speed = 1000.0f
            };
            Mouse.WheelChanged += camera.MouseWheelHandler;

            sph = new SPHClavet();
            Mouse.ButtonDown += sph.MouseDownHandler;
            Keyboard.KeyDown += sph.KeyDownHandler;

            text = new Text(camera);
            Keyboard.KeyDown += text.KeyDownHandler;

            metaballs = new Metaballs(camera);
            Keyboard.KeyDown += metaballs.KeyDownHandler;

            lines = new Lines(camera);
        }

        protected override void OnUnload(EventArgs e)
        {
            text.Unload();
            metaballs.Unload();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            camera.Resolution = new Point(Width, Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Timer.Update(e.Time);
            FluidScene.Mouse.Update(camera, Mouse.X, Mouse.Y);
            sph.Update();
            metaballs.Update();
            text.Update(Mouse.X, Mouse.Y, sph.ParticleCount);
            camera.Update((float)e.Time);

            if (Keyboard[Key.W]) camera.MoveUp();
            if (Keyboard[Key.S]) camera.MoveDown();
            if (Keyboard[Key.A]) camera.MoveLeft();
            if (Keyboard[Key.D]) camera.MoveRight();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            metaballs.Render(sph.ParticlePositions, sph.ParticleColors);
            lines.Render(sph.Lines);
            text.Render();
            
            SwapBuffers();
        }

        int i = new Random().Next(10000, 99999);
        void KeyUpHandler(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Exit();
            }

            if (e.Key == Key.F11)
            {
                if (WindowState == WindowState.Fullscreen)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Fullscreen;
            }

            if (e.Key == Key.F12)
            {
                Bitmap bmp = new Bitmap(Width, Height);
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, this.Width, this.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                GL.ReadPixels(0, 0, Width, Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr,
                              OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                string filepath = "Screenshots/FluidScene_" + i++.ToString() + ".png";
                Directory.CreateDirectory(ExeDirectory + "/Screenshots");
                bmp.Save(filepath, ImageFormat.Png);
                Console.WriteLine("Screenshot saved: {0}", filepath);
            }
        }
    }
}
