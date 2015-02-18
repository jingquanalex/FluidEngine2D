using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;

namespace FluidScene
{
    static class Mouse
    {
        static Point positionLocal;
        static Point positionGlobal;

        public static Point PositionLocal
        {
            get { return positionLocal; }
            set { positionLocal = value; }
        }

        public static Point PositionGlobal
        {
            get { return positionGlobal; }
            private set { positionGlobal = value; }
        }

        public static Vector2 PositionGlobalv
        {
            get { return new Vector2(positionGlobal.X, positionGlobal.Y); }
        }

        public static void Update(Camera2D camera, int mouseX, int mouseY)
        {
            positionLocal = new Point(mouseX, mouseY);
            positionGlobal = new Point((int)((mouseX - camera.Resolution.X / 2 + camera.Position.X / camera.Zoom) * camera.Zoom),
                (int)(-(mouseY - camera.Resolution.Y / 2 - camera.Position.Y / camera.Zoom) * camera.Zoom));
        }
    }
}
