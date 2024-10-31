using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class provides a filter to find recommendations based on 
    /// the maximum known attraction value within the matrix.
    /// </summary>
    public class MaxFilter : IRecommendationFilter
    {
        /// <summary>
        /// Currently the highest attraction value
        /// </summary>
        public double CurrentAttractionValue { get; private set; }

        // Data structures for all mapping pairs

        /// <summary>
        /// All mapping pairs currently add by the <see cref="ReflexionAnalysis.Recommendations"/> object.
        /// </summary>
        private Dictionary<string, MappingPair> mappingPairs = new Dictionary<string, MappingPair>();

        /// <summary>
        /// All known cluster ids within the mapping pairs
        /// </summary>
        private HashSet<string> knownClusters = new HashSet<string>();

        /// <summary>
        /// All known candidate ids  within the mapping pairs
        /// </summary>
        private HashSet<string> knownCandidates = new HashSet<string>();

        // Data structures for recommendations

        /// <summary>
        /// All clusters currently within the list of recommendations
        /// </summary>
        private Dictionary<string, int> clusterInRecommendations = new Dictionary<string, int>();

        /// <summary>
        /// All candidates currently within the list of recommendations
        /// </summary>
        private Dictionary<string, int> candidatesInRecommendations = new Dictionary<string, int>();

        /// <summary>
        /// Current recommendations
        /// </summary>
        private Dictionary<string, MappingPair> recommendations = new Dictionary< string, MappingPair>();

        /// <summary>
        /// Current recommendations
        /// </summary>
        public IEnumerable<MappingPair> Recommendations { get => new List<MappingPair>(recommendations.Values); }

        /// <summary>
        /// All mapping pairs currently add by the <see cref="ReflexionAnalysis.Recommendations"/> object.
        /// This property returns a clone.
        /// </summary>
        public IEnumerable<MappingPair> MappingPairs { get => mappingPairs.Values.ToList(); }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<MappingPair> GetAutomaticMappings()
        {
            return this.Recommendations;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MappingPair> GetRecommendations()
        {
            return this.Recommendations;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<MappingPair> GetMappingPairs()
        {
            return this.MappingPairs;
        }

        /// <summary>
        /// Returns the all recommended mapping pairs for a given cluster node id.
        /// </summary>
        /// <param name="clusterId">Given cluster node id.</param>
        /// <returns><see cref="IEnumerable"/> object containing the recommened mapping pairs</returns>
        public IEnumerable<MappingPair> GetRecommendationForCluster(string clusterId)
        {
            IList<MappingPair> recommendationsForCluster = new List<MappingPair>();
            foreach (string candidateId in candidatesInRecommendations.Keys)
            {
                string key = this.CreateKey(candidateId, clusterId);
                
                if(recommendations.TryGetValue(key, out MappingPair recommendation))
                {
                    recommendationsForCluster.Add(recommendation);
                }
            }
            return recommendationsForCluster;
        }

        /// <summary>
        /// Returns the all recommended mapping pairs for a given candidate node id.
        /// </summary>
        /// <param name="candidateId">Given canddiate node id.</param>
        /// <returns><see cref="IEnumerable"/> object containing the recommened mapping pairs</returns>
        public IEnumerable<MappingPair> GetRecommendationForCandidate(string candidateId)
        {
            IList<MappingPair> recommendationsForCandidate = new List<MappingPair>();
            foreach (string clusterId in clusterInRecommendations.Keys)
            {
                string key = this.CreateKey(candidateId, clusterId);

                if (recommendations.TryGetValue(key, out MappingPair recommendation))
                {
                    recommendationsForCandidate.Add(recommendation);
                }
            }
            return recommendationsForCandidate;
        }

        /// <summary>
        /// Removes a candidate id from the recommendations if it was mapped.
        /// </summary>
        /// <param name="candidateId">Given candidate id to remove</param>
        public void RemoveCandidate(string candidateId)
        {
            foreach (string clusterId in knownClusters)
            {
                string key = CreateKey(candidateId, clusterId);
                mappingPairs.Remove(key); 
            }

            foreach (string clusterId in clusterInRecommendations.Keys)
            {
                string key = CreateKey(candidateId, clusterId);
                if (recommendations.ContainsKey(key))
                {
                    recommendations.Remove(key);
                }
            }

            candidatesInRecommendations.Remove(candidateId);
            knownCandidates.Remove(candidateId);

            if (recommendations.Count == 0)
            {
                UpdateRecommendations();
            }
        }

        /// <summary>
        /// Removes a cluster id from the recommendations if it was mapped.
        /// </summary>
        /// <param name="candidateId">Given cluster id to remove</param>
        public void RemoveCluster(string clusterId)
        {
            foreach(string candidateId in knownCandidates)
            {
                string key = CreateKey(candidateId, clusterId);
                mappingPairs.Remove(key);
            }

            foreach(string candidateId in candidatesInRecommendations.Keys)
            {
                string key = CreateKey(candidateId, clusterId);
                if(recommendations.ContainsKey(key)) 
                {
                    recommendations.Remove(key);
                }
            }

            clusterInRecommendations.Remove(clusterId);
            knownClusters.Remove(clusterId);

            if (recommendations.Count == 0)
            {
                UpdateRecommendations();
            }
        }

        /// <summary>
        /// Returns the current mapping pair for given candidate and cluster node id.
        /// </summary>
        /// <param name="candidateId">Given candidate id</param>
        /// <param name="clusterId">Given cluster id</param>
        /// <returns>The corresponding mapping pair. Or null if not 
        /// matching mapping pair can be found.</returns>
        public MappingPair GetMappingPair(string candidateId, string clusterId)
        {
            string key = CreateKey(candidateId, clusterId); 

            if (mappingPairs.ContainsKey(key))
            {
                return this.mappingPairs[key]; 
            } 
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Updates a given mapping pair within this object.
        /// </summary>
        /// <param name="mappingPair">Given mapping pair</param>
        public void UpdateMappingPair(MappingPair mappingPair)
        {
            mappingPairs[CreateKey(mappingPair.CandidateID, mappingPair.ClusterID)] = mappingPair;

            if(!knownClusters.Contains(mappingPair.ClusterID))
            {
                knownClusters.Add(mappingPair.ClusterID);
            }

            if (!knownCandidates.Contains(mappingPair.CandidateID))
            {
                knownCandidates.Add(mappingPair.CandidateID);
            }

            UpdateMappingPairInRecommendations(mappingPair);

            if(recommendations.Count == 0)
            {
                UpdateRecommendations();
            }
        }

        /// <summary>
        /// Updates the datastructures regarding the attraction 
        /// value of the given mapping pair. 
        /// 
        /// Resets the recommendations if the updated mapping pair has an 
        /// bigger attraction value than the current one and adds it 
        /// to the new recommendations.
        /// 
        /// Adds the mapping pair to the current recommendations if the
        /// attraction value is near the current highest attraction value.
        /// 
        /// Removes the mapping pair if it is contained within the recommendations 
        /// but has a smaller attraction value that currently highest attraction value
        /// 
        /// </summary>
        /// <param name="mappingPair">Given mapping pair.</param>
        private void UpdateMappingPairInRecommendations(MappingPair mappingPair)
        {
            double difference = CurrentAttractionValue - mappingPair.AttractionValue;
            double delta = ReflexionAnalysis.Recommendations.ATTRACTION_VALUE_DELTA;
            bool containedInRecommendations = recommendations.ContainsKey(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID));

            if (difference < (delta * -1) && mappingPair.AttractionValue > 0)
            {
                // Case new mappingPair has bigger attraction value 
                ResetRecommendations();
                this.IncrementCountInDic(clusterInRecommendations, mappingPair.ClusterID);
                this.IncrementCountInDic(candidatesInRecommendations, mappingPair.CandidateID);
                recommendations[CreateKey(mappingPair.CandidateID, mappingPair.ClusterID)] = mappingPair;
                CurrentAttractionValue = mappingPair.AttractionValue;
            } 
            else if(difference < delta)
            {
                // Case updated mappingPair has around the same attraction value 
                if(!containedInRecommendations && mappingPair.AttractionValue > 0)
                {
                    this.IncrementCountInDic(clusterInRecommendations, mappingPair.ClusterID);
                    this.IncrementCountInDic(candidatesInRecommendations, mappingPair.CandidateID);
                    recommendations.Add(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID), mappingPair);
                }
            } 
            else if(difference > delta)
            {
                if(containedInRecommendations)
                {
                    recommendations.Remove(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID));
                    this.DecrementCountInDic(clusterInRecommendations, mappingPair.ClusterID);
                    this.DecrementCountInDic(candidatesInRecommendations, mappingPair.CandidateID);
                }
            }
        }

        /// <summary>
        /// Updates the recommendation data structures for all known mapping pairs.
        /// </summary>
        private void UpdateRecommendations()
        {
            recommendations.Clear();
            CurrentAttractionValue = double.MinValue;
            foreach (MappingPair mappingPair in mappingPairs.Values)
            {
                UpdateMappingPairInRecommendations(mappingPair);
            }
        }

        /// <summary>
        /// Increments an value in a dictionary for a given key.
        /// Adds the key with the starting number 1 if the key was 
        /// not contained.
        /// </summary>
        /// <param name="dic">Given dictionary</param>
        /// <param name="key">Given key</param>
        private void IncrementCountInDic(Dictionary<string, int> dic, string key)
        {
            if(dic.ContainsKey(key))
            {
                dic[key]++;
            } 
            else 
            {
                dic.Add(key, 1);
            }
        }

        /// <summary>
        /// Decrements an value in a dictionary for a given key.
        /// If the values reaches Zero the key is removed from the dictionary.
        /// </summary>
        /// <param name="dic">Given dictionary</param>
        /// <param name="key">Given key</param>
        private void DecrementCountInDic(Dictionary<string, int> dic, string key)
        {
            if (dic.ContainsKey(key))
            {
                dic[key]--;

                if (dic[key] == 0)
                {
                    dic.Remove(key);
                }
            } 
            else
            {
                throw new Exception($"Could not decrement counter for key {key}. Id not found in recommendations.");
            }
        }

        /// <summary>
        /// Creates the key to save a mapping pair based
        /// on the given candidate and cluster node ids 
        /// </summary>
        /// <param name="candidateId">Given candidate id</param>
        /// <param name="clusterId">Given cluster id</param>
        /// <returns></returns>
        private string CreateKey(string candidateId, string clusterId)
        {
            return candidateId + "#" + clusterId;
        }

        /// <summary>
        /// Resets the datastructures for recommendations.
        /// </summary>
        private void ResetRecommendations()
        {
            clusterInRecommendations.Clear();
            candidatesInRecommendations.Clear();
            recommendations.Clear();
            CurrentAttractionValue = 0;
        }

        /// <summary>
        /// Resets the recommendations and forget about all known mapping pairs.
        /// </summary>
        public void Reset()
        {
            ResetRecommendations();
            mappingPairs.Clear();
            knownClusters.Clear();
            knownCandidates.Clear();
        }
    }
}
