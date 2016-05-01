using System;

namespace DataCenter
{
    public class Utils
    {
        public static IDisposable SetTimeout(Action action, int durationInMilliseconds)
        {
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Elapsed += (source, e) =>
            {
                action();
                timer.Dispose();
            };

            timer.AutoReset = false;
            timer.Enabled = true;
            return timer;
        }
    }
}