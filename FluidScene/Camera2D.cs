using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FluidScene
{
    class Camera2D : Camera
    {
        float zoom = 1.0f;

        /// <summary>
        /// Near clipping plane of the camera.
        /// </summary>
        public override float Near
        {
            get { return near; }
            set { near = value; UpdateProjectionMatrix(); }
        }

        /// <summary>
        /// Far clipping plane of the camera.
        /// </summary>
        public override float Far
        {
            get { return far; }
            set { far = value; UpdateProjectionMatrix(); }
        }

        /// <summary>
        /// Resolution of the viewport to build the aspect ratio.
        /// </summary>
        public override Point Resolution
        {
            get { return resolution; }
            set { resolution = value; UpdateProjectionMatrix(); }
        }

        /// <summary>
        /// Resolution multiplier to simulate zooming.
        /// </summary>
        public float Zoom
        {
            get { return zoom; }
            set { zoom = value; UpdateProjectionMatrix(); }
        }

        public Camera2D() : base()
        {
            UpdateProjectionMatrix();
            UpdateViewMatrix();
        }

        public void MoveUp()
        {
            position.Y += Speed * Zoom * dt;
        }

        public void MoveDown()
        {
            position.Y -= Speed * Zoom * dt;
        }

        public void MoveLeft()
        {
            position.X -= Speed * Zoom * dt;
        }

        public void MoveRight()
        {
            position.X += Speed * Zoom * dt;
        }

        public void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            Zoom += (float)-e.Delta / 50;
        }

        protected override void UpdateProjectionMatrix()
        {
            Matrix4.CreateOrthographic(resolution.X * zoom, resolution.Y * zoom, near, far, out matProjection);
            Matrix4.CreateOrthographic(resolution.X, resolution.Y, near, far, out matProjectionNoZoom);
        }
    }
}
