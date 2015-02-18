using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FluidScene
{
    class Program
    {
        static void Main(string[] args)
        {
            using(var scene = new FluidSceneMain())
            {
                scene.Run();
            }
        }
    }
}
