using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPerMinute : AbstractReadingStatsIndex
    {
        public Readings_StatsPerMinute() : base(TimeSpan.FromMinutes(1))
        {
        }
    }
}
