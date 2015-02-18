using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FluidScene
{
    public abstract class Camera
    {
        public Matrix4 matProjection, matProjectionNoZoom, matView;
        protected Vector3 position, direction, up;
        protected float near, far, dt;
        protected Point resolution;

        /// <summary>
        /// Position vector of the camera.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        
        /// <summary>
        /// Direction vector of the camera.
        /// </summary>
        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        
        /// <summary>
        /// Up vector of the camera.
        /// </summary>
        public Vector3 Up
        {
            get { return up; }
            private set { up = value; }
        }
        
        /// <summary>
        /// Near clipping plane. Implement to update the projection matrix.
        /// </summary>
        public virtual float Near
        {
            get { return near; }
            set { near = value; }
        }

        /// <summary>
        /// Far clipping plane. Implement to update the projection matrix.
        /// </summary>
        public virtual float Far
        {
            get { return far; }
            set { far = value; }
        }

        /// <summary>
        /// Speed of camera movement.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Resolution of the viewport. Implement to update the projection matrix.
        /// </summary>
        public virtual Point Resolution { get; set; }

        public Matrix4 ProjectionMatrix
        {
            get { return matProjection; }
        }

        public Matrix4 ViewMatrix
        {
            get { return matView; }
        }

        protected Camera()
        {
            Resolution = resolution = new Point();
            Position = Vector3.UnitZ;
            Direction = -Vector3.UnitZ;
            Up = Vector3.UnitY;
            Near = 0.1f;
            Far = 1000.0f;
            Speed = 100.0f;
        }

        protected abstract void UpdateProjectionMatrix();

        protected virtual void UpdateViewMatrix()
        {
            matView = Matrix4.LookAt(position, position + direction, up);
        }

        public virtual void Update(float dt)
        {
            this.dt = dt;
            UpdateViewMatrix();
        }
    }
}
