using Accord.MachineLearning;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class HugMeFilter : IRecommendationFilter
    {
        private Dictionary<(string candidateId, string clusterId), MappingPair> mappingPairs = new();

        public IEnumerable<MappingPair> MappingPairs { get => mappingPairs.Values.ToList(); }

        HashSet<string> knownCandidates = new();

        HashSet<string> knownCluster = new();

        public MappingPair GetMappingPair(string candidateId, string clusterId)
        {
            mappingPairs.TryGetValue((candidateId, clusterId), out MappingPair mappingPair);
            return mappingPair;
        }

        public IEnumerable<MappingPair> GetAutomaticMappings()
        {
            List<MappingPair> recommendations = new List<MappingPair>();
            foreach (string candidateId in knownCandidates)
            {
                IEnumerable<MappingPair> recommendationsForCandidate = this.GetRecommendationForCandidate(candidateId);
                if(recommendationsForCandidate.Count() == 1)
                {
                    recommendations.AddRange(recommendationsForCandidate);
                }
            }

            return recommendations;
        }

        public IEnumerable<MappingPair> GetRecommendations()
        {
            List<MappingPair> recommendations = new List<MappingPair>();
            foreach (string candidateId in knownCandidates)
            {
                IEnumerable<MappingPair> recommendationsForCandidate = this.GetRecommendationForCandidate(candidateId);
                recommendations.AddRange(recommendationsForCandidate);
            }

            return recommendations;
        }

        public IEnumerable<MappingPair> GetMappingPairs()
        {
            return mappingPairs.Values;
        }

        public IEnumerable<MappingPair> GetRecommendationForCandidate(string candidateId)
        {
            List<MappingPair> candidatePairs = mappingPairs.Values.Where(mp => mp.CandidateID.Equals(candidateId)).ToList();
            if (!candidatePairs.Any())
            {
                return Enumerable.Empty<MappingPair>();
            }

            double avg = candidatePairs.Average(mp => mp.AttractionValue);
            double stdDev = Math.Sqrt(candidatePairs.Average(mp => Math.Pow(mp.AttractionValue - avg, 2)));

            var result = candidatePairs.Where(mp => mp.AttractionValue > avg + stdDev).ToList();
            return result.Any() ? result : candidatePairs.Where(mp => mp.AttractionValue > avg).ToList();
        }

        public IEnumerable<MappingPair> GetRecommendationForCluster(string clusterId)
        {
            var clusterPairs = mappingPairs.Values.Where(mp => mp.ClusterID.Equals(clusterId)).ToList();
            if (!clusterPairs.Any())
                return Enumerable.Empty<MappingPair>();

            double avg = clusterPairs.Average(mp => mp.AttractionValue);
            double stdDev = Math.Sqrt(clusterPairs.Average(mp => Math.Pow(mp.AttractionValue - avg, 2)));

            var result = clusterPairs.Where(mp => mp.AttractionValue > avg + stdDev).ToList();
            return result.Any() ? result : clusterPairs.Where(mp => mp.AttractionValue > avg).ToList();
        }

        public void RemoveCandidate(string candidateId)
        {
            var keysToRemove = mappingPairs.Keys.Where(k => k.candidateId == candidateId).ToList();
            foreach (var key in keysToRemove)
            {
                mappingPairs.Remove(key);
            }

            if (knownCluster.Contains(candidateId))
            {
                knownCluster.Remove(candidateId);
            }
        }

        public void RemoveCluster(string clusterId)
        {
            var keysToRemove = mappingPairs.Keys.Where(k => k.clusterId == clusterId).ToList();
            foreach (var key in keysToRemove)
            {
                mappingPairs.Remove(key);
            }

            if (knownCluster.Contains(clusterId))
            {
                knownCluster.Remove(clusterId);
            }
        }

        public void UpdateMappingPair(MappingPair mappingPair)
        {
            var key = (mappingPair.CandidateID, mappingPair.ClusterID);

            if (!knownCandidates.Contains(mappingPair.CandidateID))
            {
                knownCandidates.Add(mappingPair.CandidateID);
            }

            if (!knownCluster.Contains(mappingPair.ClusterID))
            {
                knownCluster.Add(mappingPair.ClusterID);
            }

            mappingPairs[key] = mappingPair;
        }

        public void Reset()
        {
            this.mappingPairs.Clear();
        }
    }
}
