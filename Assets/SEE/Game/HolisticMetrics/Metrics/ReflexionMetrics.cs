using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// Provides metrics for reflexion cities.
    /// Specifically, the number of convergent, absent, divergent, and unmapped edges are provided.
    /// If the given city is not a reflexion city, no metrics will be returned.
    /// </summary>
    internal class ReflexionMetrics : Metric
    {
        internal override MetricValue Refresh(SEECity city)
        {
            ReflexionGraph graph;
            if (city is SEEReflexionCity reflexionCity)
            {
                graph = reflexionCity.ReflexionGraph;
            }
            else
            {
                // No metrics to report.
                return new MetricValueCollection();
            }

            // Map from states to number of times they occur within the graph
            Dictionary<State, int> states = graph.Edges().Select(ReflexionGraphTools.State)
                                                 .GroupBy(x => x)
                                                 .ToDictionary(x => x.Key, x => x.Count());
            int maximum = states.Values.Max();

            int Count(State state) => states.TryGetValue(state, out int value) ? value : 0;

            MetricValueRange convergences = new MetricValueRange
            {
                Name = "Convergent and allowed edges",
                DecimalPlaces = 0,
                Value = Count(State.Convergent) + Count(State.AllowedAbsent)
                                                 + Count(State.Allowed) + Count(State.ImplicitlyAllowed),
                Lower = 0,
                Higher = maximum
            };
            MetricValueRange absences = new MetricValueRange
            {
                Name = "Absent edges",
                DecimalPlaces = 0,
                Value = Count(State.Absent),
                Lower = 0,
                Higher = maximum
            };
            MetricValueRange divergences = new MetricValueRange
            {
                Name = "Divergent edges",
                DecimalPlaces = 0,
                Value = Count(State.Divergent),
                Lower = 0,
                Higher = maximum
            };
            MetricValueRange unmapped = new MetricValueRange
            {
                Name = "Unmapped edges",
                DecimalPlaces = 0,
                Value = Count(State.Undefined) + Count(State.Unmapped) + Count(State.Specified),
                Lower = 0,
                Higher = maximum
            };

            return new MetricValueCollection
            {
                Name = "Reflexion Metrics",
                MetricValues = new List<MetricValueRange>
                {
                    convergences, divergences, absences, unmapped
                }
            };
        }
    }
}