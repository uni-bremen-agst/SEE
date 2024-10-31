using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class is an data object used to hold statistical results about Candidates.
    /// They are used to generate xml parts, which are add to the xml generated
    /// when saving an experiment result.
    /// </summary>
    public class CandidateStatistic
    {
        /// <summary>
        /// This constructor initializes a new instance of <see cref="CandidateStatistic"/>
        /// given a candidate id.
        /// </summary>
        /// <param name="candidateID">Given candidate id</param>
        /// <exception cref="ArgumentException">Throws if given candidate id is null</exception>
        public CandidateStatistic(string candidateID)
        {
            if (candidateID == null)
            {
                throw new ArgumentException("Cannot initialize candidate statistic result. Candidate ID is null.");
            }

            CandidateID = candidateID;
        }

        /// <summary>
        /// The last attraction value calculated for this candidate and a cluster, which the candidate 
        /// was mapped to.
        /// </summary>
        public double AttractionValue { get; set; } = -1;
        
        /// <summary>
        /// Candidate id this object refering to.
        /// </summary>
        public string CandidateID { get; set; }

        /// <summary>
        /// The id of the last cluster this candidate was mapped to.
        /// </summary>
        private string mappedClusterID = "Unknown";

        /// <summary>
        /// The id of the last cluster this candidate was mapped to.
        /// </summary>
        public string MappedClusterID
        {
            get
            {
                return mappedClusterID;
            }
            set
            {
                mappedClusterID = value != null ? value : "Unknown";
            }
        }

        /// <summary>
        /// The id of the cluster expected for this candidate regarding an oracle reflexion graph 
        /// </summary>
        private string expectedClusterID = "Unknown";

        /// <summary>
        /// The id of the cluster expected for this candidate regarding an oracle reflexion graph 
        /// </summary>
        public string ExpectedClusterID 
        { 
            get
            {
                return expectedClusterID;
            }
            set
            {
                expectedClusterID = value != null ? value : "Unknown";
            }
        } 

        /// <summary>
        /// A list of percentile ranks corresponding to each mapping step.
        /// </summary>
        private List<double> PercentileRanks { get; set; } = new List<double>();

        /// <summary>
        /// A list of percentile ranks corresponding to each mapping step which are above zero.
        /// </summary>
        public List<double> ValidPercentileRanks { get; set; } = new List<double>();

        /// <summary>
        /// All valid percentile ranks averaged
        /// </summary>
        public double AveragePercentileRank { get; set; }

        /// <summary>
        /// The last mapping step this candidate was mapped to a cluster.
        /// </summary>
        public int MappedAtMappingStep { get; set; } = -1;
        
        /// <summary>
        /// Is set to true if the candidate is mapped to its expected cluster after the experiment.
        /// </summary>
        public bool Hit { get; set; }

        /// <summary>
        /// Add a percentile rank to the list <see cref="PercentileRanks"/>
        /// </summary>
        /// <param name="percentileRank">Given percentile rank</param>
        public void AddPercentileRank(double percentileRank)
        {
            PercentileRanks.Add(percentileRank);
        }

        /// <summary>
        /// Calculate all values which are dependend on previously recorded data, 
        /// like average values.
        /// </summary>
        public void CalculateResults()
        {
            ValidPercentileRanks = PercentileRanks.Where(n => n >= 0).ToList();
            AveragePercentileRank = ValidPercentileRanks.Count > 0 ? ValidPercentileRanks.Average() : -1;
            AveragePercentileRank = Math.Round(AveragePercentileRank, 4);
        }

        /// <summary>
        /// Writes all data of this object to an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public XElement ToXElement()
        {
            string separator = string.Empty;
            StringBuilder percentileRanksStr = new StringBuilder();

            foreach (double rank in PercentileRanks)
            {
                percentileRanksStr.Append(separator + rank);
                separator = ";";
            }

            XElement candidateElement = new XElement("candidate", percentileRanksStr.ToString());
            XAttribute candidateID = new XAttribute("ID", this.CandidateID);
            XAttribute mappedTo = new XAttribute("MappedToCluster", this.MappedClusterID != null ? this.MappedClusterID : "Unknown");
            XAttribute expectedMappedTo = new XAttribute("ExpectedCluster", this.ExpectedClusterID != null ? this.ExpectedClusterID : "Unknown");
            XAttribute hit = new XAttribute("Hit", this.Hit);
            XAttribute mappedAtMeppingStep = new XAttribute("MappedAtStep", this.MappedAtMappingStep);
            XAttribute attractionValue = new XAttribute("AttractionValue", this.AttractionValue);
            XAttribute average = new XAttribute("averageRank", AveragePercentileRank);

            candidateElement.Add(candidateID);
            candidateElement.Add(mappedTo);
            candidateElement.Add(expectedMappedTo);
            candidateElement.Add(attractionValue);
            candidateElement.Add(hit);
            candidateElement.Add(mappedAtMeppingStep);
            candidateElement.Add(average);
            return candidateElement;
        }
    }
}
