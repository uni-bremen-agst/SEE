using Newtonsoft.Json;
using SEE.DataModel;
using SEE.DataModel.DG;
using System;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Objects of this class represent a pair of a candidate node and cluster node with an associated 
    /// attraction value.
    /// </summary>
    public class MappingPair : IComparable<MappingPair>
    {
        /// <summary>
        /// Attraction value of the mapping pair.
        /// 
        /// Is set to -1.0 if the mapping represents an 
        /// recorded unmapping.
        /// 
        /// </summary>
        private double? attractionValue;

        /// <summary>
        /// Attraction value of the mapping pair.
        /// Cannot be overriden once set.
        /// 
        /// Is set to -1.0 if the mapping represents an 
        /// recorded unmapping or the mapping of child nodes.
        /// 
        /// </summary>
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

        /// <summary>
        /// Candidate Node of the mapping pair.
        /// Values can be null if this object was deserialized from Json.
        /// </summary>
        [JsonIgnore]
        public Node Candidate { get; }

        /// <summary>
        /// Cluster Node of the mapping pair.
        /// Values can be null if this object was deserialized from Json.
        /// </summary>
        [JsonIgnore]
        public Node Cluster { get; }

        /// <summary>
        /// Cluster Node id of the mapping pair.
        /// </summary>
        private string clusterID;

        /// <summary>
        /// Candidate Node id of the mapping pair.
        /// </summary>
        private string candidateID;

        /// <summary>
        /// Change type determining if this mapping pair was mapped or unmapped during 
        /// the mappign process.
        /// 
        /// Used for statistical recording purposes.
        /// 
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ChangeType? ChangeType { get; set; }

        /// <summary>
        /// Datetime of the moment this MappingPair was mapped.
        /// 
        /// Used for statistical recording purposes.
        /// 
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ChosenAt { get; set; }

        /// <summary>
        /// Cluster Node id of the mapping pair.
        /// 
        /// Cannot be overriden.
        /// 
        /// </summary>
        public string ClusterID
        {
            get
            {
                return Cluster != null ? Cluster.ID : clusterID;
            }
            set
            {
                if (Cluster != null || clusterID != null)
                {
                    throw new Exception("Cannot override ClusterID");
                }
                clusterID = value;
            }
        }

        /// <summary>
        /// Candidate id of the mapping pair.
        /// 
        /// Cannot be overriden.
        /// 
        /// </summary>
        public string CandidateID
        {
            get
            {
                return Candidate != null ? Candidate.ID : candidateID;
            }
            set
            {
                if (Candidate != null || candidateID != null)
                {
                    throw new Exception("Cannot override CandidateID");
                }
                candidateID = value;
            }
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="MappingPair"/>.
        /// </summary>
        /// <param name="candidate">Candidate node of this mapping pair</param>
        /// <param name="cluster">Candidate node of this mapping pair</param>
        /// <param name="attractionValue">Attraction value between the two nodes</param>
        public MappingPair(Node candidate, Node cluster, double attractionValue)
        {
            this.Cluster = cluster;
            this.Candidate = candidate;
            this.attractionValue = attractionValue;
        }

        /// <summary>
        /// Comparing function of <see cref="IComparable"/>. A MappingPair 
        /// is compared by the attraction value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MappingPair other)
        {
            if (this == other)
            {
                return 0;
            }
            return this.AttractionValue.CompareTo(other.AttractionValue);
        }

        /// <summary>
        /// Function returning a short string description of this object.
        /// </summary>
        /// <returns>Description of this object</returns>
        public string ToShortString()
        {
            return $"{this.CandidateID} -{attractionValue}-> {this.ClusterID}";
        }

        /// <summary>
        /// Equals function returning true if the given object 
        /// is a MappingPair containing a equally cluster node and candidate node object.
        /// and an attraction value differing less than 
        /// the <see cref="Recommendations.ATTRACTION_VALUE_DELTA"/>
        /// </summary>
        /// <param name="obj">Given object</param>
        /// <returns>Return wether the given object is equals. Otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            MappingPair mappingPair = (MappingPair)obj;

            return this.ClusterID.Equals(mappingPair.ClusterID)
                && this.CandidateID.Equals(mappingPair.CandidateID)
                && Math.Abs(this.AttractionValue - mappingPair.AttractionValue) < Recommendations.ATTRACTION_VALUE_DELTA;
        }

        /// <summary>
        /// Generates an Hash based on the cluster id, candidate id and a truncated attraction value, 
        /// considering the attraction value delta.
        /// </summary>
        /// <returns>Returns the generated hash code.</returns>
        public override int GetHashCode()
        {
            // truncate value depending on the defined delta to erase decimal places
            double truncatedValue = Math.Truncate(AttractionValue / Recommendations.ATTRACTION_VALUE_DELTA) 
                                    * Recommendations.ATTRACTION_VALUE_DELTA;
            return HashCode.Combine(this.Cluster.ID, this.Candidate.ID, truncatedValue);
        }
    }
}
