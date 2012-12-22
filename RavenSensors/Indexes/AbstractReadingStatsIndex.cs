using System;
using System.Linq;
using System.Linq.Expressions;
using Raven.Client.Indexes;
using RavenSensors.Entities;

namespace RavenSensors.Indexes
{
    public abstract class AbstractReadingStatsIndex : AbstractIndexCreationTask<Reading, ReadingStats>
    {
        protected AbstractReadingStatsIndex(TimeSpan period)
        {
            Map = readings => from reading in readings
                              let periodTicks = long.MinValue
                              let roundedTime = new DateTime(((reading.Timestamp.Ticks + periodTicks - 1) / periodTicks) * periodTicks, DateTimeKind.Utc)
                              select new {
                                             reading.SensorId,
                                             Timestamp = roundedTime,
                                             MinValue = reading.Value,
                                             MaxValue = reading.Value,
                                             TotalValue = reading.Value,
                                             Count = 1,
                                             AverageValue = 0
                                         };

            Map = new PeriodUpdateVistor(period).VisitAndConvert(Map, "AbstractReadingStatsIndex");

            Reduce = results => from result in results
                                group result by new { result.SensorId, result.Timestamp }
                                into g
                                let min = g.Min(x => x.MinValue)
                                let max = g.Max(x => x.MaxValue)
                                let total = g.Sum(x => x.TotalValue)
                                let count = g.Sum(x => x.Count)
                                select new {
                                               g.Key.SensorId,
                                               g.Key.Timestamp,
                                               MinValue = min,
                                               MaxValue = max,
                                               TotalValue = total,
                                               Count = count,
                                               AverageValue = total / count,
                                           };
        }

        /// <summary>
        /// Replaces the long.MinValue in the mapping expression with the appropriate value for the period requested
        /// </summary>
        private class PeriodUpdateVistor : ExpressionVisitor
        {
            private readonly TimeSpan _period;

            public PeriodUpdateVistor(TimeSpan period)
            {
                _period = period;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value is long && (long) node.Value == long.MinValue)
                    return base.VisitConstant(Expression.Constant(_period.Ticks, typeof(long)));

                return base.VisitConstant(node);
            }
        }
    }

    public class ReadingStats
    {
        public string SensorId { get; set; }
        public DateTime Timestamp { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double TotalValue { get; set; }
        public int Count { get; set; }
        public double AverageValue { get; set; }
    }
}
