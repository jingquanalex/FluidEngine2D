#define PARALLEL // If multithreading is desired

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace FluidScene
{
    /* References:
     * Particle-based Viscoelastic Fluid Simulation
     * http://www.ligum.umontreal.ca/Clavet-2005-PVFS/pvfs.pdf
     * http://www.diva-portal.org/smash/get/diva2:676516/FULLTEXT01.pdf
     * http://www8.cs.umu.se/education/examina/Rapporter/KalleSjostrom_final.pdf
    */

    class SPHClavet
    {
        // debug
        public static int loops = 0;
        public static float test = 0.0f;
        public static Vector2 testv = Vector2.Zero;

        public static float kDT = 1.0f / 60;
        public static float kGravity = 500.0f;
        public static float kInteractionRadius = 40.0f;
        public static float kLinearViscocity = 0.001f;
        public static float kQuadraticViscocity = 0.01f;
        public static float kRestDensity = 100.0f;
        public static float kStiffness = 15.0f;
        public static float kStiffnessNear = 600.0f;

        public static int kGridCellSize = 20;
        public static int kGridHalfColumns = 1920 / kGridCellSize / 2;
        public static int kGridHalfRows = 1080 / kGridCellSize / 2;
        public static float kCollisionRadius = 20.0f;

        SpatialHashGrid grid;
        List<Particle> listParticles = new List<Particle>();
        List<Line> listLines = new List<Line>();
        List<Line> listLinesCollisions = new List<Line>();
        Particle testParticle;

        MouseState mouseState;
        KeyboardState keyboardState;
        bool gravityState = true;

        public List<Line> Lines
        {
            get
            {
                List<Line> list = new List<Line>();
                list.AddRange(listLines);
                list.AddRange(listLinesCollisions);
                return list;
            }
        }

        public List<Vector2> ParticlePositions
        {
            get
            {
                List<Vector2> pos = new List<Vector2>();
                foreach (Particle p in listParticles)
                {
                    pos.Add(p.Position);
                }
                return pos;
            }
        }

        public List<Color4> ParticleColors
        {
            get
            {
                List<Color4> col = new List<Color4>();
                foreach (Particle p in listParticles)
                {
                    col.Add(p.Color);
                }
                return col;
            }
        }

        public int ParticleCount
        {
            get { return listParticles.Count; }
        }

        public SPHClavet()
        {
            grid = new SpatialHashGrid(kGridCellSize, kGridHalfColumns, kGridHalfRows);
            AddLine(-grid.HalfWidth, -grid.HalfHeight, grid.HalfWidth, -grid.HalfHeight, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
            AddLine(grid.HalfWidth, -grid.HalfHeight, grid.HalfWidth, grid.HalfHeight, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
            AddLine(grid.HalfWidth, grid.HalfHeight, -grid.HalfWidth, grid.HalfHeight, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
            AddLine(-grid.HalfWidth, grid.HalfHeight, -grid.HalfWidth, -grid.HalfHeight, new Color4(0.0f, 0.0f, 0.0f, 1.0f));

            //youtube 720p frame
            AddLineCollision(-640, -360, 640, -360);
            AddLineCollision(640, -360, 640, 360);
            AddLineCollision(640, 360, -640, 360);
            AddLineCollision(-640, 360, -640, -360);

            AddLineCollision(-300, -300, 300, -300);
            AddLineCollision(-400, 300, -300, -300);
            AddLineCollision(300, -300, 400, 300);
            AddLineCollision(400, 300, -400, 300);
        }

        public void Update()
        {
            mouseState = OpenTK.Input.Mouse.GetState();
            keyboardState = OpenTK.Input.Keyboard.GetState();

            if (keyboardState[Key.Y]) kRestDensity += 1.01f;
            if (keyboardState[Key.H]) kRestDensity -= 1.01f;
            if (keyboardState[Key.U]) kStiffness += 1.01f;
            if (keyboardState[Key.J]) kStiffness -= 1.01f;
            if (keyboardState[Key.I]) kStiffnessNear += 10.01f;
            if (keyboardState[Key.K]) kStiffnessNear -= 10.01f;
            if (keyboardState[Key.Keypad5]) kLinearViscocity += 1f;
            if (keyboardState[Key.Keypad2]) kLinearViscocity -= 1f;
            if (keyboardState[Key.Keypad6]) kQuadraticViscocity += 0.001f;
            if (keyboardState[Key.Keypad3]) kQuadraticViscocity -= 0.001f;

            if (keyboardState[Key.Q])
            {
                Random r = new Random();
                if (Timer.RunEvery(0.1f))
                {
                    testParticle = new Particle(Mouse.PositionGlobal.X, Mouse.PositionGlobal.Y);
                    testParticle.Color.R = 1.0f;
                    listParticles.Add(testParticle);
                }
            }

            float timeStep = (float)Math.Min(Timer.DT, kDT);
            //float timeStep = kDT;

            DrawLine();

            if (ParticleCount > 0)
            {
                ApplyExternalForces(timeStep);
                ApplyViscosity(timeStep);
                AdvanceParticles(timeStep);
                grid.UpdateNeighbours(listParticles);
                DoubleDensityRelaxation(timeStep);
                ResolveCollisions(timeStep);
                UpdateVelocity(timeStep);
                ColorParticles();
            }
        }

        Vector2 prevMousePos;
        void ApplyExternalForces(float dt)
        {
            foreach (Particle p in listParticles)
            {
                // Gravity
                if (gravityState)
                {
                    p.Velocity.Y -= kGravity * dt;
                }

                // Attract particles towards cursor
                if (mouseState[MouseButton.Right])
                {
                    Vector2 dir = Mouse.PositionGlobalv - p.Position;
                    if (dir == Vector2.Zero) return;
                    float distSquared = dir.LengthSquared;
                    float radius = 150.0f;

                    if (distSquared < radius * 50)
                    {
                        distSquared = (float)Math.Sqrt(distSquared);
                        p.Velocity += dir / distSquared * 5 * kGravity * dt;
                        if (Mouse.PositionGlobalv != prevMousePos)
                            p.Velocity += (Mouse.PositionGlobalv - prevMousePos) * kGravity * 0.5f * dt;
                    }
                }
            }

            prevMousePos = Mouse.PositionGlobalv;
        }

        void ApplyViscosity(float dt)
        {
#if PARALLEL
            Parallel.ForEach(listParticles, p =>
#else
            foreach (Particle p in listParticles)
#endif
            {
                foreach (Particle n in p.Neighbours)
                {
                    if (p.index < n.index)
                    {
                        Vector2 v = n.Position - p.Position;
                        float vLength = v.Length;
                        Vector2 vn = v / vLength;
                        float u = Vector2.Dot(p.Velocity - n.Velocity, vn);
                        if (u > 0.0f)
                        {
                            float q = 1.0f - vLength / p.Radius;
                            Vector2 impulse = 0.5f * dt * q * (kLinearViscocity * u + kQuadraticViscocity * u * u) * vn;
                            if (Math.Abs(impulse.X) > 10000.0f) impulse.X /= 10000.0f;
                            if (Math.Abs(impulse.Y) > 10000.0f) impulse.Y /= 10000.0f;
                            //if (float.IsInfinity(impulse.LengthSquared)) System.Diagnostics.Debugger.Break();
                            p.Velocity -= impulse;
                            n.Velocity += impulse;
                        }

                        // Color Mixing
                        if (Timer.RunEvery(0.1f))
                        //if ((p.Velocity + n.Velocity).Length > 500.0f)
                        {
                            float mixWeight = 0.5f;
                            float r = p.Color.R * mixWeight + n.Color.R * (1.0f - mixWeight);
                            float g = p.Color.G * mixWeight + n.Color.G * (1.0f - mixWeight);
                            float b = p.Color.B * mixWeight + n.Color.B * (1.0f - mixWeight);
                            p.Color = n.Color = new Color4(r, g, b, 1.0f);
                        }
                    }
                }
            }
#if PARALLEL
);
#endif
        }

        void AdvanceParticles(float dt)
        {
            // Update particle position with its accumulated forces
            // with a prediction-relaxation integrator
            foreach (Particle p in listParticles)
            {
                p.PositionPrev = p.Position;
                p.Position += p.Velocity * dt;
            }

            // Remove particles outside of grid
            listParticles.RemoveAll(p2 =>
                   p2.Position.X < -grid.HalfWidth || p2.Position.X > grid.HalfWidth ||
                   p2.Position.Y < -grid.HalfHeight || p2.Position.Y > grid.HalfHeight);
        }

        void DoubleDensityRelaxation(float dt)
        {
            loops = 0;
#if PARALLEL
            Parallel.ForEach(listParticles, p =>
#else
            foreach (Particle p in listParticles)
#endif
            {
                // Sample neighbours for particle density
                // with a quadratic spike kernel
                p.Density = p.DensityNear = 0.0f;
                foreach (Particle n in p.Neighbours)
                {
                    Vector2 v = n.Position - p.Position;
                    float q = 1.0f - v.Length / p.Radius;
                    p.Density += q * q;
                    p.DensityNear += q * q * q;
                }

                // The higher rest density is, the higher the density and surface tension
                p.Pressure = kStiffness * (p.Density - p.RestDensity);
                p.PressureNear = kStiffnessNear * p.DensityNear;

                // Keep within sensible range to avoid infinity/NaN (particles disappearing)
                if (p.Pressure + p.PressureNear < 0.000001f || p.Pressure + p.PressureNear > 1000000f)
                {
                    p.Pressure = 0;
                    p.PressureNear = 0;
                }

                Vector2 dx = Vector2.Zero;
                foreach (Particle n in p.Neighbours)
                {
                    Vector2 v = n.Position - p.Position;
                    if (v != Vector2.Zero)
                    {
                        loops++;
                        float length = v.Length;
                        float q = 1.0f - v.Length / p.Radius;
                        Vector2 displacement = 0.5f * dt * dt * (p.Pressure * q + p.PressureNear * q * q) * v / length;
                        n.Position += displacement;
                        dx -= displacement;
                    }
                }
                p.Position += dx;
            }
#if PARALLEL
);
#endif
        }

        void ResolveCollisions(float dt)
        {
#if PARALLEL
            Parallel.ForEach(listParticles, p =>
#else
            foreach (Particle p in listParticles)
#endif
            {
                foreach (Line line in listLinesCollisions)
                {
                    if (!line.IsDot())
                    {
                        float f = Vector2.Dot(p.Position - line.PointStart, line.Vector) / line.Vector.LengthSquared;

                        if (f >= 0.0f && f <= 1.0f)
                        {
                            Vector2 proj = f * line.Vector;
                            float distSquared = (proj - (p.Position - line.PointStart)).LengthSquared;

                            if (distSquared < kCollisionRadius * kCollisionRadius)
                            {
                                Vector2 lineN = line.Vector.PerpendicularLeft.Normalized();
                                //Vector2 velocityR = -2 * Vector2.Dot(p.Velocity, lineN) * lineN;
                                //p.Position += velocityR * 0.9f * dt;

                                p.Position += lineN * (float)Math.Pow(kCollisionRadius - Math.Sqrt(distSquared), 2) * dt;
                            }
                        }
                    }
                }
            }
#if PARALLEL
);
#endif
        }

        void UpdateVelocity(float dt)
        {
            foreach (Particle p in listParticles)
            {
                p.Velocity = (p.Position - p.PositionPrev) / dt;
            }
        }

        void ColorParticles()
        {
            foreach (Particle p in listParticles)
            {
                if (mouseState[MouseButton.Middle])
                {
                    if (testParticle != null)
                    {
                        p.Color.G = 0.0f;
                        foreach (Particle n in testParticle.Neighbours)
                        {
                            n.Color.G = 1.0f;
                        }
                    }
                }

                p.Color.A = 1 - 1 / p.Density;
                test = p.Color.A;
            }
        }

        void AddLine(float startX, float startY, float endX, float endY)
        {
            listLines.Add(new Line(new Vector2(startX, startY), new Vector2(endX, endY)));
        }

        void AddLine(float startX, float startY, float endX, float endY, Color4 color)
        {
            listLines.Add(new Line(new Vector2(startX, startY), new Vector2(endX, endY), color));
        }

        void AddLineCollision(float startX, float startY, float endX, float endY)
        {
            listLinesCollisions.Add(new Line(new Vector2(startX, startY), new Vector2(endX, endY)));
        }

        void RemoveLastLine()
        {
            if (listLinesCollisions.Count > 0)
            {
                listLinesCollisions.RemoveRange(listLinesCollisions.Count - 1, 1);
            }
        }

        bool isDrawingLine = false;
        void DrawLine()
        {
            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                if (isDrawingLine)
                {
                    listLinesCollisions[listLinesCollisions.Count - 1].PointEnd = Mouse.PositionGlobalv;
                }
                else
                {
                    listLinesCollisions.Add(new Line(Mouse.PositionGlobalv, Mouse.PositionGlobalv));
                    isDrawingLine = true;
                }
            }
            else if (mouseState.IsButtonUp(MouseButton.Left))
            {
                if (isDrawingLine)
                {
                    // Don't draw dots
                    if (listLinesCollisions[listLinesCollisions.Count - 1].IsDot())
                    {
                        RemoveLastLine();
                    }

                    isDrawingLine = false;
                }
            }
        }

        public void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
        }

        public void MouseUpHandler(object sender, MouseButtonEventArgs e)
        {
        }

        public void KeyDownHandler(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10));
            }

            if (e.Key == Key.Number1)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(1.0f, 0.0f, 0.0f, 1.0f)));
            }

            if (e.Key == Key.Number2)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(0.0f, 1.0f, 0.0f, 1.0f)));
            }

            if (e.Key == Key.Number3)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(0.0f, 0.0f, 1.0f, 1.0f)));
            }

            if (e.Key == Key.Number4)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(0.0f, 1.0f, 1.0f, 1.0f)));
            }

            if (e.Key == Key.Number5)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(1.0f, 1.0f, 0.0f, 1.0f)));
            }

            if (e.Key == Key.Number6)
            {
                for (int i = -10; i < 10; i++)
                    for (int j = -10; j < 10; j++)
                        listParticles.Add(new Particle(Mouse.PositionGlobalv.X + i * 10, Mouse.PositionGlobalv.Y + j * 10,
                            new Color4(1.0f, 0.0f, 1.0f, 1.0f)));
            }

            if (e.Key == Key.C)
            {
                listParticles.Clear();
            }

            if (e.Key == Key.G)
            {
                gravityState = !gravityState;
            }

            if (e.Key == Key.R)
            {
                RemoveLastLine();
            }
        }

        class Particle
        {
            public Vector2 Position;
            public Vector2 PositionPrev;
            public Vector2 Velocity;
            public Color4 Color;
            public float RestDensity;
            public float Density;
            public float DensityNear;
            public float Pressure;
            public float PressureNear;
            public float Radius;
            public Point Key;
            public List<Particle> Neighbours;
            public uint index = 0;

            static uint indexCount = 0;

            public Particle(float posX, float posY)
            {
                Position = PositionPrev = new Vector2(posX, posY);
                Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                RestDensity = kRestDensity;
                Radius = kInteractionRadius;
                Neighbours = new List<Particle>();
                index = indexCount++;
            }

            public Particle(float posX, float posY, Color4 color)
            {
                Position = PositionPrev = new Vector2(posX, posY);
                Color = color;
                RestDensity = kRestDensity;
                Radius = kInteractionRadius;
                Neighbours = new List<Particle>();
                index = indexCount++;
            }

            public Particle(float posX, float posY, float restdensity, Color4 color)
            {
                Position = PositionPrev = new Vector2(posX, posY);
                Color = color;
                RestDensity = restdensity;
                Radius = kInteractionRadius;
                Neighbours = new List<Particle>();
                index = indexCount++;
            }
        }

        class SpatialHashGrid
        {
            int cellSize, rows, columns, halfRows, halfColumns;
            Dictionary<int, List<Particle>> buckets;

            public int HalfWidth
            {
                get { return cellSize * halfColumns; }
            }

            public int HalfHeight
            {
                get { return cellSize * halfRows; }
            }

            public SpatialHashGrid(int cellsize, int halfColumns, int halfRows)
            {
                this.cellSize = cellsize;
                this.halfColumns = halfColumns;
                this.halfRows = halfRows;
                this.columns = halfColumns * 2 + 1;
                this.rows = halfRows * 2 + 1;
                this.buckets = new Dictionary<int, List<Particle>>();
            }

            public void UpdateNeighbours(List<Particle> particleList)
            {
                if (particleList.Count > 0)
                {
                    // Clear buckets
                    buckets.Clear();

                    // Remove particles outside of grid
                    particleList.RemoveAll(p2 =>
                           p2.Position.X < -HalfWidth || p2.Position.X > HalfWidth ||
                           p2.Position.Y < -HalfHeight || p2.Position.Y > HalfHeight);

                    foreach (Particle p in particleList)
                    {
                        // Make key from particle's position
                        p.Key = GetHashKey(p.Position);

                        // Add particle to the cell and neighbour's cell
                        for (int x = p.Key.X - 1; x <= p.Key.X + 1; x++)
                        {
                            for (int y = p.Key.Y - 1; y <= p.Key.Y + 1; y++)
                            {
                                int hashValue = GetHashValue(x, y);

                                List<Particle> refList;
                                if (buckets.TryGetValue(hashValue, out refList))
                                {
                                    refList.Add(p);
                                }
                                else
                                {
                                    buckets.Add(hashValue, new List<Particle>());
                                    buckets[hashValue].Add(p);
                                }
                            }
                        }
                    }
                    
#if PARALLEL
                    Parallel.ForEach(particleList, p =>
#else
                    foreach (Particle p in particleList)
#endif
                    {
                        p.Neighbours.Clear();
                        foreach (Particle n in buckets[GetHashValue(p.Key.X, p.Key.Y)])
                        {
                            if (p != n)
                            {
                                if ((p.Position - n.Position).LengthSquared < kGridCellSize * kGridCellSize)
                                    p.Neighbours.Add(n);
                            }
                        }
                    }
#if PARALLEL
                    );
#endif
                }
            }

            public List<Particle> FindPossibleNeighbours(Particle p)
            {
                List<Particle> neighbours;

                buckets.TryGetValue(GetHashValue(p.Key.X, p.Key.Y), out neighbours);

                return neighbours;
            }

            int GetHashValue(Vector2 position)
            {
                int x = (int)Math.Floor(position.X / cellSize);
                int y = (int)Math.Floor(position.Y / cellSize);
                return GetHashValue(x, y);
            }

            int GetHashValue(int x, int y)
            {
                return x + y * rows;
                //return (x * 73856093 ^ y * 19349663) % (columns * rows);
            }

            Point GetHashKey(Vector2 position)
            {
                int x = (int)Math.Floor(position.X / cellSize);
                int y = (int)Math.Floor(position.Y / cellSize);
                return new Point(x, y);
            }

            public List<Particle> FindPossibleCollision(Line line)
            {
                List<Particle> neighbours = new List<Particle>();
                List<Particle> n;

                for (int sample = 0; sample < 3; sample++)
                {
                    float dx = line.PointEnd.X - line.PointStart.X;
                    float dy = line.PointEnd.Y - line.PointStart.Y;

                    if (sample == 1)
                    {
                        dx += cellSize;
                        dy += cellSize;
                    }
                    else if (sample == 2)
                    {
                        dx -= cellSize;
                        dy -= cellSize;
                    }
                    else if (sample == 3)
                    {
                        dx += cellSize;
                        dy -= cellSize;
                    }
                    else if (sample == 4)
                    {
                        dx -= cellSize;
                        dy += cellSize;
                    }

                    Point startCoord = GetHashKey(line.PointStart);
                    Point endCoord = GetHashKey(line.PointEnd);

                    if (startCoord == endCoord)
                    {
                        if (buckets.TryGetValue(GetHashValue(startCoord.X, startCoord.Y), out n)) neighbours.AddRange(n);
                        return neighbours;
                    }

                    float length = Math.Abs(dx) > Math.Abs(dy) ? Math.Abs(dx) : Math.Abs(dy);
                    dx /= length;
                    dy /= length;
                    float x = line.PointStart.X + 0.5f * Math.Sign(dx);
                    float y = line.PointStart.Y + 0.5f * Math.Sign(dy);

                    for (int i = 0; i < length; i++)
                    {
                        if (buckets.TryGetValue(GetHashValue(new Vector2(x, y)), out n)) neighbours.AddRange(n);
                        x += dx;
                        y += dy;
                    }
                }

                return neighbours;
            }
        }
    }
}
