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
            foreach (string cluster in knownClusters)
            {
                string key = candidateId + cluster;
                mappingPairs.Remove(key); 
            }

            foreach (string cluster in clusterInRecommendations.Keys)
            {
                string key = candidateId + cluster;
                if(recommendations.ContainsKey(key))
                {
                    recommendations.Remove(key);
                }
            }

            candidatesInRecommendations.Remove(candidateId);
            knownCandidates.Remove(candidateId);
        }

        public void RemoveCluster(string clusterId)
        {
            // TODO:
            throw new NotImplementedException("Currently not implemented.");
        }

        public MappingPair GetMappingPair(string candidateId, string clusterId)
        {
            string key = candidateId + clusterId;

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
            mappingPairs[mappingPair.CandidateID + mappingPair.ClusterID] = mappingPair;

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
            bool containedInRecommendations = recommendations.ContainsKey(mappingPair.CandidateID + mappingPair.ClusterID);

            if (difference < (delta * -1) && mappingPair.AttractionValue > 0)
            {
                // Case new mappingPair has bigger attraction value 
                ResetRecommendations();
                this.AddIdInRecommendations(clusterInRecommendations, mappingPair.ClusterID);
                this.AddIdInRecommendations(candidatesInRecommendations, mappingPair.CandidateID);
                recommendations[mappingPair.CandidateID + mappingPair.ClusterID] = mappingPair;
                CurrentAttractionValue = mappingPair.AttractionValue;
            } 
            else if(difference < delta)
            {
                // Case updated mappingPair has around the same attraction value 
                if(!containedInRecommendations && mappingPair.AttractionValue > 0)
                {
                    this.AddIdInRecommendations(clusterInRecommendations, mappingPair.ClusterID);
                    this.AddIdInRecommendations(candidatesInRecommendations, mappingPair.CandidateID);
                    recommendations.Add(mappingPair.CandidateID + mappingPair.ClusterID, mappingPair);
                }
            } 
            else if(difference > delta)
            {
                if(containedInRecommendations)
                {
                    recommendations.Remove(mappingPair.CandidateID + mappingPair.ClusterID);
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
