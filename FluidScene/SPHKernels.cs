using System;
using OpenTK;

namespace FluidScene
{
    class SPHKernels
    {
        float radius, radiusSquared;
        float poly6, poly6Gradient, spikey, spikeyGradient, viscosityLaplacian;

        public SPHKernels(float smoothingRadius)
        {
            radius = smoothingRadius;
            radiusSquared = radius * radius;
            poly6 = (float)(315 / (64 * Math.PI * Math.Pow(radius, 9)));
            poly6Gradient = (float)(945 / (32 * Math.PI * Math.Pow(radius, 9)));
            spikey = (float)(15 / (Math.PI * Math.Pow(radius, 6)));
            spikeyGradient = (float)-(45 / (Math.PI * Math.Pow(radius, 6)));
            viscosityLaplacian = (float)(45 / (Math.PI * Math.Pow(radius, 6)));
        }

        public float Poly6(Vector2 particlePos, Vector2 neighbourPos)
        {
            float f = radiusSquared - (particlePos - neighbourPos).LengthSquared;
            if (f < 0.0f) return 0.0f;
            return poly6 * f * f * f;
        }

        public Vector2 Poly6Gradient(Vector2 particlePos, Vector2 neighbourPos)
        {
            Vector2 r = particlePos - neighbourPos;
            float f = radiusSquared - r.LengthSquared;
            if (f < 0.0f) return Vector2.Zero;
            return poly6Gradient * f * f * -r;
        }

        public float Poly6DeltaQ()
        {
            return (float)(poly6 * Math.Pow(radiusSquared - 0.1f * radiusSquared, 3));
        }

        public float Spiky(Vector2 particlePos, Vector2 neighbourPos)
        {
            float f = radius - (particlePos - neighbourPos).Length;
            if (f < 0.0f) return 0.0f;
            return spikey * f * f * f;
        }

        public Vector2 SpikyGradient(Vector2 particlePos, Vector2 neighbourPos)
        {
            Vector2 r = particlePos - neighbourPos;
            if (r == Vector2.Zero || radiusSquared - r.LengthSquared < 0.0f) // NaN and r <= h check
                return Vector2.Zero;
            float rLength = r.Length;
            float f = radius - rLength;
            return spikeyGradient * f * f * r / rLength;
        }

        public float Viscosity(Vector2 particlePos, Vector2 neighbourPos)
        {
            float h = radius;
            float r = (particlePos - neighbourPos).Length;
            if (h - r < 0.0f) return 0.0f;
            return -(r * r * r) / (2 * h * h * h) + (r * r) / (h * h) + h / (2 * r) - 1;
        }

        public float ViscosityLaplacian(Vector2 particlePos, Vector2 neighbourPos)
        {
            float f = radius - (particlePos - neighbourPos).Length;
            if (f < 0.0f) return 0.0f;
            return viscosityLaplacian * f;
        }
    }
}
