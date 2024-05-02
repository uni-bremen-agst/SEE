using Accord.MachineLearning;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class RecommendedNodes
    {
        /// <summary>
        /// Currently the highest attraction value
        /// </summary>
        public double CurrentAttractionValue { get; private set; }

        // Data structures for all mapping pairs

        /// <summary>
        /// 
        /// </summary>
        Dictionary<string, MappingPair> mappingPairs = new Dictionary<string, MappingPair>();
        HashSet<string> knownClusters = new HashSet<string>();
        HashSet<string> knownCandidates = new HashSet<string>();

        // Data structures for recommendations

        /// <summary>
        /// 
        /// </summary>
        Dictionary<string, int> clusterInRecommendations = new Dictionary<string, int>();
        Dictionary<string, int> candidatesInRecommendations = new Dictionary<string, int>();
        Dictionary<string, MappingPair> recommendations = new Dictionary< string, MappingPair>();

        public Dictionary<string, MappingPair> Recommendations { get => new Dictionary<string, MappingPair>(recommendations); }

        public IEnumerable<MappingPair> MappingPairs { get => mappingPairs.Values.ToList(); }

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
        }

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
        }

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

        private void UpdateRecommendations()
        {
            recommendations.Clear();
            CurrentAttractionValue = double.MinValue;
            foreach (MappingPair mappingPair in mappingPairs.Values)
            {
                UpdateMappingPairInRecommendations(mappingPair);
            }
        }
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

        private void UpdateMappingPairInRecommendations(MappingPair mappingPair)
        {
            double difference = CurrentAttractionValue - mappingPair.AttractionValue;
            double delta = CandidateRecommendation.ATTRACTION_VALUE_DELTA;
            bool containedInRecommendations = recommendations.ContainsKey(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID));

            if (difference < (delta * -1) && mappingPair.AttractionValue > 0)
            {
                // Case new mappingPair has bigger attraction value 
                ResetRecommendations();
                this.AddIdInRecommendations(clusterInRecommendations, mappingPair.ClusterID);
                this.AddIdInRecommendations(candidatesInRecommendations, mappingPair.CandidateID);
                recommendations[CreateKey(mappingPair.CandidateID, mappingPair.ClusterID)] = mappingPair;
                CurrentAttractionValue = mappingPair.AttractionValue;
            } 
            else if(difference < delta)
            {
                // Case updated mappingPair has around the same attraction value 
                if(!containedInRecommendations && mappingPair.AttractionValue > 0)
                {
                    this.AddIdInRecommendations(clusterInRecommendations, mappingPair.ClusterID);
                    this.AddIdInRecommendations(candidatesInRecommendations, mappingPair.CandidateID);
                    recommendations.Add(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID), mappingPair);
                }
            } 
            else if(difference > delta)
            {
                if(containedInRecommendations)
                {
                    recommendations.Remove(CreateKey(mappingPair.CandidateID, mappingPair.ClusterID));
                    this.RemoveIdInRecommendations(clusterInRecommendations, mappingPair.ClusterID);
                    this.RemoveIdInRecommendations(candidatesInRecommendations, mappingPair.CandidateID);
                }
            }
        }

        private void AddIdInRecommendations(Dictionary<string, int> dic, string id)
        {
            if(dic.ContainsKey(id))
            {
                dic[id]++;
            } 
            else 
            {
                dic.Add(id, 1);
            }
        }

        private void RemoveIdInRecommendations(Dictionary<string, int> dic, string id)
        {
            if (dic.ContainsKey(id))
            {
                dic[id]--;

                if (dic[id] == 0)
                {
                    dic.Remove(id);
                }
            } 
            else
            {
                throw new Exception($"Could not decrement counter for id {id}. Id not found in recommendations.");
            }
        }

        private string CreateKey(string candidateId, string clusterId)
        {
            return candidateId + "#" + clusterId;
        }

        private void ResetRecommendations()
        {
            clusterInRecommendations.Clear();
            candidatesInRecommendations.Clear();
            recommendations.Clear();
            CurrentAttractionValue = 0;
        }

        public void Reset()
        {
            ResetRecommendations();
            mappingPairs.Clear();
            knownClusters.Clear();
            knownCandidates.Clear();
        }
    }
}
