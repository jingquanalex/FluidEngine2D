using System;
using System.IO;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FluidScene
{
    class Shader
    {
        protected int vsHandle, fsHandle, progHandle;

        public int ProgramHandle
        {
            get { return progHandle; }
        }

        public Shader(string ShaderName)
        {
            vsHandle = GL.CreateShader(ShaderType.VertexShader);
            fsHandle = GL.CreateShader(ShaderType.FragmentShader);

            string vspath = ParseShaderFile(ShaderName + ".vert");
            string fspath = ParseShaderFile(ShaderName + ".frag");

            if (vspath == null | fspath == null) return;

            GL.ShaderSource(vsHandle, vspath);
            GL.ShaderSource(fsHandle, fspath);

            Console.Write("Loading shader: {0}", ShaderName);

            GL.CompileShader(vsHandle);
            GL.CompileShader(fsHandle);

            string vslog = GL.GetShaderInfoLog(vsHandle);
            string fslog = GL.GetShaderInfoLog(fsHandle);

            if(vslog != "")
            {
                Console.WriteLine("\nVertex Shader: " + vslog);
            }

            if (fslog != "")
            {
                Console.WriteLine("\nFragment Shader: " + fslog);
            }

            progHandle = GL.CreateProgram();

            GL.AttachShader(progHandle, vsHandle);
            GL.AttachShader(progHandle, fsHandle);
            
            GL.LinkProgram(progHandle);

            string linklog = GL.GetProgramInfoLog(progHandle);

            if(linklog != "")
            {
                Console.WriteLine("\nLinking Failed: " + fslog);
            }
            else
            {
                Console.Write(" OK\n");
            }

            GL.DeleteShader(vsHandle);
            GL.DeleteShader(fsHandle);
        }

        public virtual void Unload()
        {
            GL.DeleteProgram(progHandle);
        }

        string ParseShaderFile(string shadername)
        {
            try
            {
                using (StreamReader sr = new StreamReader(FluidSceneMain.ExeDirectory + "/Shader/" + shadername))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Shader file not found: {0}", e.Message);
                return null;
            }
        }
    }
}
