namespace Simulation
{
    public class Clock
    {
        public static long GetTicks()
        {
            // Unity time is seconds!
            return (long)(UnityEngine.Time.realtimeSinceStartup * 1000f);
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

