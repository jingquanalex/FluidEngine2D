using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidScene
{
    static class Timer
    {
        public static float FPS { get; private set; }
        public static float AverageFPS { get; private set; }
        public static float DT { get; private set; }

        static List<float> listFPS = new List<float>();
        public static void Update(double time)
        {
            foreach (Delayer d in listDelays)
            {
                d.CurrentTime += (float)time;

                if (d.CurrentTime >= d.LifeTime)
                    d.CurrentTime = 0.0f;
            }

            FPS = 1.0f / (float)time;
            DT = (float)time;

            if(listFPS.Count < 25)
            {
                listFPS.Add(FPS);
            }
            else if(listFPS.Count == 25)
            {
                AverageFPS = listFPS.Aggregate((a, c) => a + c) / listFPS.Count;
                listFPS.RemoveRange(0, 1);
            }
        }

        class Delayer
        {
            public float CurrentTime { get; set; }
            public float LifeTime { get; set; }

            public Delayer(float delay)
            {
                LifeTime = CurrentTime = delay;
            }
        }

        static List<Delayer> listDelays = new List<Delayer>();
        static Delayer delayer;
        public static bool RunEvery(float seconds)
        {
            if ((delayer = listDelays.Find(d => d.LifeTime == seconds)) == null)
            {
                listDelays.Add(new Delayer(seconds));
            }
            else if(delayer.CurrentTime == 0.0f)
            {
                return true;
            }

            return false;
        }
    }
}
