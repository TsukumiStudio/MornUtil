using System;

namespace MornLib
{
    public static class MornTimeSpan
    {
        public static TimeSpan ToTimeSpanAsSeconds(this float seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }
    }
}