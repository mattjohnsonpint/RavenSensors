using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Timers;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database.Server;
using RavenSensors.Entities;
using RavenSensors.Indexes;

namespace RavenSensors
{
    internal class Program
    {
        // change these as needed
        private static readonly TimeSpan TakeReadingsEvery = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DeleteReadingsOlderThan = TimeSpan.FromSeconds(30);

        private static readonly Dictionary<string, ISensor> Sensors = new Dictionary<string, ISensor>();
        private static readonly Timer PollingTimer = new Timer();

        private static IDocumentStore _documentStore;

        private static void Main()
        {
            // Initialize the database and create indexes
            var documentStore = new EmbeddableDocumentStore {
                                                                UseEmbeddedHttpServer = true,
#if DEBUG
                                                                RunInMemory = true
#endif
                                                            };
            _documentStore = documentStore;
            documentStore.SetStudioConfigToAllowSingleDb();
            documentStore.Configuration.AnonymousUserAccessMode = AnonymousUserAccessMode.All;
            documentStore.Configuration.Settings.Add("Raven/ActiveBundles", "Expiration");
            documentStore.Configuration.Settings.Add("Raven/Expiration/DeleteFrequencySeconds", "10");
            documentStore.Initialize();
            IndexCreation.CreateIndexes(typeof(Program).Assembly, documentStore);

            // Set up some pressure gauge simulators at different starting points and speeds
            AddPressureGauge("Pressure Gague 1", 30, 10);
            AddPressureGauge("Pressure Gague 2", 50, 20);
            AddPressureGauge("Pressure Gague 3", 70, 30);

            // Set up the polling timer to read the gauges periodically
            PollingTimer.Interval = TakeReadingsEvery.TotalMilliseconds;
            PollingTimer.Elapsed += PollingTimerElapsed;
            PollingTimer.Start();

            while (DoMenu())
            {
            }

            PollingTimer.Stop();
            PollingTimer.Dispose();
            foreach (var sensor in Sensors.Values)
                sensor.Dispose();

            documentStore.DocumentDatabase.StopBackgroundWorkers();
            documentStore.Dispose();
        }

        private static bool DoMenu()
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("Reading Gauges: {0}", PollingTimer.Enabled ? "YES" : "NO");
            Console.WriteLine();
            Console.WriteLine("L: Launch Raven Studio");
            Console.WriteLine("S: {0} Reading Gauges", PollingTimer.Enabled ? "Stop" : "Start");
            Console.WriteLine("D: Display reading stats from the last 10 seconds");
            Console.WriteLine("Q: Quit");
            Console.WriteLine();
            Console.Write("Option: ");
            var key = Console.ReadKey();
            Console.WriteLine();

            var choice = key.KeyChar.ToString(CultureInfo.InvariantCulture).ToUpper();

            switch (choice)
            {
                case "L":
                    var store = (EmbeddableDocumentStore) _documentStore;
                    Process.Start(store.Configuration.ServerUrl);
                    break;

                case "S":
                    PollingTimer.Enabled = !PollingTimer.Enabled;
                    break;

                case "D":
                    DisplayReadingStats();
                    break;

                case "Q":
                    return false;
            }

            return true;
        }

        private static void DisplayReadingStats()
        {
            using (var session = _documentStore.OpenSession())
            {
                var cutOff = DateTime.UtcNow.AddSeconds(-11); // a little extra helps here
                var stats = session.Query<ReadingStats, Readings_StatsPer10Seconds>()
                                   .Customize(x => x.WaitForNonStaleResultsAsOf(cutOff))
                                   .FirstOrDefault(x => x.Timestamp >= cutOff);

                if (stats == null)
                    return;

                Console.WriteLine();
                Console.WriteLine();

                Console.WriteLine("Recordings made during this period:  {0}", stats.Count);
                Console.WriteLine("Average pressure during this period: {0:N2} psi", stats.AverageValue);
                Console.WriteLine("Lowest pressure during this period:  {0:N2} psi", stats.MinValue);
                Console.WriteLine("Highest pressure during this period: {0:N2} psi", stats.MaxValue);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static void AddPressureGauge(string name, double initialPressure, double speed)
        {
            using (var session = _documentStore.OpenSession())
            {
                var sensor = new Sensor { Name = name };
                session.Store(sensor);
                session.SaveChanges();

                var gauge = new PressureGauge(initialPressure, speed);
                Sensors.Add(sensor.Id, gauge);
            }
        }

        private static void PollingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Record the sensor readings
            using (var session = _documentStore.OpenSession())
            {
                var now = DateTime.UtcNow;

                foreach (var sensor in Sensors)
                {
                    var reading = new Reading {
                                                  SensorId = sensor.Key,
                                                  Timestamp = now,
                                                  Value = sensor.Value.Value
                                              };
                    session.Store(reading);

                    // Delete this document automatically at the specified time.  Uses the Expiration bundle.
                    session.Advanced.GetMetadataFor(reading)["Raven-Expiration-Date"] = now.Add(DeleteReadingsOlderThan);
                }

                session.SaveChanges();
            }
        }
    }
}
