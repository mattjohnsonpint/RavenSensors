using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPerHour : AbstractReadingStatsIndex
    {
        public Readings_StatsPerHour() : base(TimeSpan.FromHours(1))
        {
        }
    }
}
