using UnityEngine;

namespace Simulation
{
    public class FixedPointUtil
    {
        public const float FixedPointRes = 10000f;
        public const float One = FixedPointRes;

        public static long Distance(long ax, long ay, long bx, long by)
        {

            var distX = FromFixed(ax) - FromFixed(bx);
            var distY = FromFixed(ay) - FromFixed(by);
            var val = ToFixed(Mathf.Sqrt(distX * distX + distY * distY));
            return ToFixed(Mathf.Sqrt(distX * distX + distY * distY));
        }

        public static long Distance(long ax, long ay, float bx, float by)
        {
            var distX = FromFixed(ax) - bx;
            var distY = FromFixed(ay) - by;
            var val = ToFixed(Mathf.Sqrt(distX * distX + distY * distY));
            return val;
        }

        public static float FromFixed(long val)
        {
            return (float)val / FixedPointRes;
        }

        public static long ToFixed(float val)
        {
            return (long)(val * FixedPointRes);
        }

        public static long ToFixed(double val)
        {
            return (long)(val * FixedPointRes);
        }
    }
}

