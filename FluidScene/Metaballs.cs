using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FluidScene
{
    class Metaballs
    {
        Camera camera;
        Shader pass1, pass2, points;
        Matrix4 matModel;
        int vboPositionHandle, vboColorHandle;
        int vboVertexHandle, eboIndexHandle, vboTexcoordHandle, vaoHandle;
        int fboPass1, texPass1;
        float[] vertices, texcoords;
        int[] indices;

        enum Material { Spheres, Points, Metaballs };
        Material material = Material.Spheres;
        float scale;
        int fboDownScale = 1;
        
        public Metaballs(Camera camera)
        {
            this.camera = camera;
            pass1 = new Shader("MetaballPass1");
            pass2 = new Shader("MetaballPass2");
            points = new Shader("points");

            vertices = new float[]
            {
                -0.5f, -0.5f,  0.0f,
                 0.5f, -0.5f,  0.0f,
                 0.5f,  0.5f,  0.0f,
                -0.5f,  0.5f,  0.0f,
            };

            indices = new int[]
            {
                0, 1, 2, 2, 3, 0
            };

            texcoords = new float[]
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
                0.0f, 1.0f
            };

            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboVertexHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboVertexHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            eboIndexHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboIndexHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indices.Length * sizeof(uint)), indices, BufferUsageHint.StaticDraw);

            vboTexcoordHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboTexcoordHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(texcoords.Length * sizeof(float)), texcoords, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);

            vboPositionHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositionHandle);
            for (int i = 0; i < 4; i++)
            {
                GL.EnableVertexAttribArray(2 + i);
                GL.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, sizeof(float) * 16, sizeof(float) * i * 4);
                GL.VertexAttribDivisor(2 + i, 1);
            }

            vboColorHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorHandle);
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.VertexAttribDivisor(6, 1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            texPass1 = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texPass1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, camera.Resolution.X / fboDownScale, camera.Resolution.Y / fboDownScale,
                0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            fboPass1 = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboPass1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texPass1, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Unload()
        {
            pass1.Unload();
        }

        public void Update()
        {
            matModel = Matrix4.CreateScale(new Vector3(camera.Resolution.X, camera.Resolution.Y, 1.0f)) *
                Matrix4.CreateTranslation(new Vector3(camera.Position.X, camera.Position.Y, 0.0f));
        }

        public void Render(List<Vector2> listPositions, List<Color4> listColors)
        {
            if (material == Material.Spheres)
            {
                scale = 10.0f;

                GL.UseProgram(points.ProgramHandle);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                GL.UniformMatrix4(GL.GetUniformLocation(points.ProgramHandle, "matProjection"), false, ref camera.matProjection);
                GL.UniformMatrix4(GL.GetUniformLocation(points.ProgramHandle, "matView"), false, ref camera.matView);

                Matrix4[] matPositions = new Matrix4[listPositions.Count];
                for (int i = 0; i < listPositions.Count; i++)
                {
                    Matrix4 translation;
                    Matrix4.CreateScale(scale, scale, 1.0f, out matPositions[i]);
                    Matrix4.CreateTranslation(listPositions[i].X, listPositions[i].Y, 0.0f, out translation);
                    Matrix4.Mult(ref matPositions[i], ref translation, out matPositions[i]);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositionHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 16 * listPositions.Count), matPositions, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 4 * listColors.Count), listColors.ToArray(), BufferUsageHint.DynamicDraw);

                GL.BindVertexArray(vaoHandle);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, listPositions.Count);

                GL.BindVertexArray(0);
                GL.UseProgram(0);
            }
            else if (material == Material.Metaballs)
            {
                scale = 40.0f;

                // Pass 1: Render circles with alpha gradient
                GL.Viewport(0, 0, camera.Resolution.X / fboDownScale, camera.Resolution.Y / fboDownScale);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboPass1);
                GL.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.UseProgram(pass1.ProgramHandle);
                //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

                GL.UniformMatrix4(GL.GetUniformLocation(pass1.ProgramHandle, "matProjection"), false, ref camera.matProjection);
                GL.UniformMatrix4(GL.GetUniformLocation(pass1.ProgramHandle, "matView"), false, ref camera.matView);

                Matrix4[] matPositions = new Matrix4[listPositions.Count];
                for (int i = 0; i < listPositions.Count; i++)
                {
                    Matrix4 translation;
                    Matrix4.CreateScale(scale, scale, 1.0f, out matPositions[i]);
                    Matrix4.CreateTranslation(listPositions[i].X, listPositions[i].Y, 0.0f, out translation);
                    Matrix4.Mult(ref matPositions[i], ref translation, out matPositions[i]);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositionHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 16 * listPositions.Count), matPositions, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 4 * listColors.Count), listColors.ToArray(), BufferUsageHint.DynamicDraw);

                GL.BindVertexArray(vaoHandle);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, listPositions.Count);

                GL.BindVertexArray(0);
                GL.UseProgram(0);
                GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                //Pass 2: Discard pixels below gradient threshold
                GL.Viewport(0, 0, camera.Resolution.X, camera.Resolution.Y);
                GL.UseProgram(pass2.ProgramHandle);

                GL.UniformMatrix4(GL.GetUniformLocation(pass2.ProgramHandle, "matProjection"), false, ref camera.matProjectionNoZoom);
                GL.UniformMatrix4(GL.GetUniformLocation(pass2.ProgramHandle, "matView"), false, ref camera.matView);
                GL.UniformMatrix4(GL.GetUniformLocation(pass2.ProgramHandle, "matModel"), false, ref matModel);

                GL.Uniform1(GL.GetUniformLocation(pass2.ProgramHandle, "texture"), 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texPass1);

                GL.BindVertexArray(vaoHandle);
                GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
                GL.BindVertexArray(0);
                GL.UseProgram(0);
            }
        }

        public void KeyDownHandler(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F1) material = Material.Spheres;
            else if (e.Key == Key.F2) material = Material.Metaballs;
        }
    }
}
