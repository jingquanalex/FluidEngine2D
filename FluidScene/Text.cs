using System;
using System.IO;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FluidScene
{
    class Text
    {
        Camera camera;
        Shader shader;
        TextRenderer textRenderer;
        int vboVertexHandle, eboIndexHandle, vboTexcoordHandle, vaoHandle;
        float[] vertices, texcoords;
        int[] indices;
        Vector3 position, scale;
        float rotation;
        Matrix4 modelMatrix;

        bool isTextVisible = true;

        public Vector3 Position
        {
            get { return position; }
            set { position = value; UpdateTransformation(); }
        }

        public Vector3 Scale
        {
            get { return scale; }
            set { scale = value; UpdateTransformation(); }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; UpdateTransformation(); }
        }

        public Text(Camera camera)
        {
            this.camera = camera;
            textRenderer = new TextRenderer(camera.Resolution.X, camera.Resolution.Y);
            shader = new Shader("text");

            Position = Vector3.Zero;
            Scale = new Vector3(camera.Resolution.X, camera.Resolution.Y, 1.0f);

            vertices = new float[]
            {
                -0.5f, -0.5f,  0.0f,
                 0.5f, -0.5f,  0.0f,
                 0.5f,  0.5f,  0.0f,
                -0.5f,  0.5f,  0.0f
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
                0.0f, 1.0f,
            };

            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            GL.EnableVertexAttribArray(0);
            vboVertexHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboVertexHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, 0, 0);

            eboIndexHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboIndexHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indices.Length * sizeof(uint)), indices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(1);
            vboTexcoordHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboTexcoordHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(texcoords.Length * sizeof(float)), texcoords, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, true, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void Unload()
        {
            shader.Unload();
            textRenderer.Dispose();
        }

        public void Update(int mouseX, int mouseY, int particleCount)
        {
            if (isTextVisible == false) return;

            if (Timer.RunEvery(0.1f))
            {
                textRenderer.Clear();
                textRenderer.UpdateText("FPS: " + Timer.FPS.ToString("0") + Timer.AverageFPS.ToString(" (Avg 0)") +
                    Timer.DT.ToString(" (0.0000 ms)"), Color.White, new PointF(2f, 5f));

                textRenderer.UpdateText("Camera Position: " + camera.Position, Color.White, new PointF(2f, 25f));
                textRenderer.UpdateText("Mouse Local: " + Mouse.PositionLocal, Color.White, new PointF(2f, 45f));
                textRenderer.UpdateText("Mouse Global: " + Mouse.PositionGlobal, Color.White, new PointF(2f, 65f));
                textRenderer.UpdateText("Particles: " + particleCount, Color.White, new PointF(2f, 85f));
                textRenderer.UpdateText("RestDensity: " + SPHPBF.kRestDensity, Color.White, new PointF(2f, 105f));
                textRenderer.UpdateText("Relaxation: " + SPHPBF.kRelaxation, Color.White, new PointF(2f, 125f));
                textRenderer.UpdateText("loops: " + SPHPBF.loops, Color.White, new PointF(2f, 205f));
                textRenderer.UpdateText("test: " + SPHPBF.test, Color.White, new PointF(2f, 225f));
                textRenderer.UpdateText("testv: " + SPHPBF.testv, Color.White, new PointF(2f, 245f));
            }

            Position = new Vector3(camera.Position.X, camera.Position.Y, 0.0f);
        }

        public void Render()
        {
            if (isTextVisible == false) return;

            GL.UseProgram(shader.ProgramHandle);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ProgramHandle, "matProjection"), false, ref camera.matProjectionNoZoom);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ProgramHandle, "matView"), false, ref camera.matView);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ProgramHandle, "matModel"), false, ref modelMatrix);

            GL.Uniform1(GL.GetUniformLocation(shader.ProgramHandle, "texText"), 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textRenderer.Texture);

            GL.BindVertexArray(vaoHandle);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        public void KeyDownHandler(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Tilde)
            {
                isTextVisible = !isTextVisible;
            }
        }

        void UpdateTransformation()
        {
            modelMatrix = Matrix4.CreateScale(Scale) *
                Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation)) *
                Matrix4.CreateTranslation(Position);
        }
    }
}
