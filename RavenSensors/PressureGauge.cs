using System.Timers;

namespace RavenSensors
{
    /// <summary>
    /// Simulates a pressure gauge.  The pressure will slowly go up and then back down again, in an endless cycle.
    /// </summary>
    public class PressureGauge : ISensor
    {
        // These constants control the simulated pressure
        private const double MovementPerInterval = 0.01;
        private const double MinPressure = 30;
        private const double MaxPresure = 70;

        private readonly Timer _timer;
        private double _pressure;
        private bool _increasing;

        /// <summary>
        /// The current pressure, in psi.
        /// </summary>
        public double Value
        {
            get { return _pressure; }
        }

        public PressureGauge() : this(MinPressure, 10)
        {
        }

        public PressureGauge(double initalPressure, double speed)
        {
            _pressure = initalPressure;
            _increasing = true;

            _timer = new Timer(speed);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // switch directions when the limits are reached
            if (_increasing && _pressure > MaxPresure)
                _increasing = false;
            else if (!_increasing && _pressure < MinPressure)
                _increasing = true;

            // adjust the pressure
            if (_increasing)
                _pressure += MovementPerInterval;
            else
                _pressure -= MovementPerInterval;
        }
    }
}
