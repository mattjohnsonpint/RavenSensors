using System;

namespace RavenSensors.Entities
{
    public class Reading
    {
        public string Id { get; set; }
        public string SensorId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}
