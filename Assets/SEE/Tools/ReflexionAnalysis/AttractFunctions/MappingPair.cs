using Newtonsoft.Json;
using SEE.DataModel;
using SEE.DataModel.DG;
using System;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class MappingPair : IComparable<MappingPair>
    {
        private double? attractionValue;

        public double AttractionValue
        {
            get
            {
                return attractionValue ?? -1.0;
            }
            set
            {
                if (attractionValue == null)
                {
                    attractionValue = value;
                }
                else
                {
                    throw new Exception("Cannot override Attractionvalue.");
                }
            }
        }

        [JsonIgnore]
        public Node Candidate { get; }

        [JsonIgnore]
        public Node Cluster { get; }

        private string clusterID;

        private string candidateID;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ChangeType? ChangeType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ChosenAt { get; set; }

        public string ClusterID
        {
            get
            {
                return Cluster != null ? Cluster.ID : clusterID;
            }
            set
            {
                if (Cluster != null || clusterID != null) throw new Exception("Cannot override ClusterID");
                clusterID = value;
            }
        }

        public string CandidateID
        {
            get
            {
                return Candidate != null ? Candidate.ID : candidateID;
            }
            set
            {
                if (Candidate != null || candidateID != null) throw new Exception("Cannot override CandidateID");
                candidateID = value;
            }
        }

        public MappingPair(Node candidate, Node cluster, double attractionValue)
        {
            this.Cluster = cluster;
            this.Candidate = candidate;
            this.attractionValue = attractionValue;
        }

        public int CompareTo(MappingPair other)
        {
            if (this == other) return 0;
            return this.AttractionValue.CompareTo(other.AttractionValue);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            MappingPair mappingPair = (MappingPair)obj;

            return this.Cluster.Equals(mappingPair.Cluster)
                && this.Candidate.Equals(mappingPair.Candidate)
                && Math.Abs(this.AttractionValue - mappingPair.AttractionValue) < CandidateRecommendation.ATTRACTION_VALUE_DELTA;
        }

        public override int GetHashCode()
        {
            // truncate value depending on the defined delta to erase decimal places
            double truncatedValue = Math.Truncate(AttractionValue / CandidateRecommendation.ATTRACTION_VALUE_DELTA) 
                                    * CandidateRecommendation.ATTRACTION_VALUE_DELTA;
            return HashCode.Combine(this.Cluster.ID, this.Candidate.ID, truncatedValue);
        }
    }
}
