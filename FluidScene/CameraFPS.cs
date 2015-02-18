using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FluidScene
{
    class CameraFPS : Camera
    {
        float fov, aspect;

        /// <summary>
        /// Resolution of the viewport to build the aspect ratio.
        /// </summary>
        public override Point Resolution
        {
            get { return resolution; }
            set { resolution = value; aspect = resolution.X / resolution.Y; UpdateProjectionMatrix(); }
        }

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
        /// Field of view in degrees of the camera.
        /// </summary>
        public float FOV
        {
            get { return fov; }
            set { fov = value; UpdateProjectionMatrix(); }
        }

        /// <summary>
        /// Aspect ratio of the camera.
        /// </summary>
        public float Aspect
        {
            get { return aspect; }
            set { aspect = value; UpdateProjectionMatrix(); }
        }

        /// <summary>
        /// Speed of camera rotation.
        /// </summary>
        public float RotateSpeed { get; set; }

        /// <summary>
        /// Bounds of the gamewindow. Used to center the mouse.
        /// </summary>
        public Rectangle Bounds { get; set; }

        public CameraFPS() : base()
        {
            fov = MathHelper.DegreesToRadians(75.0f);
            aspect = 4.0f / 3;
            RotateSpeed = 0.001f;

            UpdateProjectionMatrix();
            UpdateViewMatrix();
        }

        public void Forward()
        {
            position += direction * Speed * dt;
        }

        public void Backward()
        {
            position += -direction * Speed * dt;
        }

        public void StrafeLeft()
        {
            position += Vector3.Cross(up, direction).Normalized() * Speed * dt;
        }

        public void StrafeRight()
        {
            position += -Vector3.Cross(up, direction).Normalized() * Speed * dt;
        }

        MouseState currState, prevState;
        float pitchAngle, maxPitchAngle = MathHelper.DegreesToRadians(89.0f);
        public void RotationHandler(object sender, MouseMoveEventArgs e)
        {
            currState = OpenTK.Input.Mouse.GetState();

            if (currState[MouseButton.Right] && currState != prevState)
            {
                int mouseDeltaX = currState.X - prevState.X;
                int mouseDeltaY = currState.Y - prevState.Y;
                if (resolution != new Point()) OpenTK.Input.Mouse.SetPosition(Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2);

                Console.WriteLine(mouseDeltaX);

                Matrix4 matYaw = Matrix4.CreateFromAxisAngle(up, MathHelper.Pi * -mouseDeltaX * RotateSpeed);
                Vector3.Transform(ref direction, ref matYaw, out direction);

                float deltaAngle = MathHelper.Pi * mouseDeltaY * RotateSpeed;

                //Reach pitch limits smoothly
                if (pitchAngle + deltaAngle < -maxPitchAngle)
                {
                    Matrix4 matPitch = Matrix4.CreateFromAxisAngle(Vector3.Cross(up, direction).Normalized(), -maxPitchAngle - pitchAngle);
                    Vector3.Transform(ref direction, ref matPitch, out direction);
                    pitchAngle = -maxPitchAngle;
                }
                else if (pitchAngle + deltaAngle > maxPitchAngle)
                {
                    Matrix4 matPitch = Matrix4.CreateFromAxisAngle(Vector3.Cross(up, direction).Normalized(), maxPitchAngle - pitchAngle);
                    Vector3.Transform(ref direction, ref matPitch, out direction);
                    pitchAngle = maxPitchAngle;
                }
                else
                {
                    Matrix4 matPitch = Matrix4.CreateFromAxisAngle(Vector3.Cross(up, direction).Normalized(), deltaAngle);
                    Vector3.Transform(ref direction, ref matPitch, out direction);
                    pitchAngle += deltaAngle;
                }
            }

            prevState = currState;
        }

        protected override void UpdateProjectionMatrix()
        {
            Matrix4.CreatePerspectiveFieldOfView(fov, aspect, near, far, out matProjection);
        }
    }
}
