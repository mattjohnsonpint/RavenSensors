using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPer10Seconds : AbstractReadingStatsIndex
    {
        public Readings_StatsPer10Seconds() : base(TimeSpan.FromSeconds(10))
        {
        }
    }
}
