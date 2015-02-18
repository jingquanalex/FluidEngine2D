#define PARALLEL // If multithreading is desired

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace FluidScene
{
    /* References:
     * Position Based Fluids
     * http://mmacklin.dreamhosters.com/pbf_sig_preprint.pdf
     * https://www.ifi.uzh.ch/vmml/publications/pcisph/pcisph.pdf‎
     * http://cg.informatik.uni-freiburg.de/publications/2011_CGF_dataStructuresSPH.pdf
     * http://cg.informatik.uni-freiburg.de/publications/2013_TVCG_IISPH.pdf
     * http://cg.informatik.uni-freiburg.de/publications/2010_VRIPHYS_boundaryHandling.pdf
     * http://nccastaff.bournemouth.ac.uk/jmacey/MastersProjects/MSc11/Rajiv/MasterThesis.pdf
    */

    class SPHPBF
    {
        // debug
        public static int loops = 0;
        public static float test = 0.0f;
        public static Vector2 testv = Vector2.Zero;

        public static float kGravity = 400.0f;
        public static float kSmoothingRadius = 20.0f;
        public static float kRestDensity = 0.00045f;
        public static float kRelaxation = 1.1f;

        public static int kGridHalfSize = (int)kSmoothingRadius;
        public static int kGridHalfColumns = 1920 / kGridHalfSize / 2;
        public static int kGridHalfRows = 1080 / kGridHalfSize / 2;
        public static float kCollisionRadius = 20.0f;

        SpatialHashGrid grid;
        SPHKernels kernel;
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
                List<Line> list = new List<Line>(listLines);
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

        public SPHPBF()
        {
            kernel = new SPHKernels(kSmoothingRadius);
            grid = new SpatialHashGrid(kGridHalfSize, kGridHalfColumns, kGridHalfRows);
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

            if (keyboardState[Key.Y]) kRestDensity += 0.0000005f;
            if (keyboardState[Key.H]) kRestDensity -= 0.0000005f;
            if (keyboardState[Key.U]) kRelaxation += 0.005f;
            if (keyboardState[Key.J]) kRelaxation -= 0.005f;

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

            DrawLine();

            //float timeStep = (float)Math.Min(Timer.DT, kDT);
            float timeStep = Timer.DT;

            //if (Timer.RunEvery(0.1f))
            {
                PredictPosition(timeStep);
                grid.UpdateNeighbours(listParticles);
                ResolveInteractions(timeStep);
                UpdatePosition(timeStep);
                ColorParticles();
            }
        }

        Vector2 prevMousePos;
        Vector2 GetExternalForces(Particle p)
        {
            Vector2 externalForces = Vector2.Zero;

            // Gravity
            if (gravityState)
            {
                externalForces.Y -= kGravity;
            }

            // Attract particles towards cursor
            if (mouseState[MouseButton.Right])
            {
                Vector2 v = Mouse.PositionGlobalv - p.Position;
                if (v != Vector2.Zero)
                {
                    float distSquared = v.LengthSquared;
                    float radius = 200.0f;

                    if (distSquared < radius * radius)
                    {
                        distSquared = (float)Math.Sqrt(distSquared);
                        externalForces += v / distSquared * 5 * kGravity;
                        //if (Mouse.PositionGlobalv != prevMousePos)
                            //externalForces += (Mouse.PositionGlobalv - prevMousePos) * 5 * kGravity * 0.5f;
                    }
                }
            }

            prevMousePos = Mouse.PositionGlobalv;
            return externalForces;
        }

        void PredictPosition(float dt)
        {
            foreach (Particle p in listParticles)
            {
                p.Velocity += GetExternalForces(p) * dt;
                p.PositionPredicted = p.Position + p.Velocity * dt;

                p.Pressure = 0.0f;
                p.PressureForce = Vector2.Zero;
            }
        }

        void ResolveInteractions(float dt)
        {
            int i = 0, solverIterations = 3;

            while (i < solverIterations)
            {
                // Note: Particle p's neighbours contains p itself
                // Find density constraint (lambda)
                foreach (Particle p in listParticles)
                {
                    float pDensity = 0.0f, pGradient = 0.0f;
                    foreach (Particle n in p.Neighbours)
                    {
                        // Find particle p's density
                        pDensity += kernel.Poly6(p.PositionPredicted, n.PositionPredicted);

                        // Find gradient of constraint
                        if (p != n) // For neighbour particle (j)
                        {
                            pGradient += (-kernel.SpikyGradient(p.PositionPredicted, n.PositionPredicted) / kRestDensity).LengthSquared;
                        }
                        else // For non-neighbouring particle (i)
                        {
                            foreach (Particle nn in n.Neighbours)
                            {
                                if (n != nn) // Neighbours only (exclude i)
                                {
                                    pGradient += (kernel.SpikyGradient(n.PositionPredicted, nn.PositionPredicted) / kRestDensity).LengthSquared;
                                }
                            }
                        }
                    }

                    p.DensityConstraint = -(pDensity / kRestDensity - 1.0f) / (pGradient + kRelaxation);
                    if (float.IsNaN(p.DensityConstraint)) throw new ArithmeticException();
                    test = p.DensityConstraint;
                }

                // Find delta position
                foreach (Particle p in listParticles)
                {
                    p.PositionDelta = Vector2.Zero;
                    foreach (Particle n in p.Neighbours)
                    {
                        if (p != n)
                        {
                            p.PositionDelta += (p.DensityConstraint + n.DensityConstraint/* + TensileInstability(p, n)*/) *
                                kernel.SpikyGradient(p.PositionPredicted, n.PositionPredicted);
                            if (float.IsNaN(p.PositionDelta.LengthSquared)) throw new ArithmeticException();
                        }
                    }
                    p.PositionDelta /= kRestDensity;

                    // Explosion control (debug)
                    if (p.PositionDelta.LengthSquared > kSmoothingRadius * kSmoothingRadius)
                        p.PositionDelta = Vector2.Zero;

                    // Resolve line collisions
                    ResolveCollisions(p, dt);
                }

                foreach (Particle p in listParticles)
                {
                    testv = p.PositionDelta;
                    p.PositionPredicted += p.PositionDelta;
                }

                i++;
            }
        }

        void UpdatePosition(float dt)
        {
            foreach (Particle p in listParticles)
            {
                p.Velocity = (p.PositionPredicted - p.Position) / dt;
                p.Position = p.PositionPredicted;
            }
        }

        float TensileInstability(Particle p, Particle n)
        {
            return -0.1f * (float)(Math.Pow(kernel.Poly6(p.PositionPredicted, n.PositionPredicted) /
                kernel.Poly6DeltaQ(), 4));
        }

        void ResolveCollisions(Particle p, float dt)
        {
            foreach (Line line in listLinesCollisions)
            {
                if (!line.IsDot())
                {
                    Vector2 particleVector = p.PositionPredicted - line.PointStart;
                    float proj = Vector2.Dot(particleVector, line.Vector) / line.Vector.LengthSquared;

                    if (proj > 0.0f && proj < 1.0f)
                    {
                        Vector2 projVector = proj * line.Vector;
                        float distSquared = (projVector - particleVector).LengthSquared;

                        if (distSquared < kCollisionRadius * kCollisionRadius)
                        {
                            if (Vector2.Dot(p.Velocity, line.Vector.PerpendicularLeft) < 0.0f)
                            {
                                Vector2 lineN = line.Vector.PerpendicularLeft.Normalized();
                                Vector2 vReflect = -2 * Vector2.Dot(p.Velocity, lineN) * lineN;
                                p.PositionPredicted += vReflect * 0.8f * dt;
                            }
                            else
                            {
                                Vector2 lineN = line.Vector.PerpendicularLeft.Normalized();
                                p.PositionPredicted += lineN * 50 * dt;
                            }
                        }
                    }
                }
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
                        p.Color.R = 1.0f;
                        foreach (Particle n in testParticle.Neighbours)
                        {
                            n.Color.R = 0.0f;
                        }
                    }
                }
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
            public Vector2 PositionDelta;
            public Vector2 PositionPredicted;
            public Vector2 Velocity;
            public float DensityConstraint;
            public float RestDensity;
            public float Pressure;
            public Vector2 PressureForce;
            public Color4 Color;
            public Point Key;
            public List<Particle> Neighbours;
            public uint index = 0;

            static uint indexCounter = 0;

            public Particle(float posX, float posY)
            {
                Position = new Vector2(posX, posY);
                Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                RestDensity = kRestDensity;
                Neighbours = new List<Particle>();
                index = indexCounter++;
            }

            public Particle(float posX, float posY, Color4 color)
            {
                Position = new Vector2(posX, posY);
                Color = color;
                RestDensity = kRestDensity;
                Neighbours = new List<Particle>();
                index = indexCounter++;
            }

            public Particle(float posX, float posY, float restdensity, Color4 color)
            {
                Position = new Vector2(posX, posY);
                Color = color;
                RestDensity = restdensity;
                Neighbours = new List<Particle>();
                index = indexCounter++;
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
                            if ((p.PositionPredicted - n.Position).LengthSquared < kGridHalfSize * kGridHalfSize)
                                p.Neighbours.Add(n);
                        }
                    }
#if PARALLEL
                    );
#endif
                }
            }

            public List<Particle> FindPossibleNeighbours(Vector2 position)
            {
                List<Particle> neighbours;

                buckets.TryGetValue(GetHashValue(position), out neighbours);

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

            /*
            public List<Particle> FindPossibleCollision(ColoredLine line)
            {
                List<Particle> neighbours = new List<Particle>();
                List<Particle> n;

                for (int sample = 0; sample < 3; sample++)
                {
                    float dx = line.PositionEnd.X - line.PositionStart.X;
                    float dy = line.PositionEnd.Y - line.PositionStart.Y;

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

                    Point startCoord = GetGridKey(line.PositionStart);
                    Point endCoord = GetGridKey(line.PositionEnd);

                    if (startCoord == endCoord)
                    {
                        if (buckets.TryGetValue(GetHashValue(startCoord.X, startCoord.Y), out n)) neighbours.AddRange(n);
                        return neighbours;
                    }

                    float length = Math.Abs(dx) > Math.Abs(dy) ? Math.Abs(dx) : Math.Abs(dy);
                    dx /= length;
                    dy /= length;
                    float x = line.PositionStart.X + 0.5f * Math.Sign(dx);
                    float y = line.PositionStart.Y + 0.5f * Math.Sign(dy);

                    for (int i = 0; i < length; i++)
                    {
                        if (buckets.TryGetValue(GetHashValue(new Vector2(x, y)), out n)) neighbours.AddRange(n);
                        x += dx;
                        y += dy;
                    }
                }

                return neighbours;
            }*/
        }
    }
}
