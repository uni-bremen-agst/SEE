using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateStatisticResult
    {
        public CandidateStatisticResult(string candidateID)
        {
            if (candidateID == null)
            {
                throw new Exception("Cannot initialize candidate statistic result. Candidate ID is null.");
            }

            CandidateID = candidateID;
        }

        public double AttractionValue { get; set; } = -1;
        public string CandidateID { get; set; }

        private string mappedClusterID = "Unknown";

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

        private string expectedClusterID = "Unknown";

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
        private List<double> PercentileRanks { get; set; } = new List<double>();
        public List<double> ValidPercentileRanks { get; set; } = new List<double>();
        public double AveragePercentileRank { get; set; }
        public int MappedAtMappingStep { get; set; } = -1;
        public bool Hit { get; set; }
        public bool? Chosen { get; set; }

        public void AddPercentileRank(double percentileRank)
        {
            PercentileRanks.Add(percentileRank);
        }

        public void CalculateResults()
        {
            ValidPercentileRanks = PercentileRanks.Where(n => n >= 0).ToList();
            AveragePercentileRank = ValidPercentileRanks.Count > 0 ? ValidPercentileRanks.Average() : -1;
            AveragePercentileRank = Math.Round(AveragePercentileRank, 4);
        }

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
            XAttribute chosen = new XAttribute("Chosen", this.Chosen != null ? this.Chosen : "Null");
            XAttribute mappedAtMeppingStep = new XAttribute("MappedAtStep", this.MappedAtMappingStep);
            XAttribute attractionValue = new XAttribute("AttractionValue", this.AttractionValue);
            XAttribute average = new XAttribute("averageRank", AveragePercentileRank);

            candidateElement.Add(candidateID);
            candidateElement.Add(mappedTo);
            candidateElement.Add(expectedMappedTo);
            candidateElement.Add(attractionValue);
            candidateElement.Add(hit);
            candidateElement.Add(chosen);
            candidateElement.Add(mappedAtMeppingStep);
            candidateElement.Add(average);
            return candidateElement;
        }
    }
}
