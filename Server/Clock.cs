public class Clock {
    public static long GetTicks()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public delegate void TimedCallback();

    public static long TimeIt(TimedCallback cb) {
        var begin = GetTicks();

        cb();

        var end = GetTicks();
        return end - begin;
    }
}