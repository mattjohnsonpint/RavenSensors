using System;

namespace RavenSensors.Indexes
{
    public class Readings_StatsPer30Minutes : AbstractReadingStatsIndex
    {
        public Readings_StatsPer30Minutes() : base(TimeSpan.FromMinutes(30))
        {
        }
    }
}
