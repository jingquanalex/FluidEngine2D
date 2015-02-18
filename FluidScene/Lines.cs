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
    public class Line
    {
        public Vector2 PointStart { get; set; }
        public Vector2 PointEnd { get; set; }
        public Color4 ColorStart { get; set; }
        public Color4 ColorEnd { get; set; }
        public Vector2 Vector
        {
            get { return PointEnd - PointStart; }
        }

        public Line(Vector2 pointStart, Vector2 pointEnd)
        {
            PointStart = pointStart;
            PointEnd = pointEnd;
            ColorStart = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            ColorEnd = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        public Line(Vector2 pointStart, Vector2 pointEnd, Color4 color)
        {
            PointStart = pointStart;
            PointEnd = pointEnd;
            ColorStart = color;
            ColorEnd = color;
        }

        public bool IsDot()
        {
            return PointStart == PointEnd;
        }
    }

    class Lines
    {
        Camera camera;
        Shader shader;
        int vaoHandle, vboPositionHandle, vboColorHandle;

        public Lines(Camera camera)
        {
            this.camera = camera;
            shader = new Shader("lines");

            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            vboPositionHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositionHandle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);

            vboColorHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorHandle);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void Unload()
        {
            shader.Unload();
        }

        public void Render(List<Line> lines)
        {
            GL.UseProgram(shader.ProgramHandle);

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ProgramHandle, "matProjection"), false, ref camera.matProjection);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ProgramHandle, "matView"), false, ref camera.matView);

            Vector2[] positions = new Vector2[lines.Count * 2];
            Color4[] colors = new Color4[lines.Count * 2];
            for (int i = 0; i < lines.Count; i++)
            {
                positions[i * 2] = lines[i].PointStart;
                positions[i * 2 + 1] = lines[i].PointEnd;
                colors[i * 2] = lines[i].ColorStart;
                colors[i * 2 + 1] = lines[i].ColorEnd;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositionHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 2 * positions.Length), positions, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(sizeof(float) * 4 * colors.Length), colors, BufferUsageHint.DynamicDraw);

            GL.BindVertexArray(vaoHandle);
            GL.DrawArrays(PrimitiveType.Lines, 0, positions.Length);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }
    }
}
