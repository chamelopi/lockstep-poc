namespace Simulation
{
    public class Clock
    {
        public static long GetTicks()
        {
            return (long)UnityEngine.Time.realtimeSinceStartup;
        }

        public delegate void TimedCallback();

        public static long TimeIt(TimedCallback cb)
        {
            var begin = GetTicks();

            cb();

            var end = GetTicks();
            return end - begin;
        }
    }
}

