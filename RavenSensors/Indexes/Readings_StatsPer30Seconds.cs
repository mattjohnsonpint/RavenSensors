using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPer30Seconds : AbstractReadingStatsIndex
    {
        public Readings_StatsPer30Seconds() : base(TimeSpan.FromSeconds(30))
        {
        }
    }
}
