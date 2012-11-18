using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPer10Minutes : AbstractReadingStatsIndex
    {
        public Readings_StatsPer10Minutes() : base(TimeSpan.FromMinutes(10))
        {
        }
    }
}
