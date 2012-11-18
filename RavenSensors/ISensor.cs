using System;

namespace RavenSensors
{
    public interface ISensor : IDisposable
    {
        double Value { get; }
    }
}