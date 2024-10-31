using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This recommendation filter can be used to calculate Recommendations based 
    /// on the HugMe-Method
    /// </summary>
    public class HugMeFilter : IRecommendationFilter
    {
        /// <summary>
        /// Attraction value matrix represented by a dictionary of mapping pairs.
        /// </summary>
        private Dictionary<(string candidateId, string clusterId), MappingPair> mappingPairs = new();

        /// <summary>
        /// Returns mapping pairs that represent that attraction value matrix.
        /// </summary>
        public IEnumerable<MappingPair> MappingPairs { get => mappingPairs.Values.ToList(); }

        /// <summary>
        /// Candidates ids known by this filter
        /// </summary>
        HashSet<string> knownCandidates = new();

        /// <summary>
        /// Cluster ids known by this filter
        /// </summary>
        HashSet<string> knownCluster = new();

        /// <summary>
        /// Returns a specified mapping pair of the matrix
        /// </summary>
        /// <param name="candidateId">given candidate id</param>
        /// <param name="clusterId">given cluster id</param>
        /// <returns></returns>
        public MappingPair GetMappingPair(string candidateId, string clusterId)
        {
            mappingPairs.TryGetValue((candidateId, clusterId), out MappingPair mappingPair);
            return mappingPair;
        }

        /// <summary>
        /// Returns the recommended mapping pairs. These are all mapping pairs
        /// which are contained within the HugMe-set of each candidate, where
        /// the set only contains one cluster.
        /// </summary>
        /// <returns>A list of mapping pairs contained 
        /// by all HugMeSets with the size of one.</returns>
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

        /// <summary>
        /// Returns the recommended mapping pairs. These are all mapping pairs
        /// which are contained within the HugMe set of each candidate.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Returns all mapping pairs contained by this filter.
        /// </summary>
        /// <returns>Returns contained mapping pairs</returns>
        public IEnumerable<MappingPair> GetMappingPairs()
        {
            return mappingPairs.Values;
        }

        /// <summary>
        /// Returns the HugMe-Set represented by mapping pairs 
        /// for a given candidate
        /// </summary>
        /// <param name="candidateId">given candidate id</param>
        /// <returns>HugMe-Set of candidate represented as mapping pairs</returns>
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

        /// <summary>
        /// Returns the HugMe-Set represented by mapping pairs 
        /// for a given cluster
        /// </summary>
        /// <param name="candidateId">given cluster id</param>
        /// <returns>HugMe-Set of candidate represented as mapping pairs</returns>
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

        /// <summary>
        /// Removes a given candidate from the attraction matrix.
        /// All mapping pairs associated with the given candidate are forgotten.
        /// </summary>
        /// <param name="candidateId">given candidate id</param>
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

        /// <summary>
        /// Removes a given cluster from the attraction matrix
        /// All mapping pairs associated with the given cluster are forgotten.
        /// </summary>
        /// <param name="clusterId">given cluster id</param>
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

        /// <summary>
        /// Update and adds attraction value of a give mapping pair.
        /// </summary>
        /// <param name="mappingPair">given mapping pair</param>
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

        /// <summary>
        /// Resets the attraction matrix. 
        /// After this call no mapping pairs are contained by this filter.
        /// </summary>
        public void Reset()
        {
            this.mappingPairs.Clear();
        }
    }
}
