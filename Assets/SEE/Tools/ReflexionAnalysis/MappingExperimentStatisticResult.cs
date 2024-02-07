using MathNet.Numerics.Financial;
using SEE.DataModel.DG;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class MappingExperimentStatisticResult
    {
        public int Seed { get; set; }

        private MappingExperimentConfig config;

        public int Hits { get; private set; }
        public int Fails { get; private set; }
        public int InitiallyMapped { get; private set; }
        public int Chosen { get; private set; }
        public double AveragePercentileRankGlobally { get; private set; }

        private bool calculatedResults;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, CandidateStatisticResult> results;

        /// <summary>
        /// <see cref="CandidateStatisticResult"/> objects are add to this list 
        /// at the moment <see cref="FinishCandidateStatisticResult"/> for a corresponding object. 
        /// So the results can be ordered by the moment they were mapped.
        /// </summary>
        private List<CandidateStatisticResult> resultsOrdered;

        public MappingExperimentStatisticResult()
        {
            results = new Dictionary<string, CandidateStatisticResult>();
            resultsOrdered = new List<CandidateStatisticResult>(); 
        }

        public void Clear()
        {
            this.results.Clear();
            this.resultsOrdered.Clear();
        }

        public void AddConfigInformation(MappingExperimentConfig config)
        {
            this.config = config;
        }

        public void AddCandidateStatisticResult(Node candidate, CandidateRecommendation recommendation)
        {
            CandidateStatisticResult statisticResult = new CandidateStatisticResult(candidate.ID);

            if (!results.ContainsKey(candidate.ID))
            {
                results.Add(candidate.ID, statisticResult);
            } 
            else
            {
                throw new Exception($"Candidate statistic result for the ID {candidate.ID} was already initialized.");
            }

            Node mapsTo = recommendation.ReflexionGraph.MapsTo(candidate);

            if (mapsTo != null)
            {
                Node expectedCluster = recommendation.OracleGraph.MapsTo(candidate);
                statisticResult.ExpectedClusterID = expectedCluster.ID;
                statisticResult.MappedClusterID = mapsTo.ID;
                statisticResult.Hit = recommendation.IsHit(statisticResult.CandidateID, statisticResult.MappedClusterID);
                FinishCandidateStatisticResult(statisticResult.CandidateID);
            } 
        }

        public void FinishCandidateStatisticResult(string candidateID)
        {
            if(!results.ContainsKey(candidateID))
            {
                throw new Exception($"Cannot finish candidate statistic result for ID {candidateID}. ID is unknown.");
            }
            this.resultsOrdered.Add(this.results[candidateID]);
            this.results.Remove(candidateID);
        }

        public bool ContainsCandidateStatisticResult(string candidateID)
        {
            return this.results.ContainsKey(candidateID);
        }

        public CandidateStatisticResult GetCandidateStatisticResult(string candidateID)
        {
            if (!results.ContainsKey(candidateID))
            {
                throw new Exception($"No candidate statistic result found for ID {candidateID}");
            }
            return results[candidateID];
        }

        public XElement CreateXml()
        {
            if(!calculatedResults)
            {
                throw new Exception("Cannot generate XML before the results were caculated.");
            }

            XElement mappingStatisticXml = new XElement("mappingStatistic",
                new XAttribute(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"),
                new XElement("hits", Hits),
                new XElement("fails", Fails),
                new XElement("AveragePercentileRankGlobally", AveragePercentileRankGlobally),
                new XElement("initiallyMapped", InitiallyMapped),
                new XElement("chosen", Chosen),
                new XElement("candidateType", config.AttractFunctionConfig.CandidateType),
                new XElement("clusterType", config.AttractFunctionConfig.ClusterType),
                new XElement("masterSeed", config.MasterSeed));

            XElement CandidateResult = new XElement("candidateStatisticResults");

            resultsOrdered.ForEach(r => CandidateResult.Add(r.ToXElement()));

            mappingStatisticXml.Add(CandidateResult);

            return mappingStatisticXml;
        }

        public void CalculateResults()
        {
            resultsOrdered.ForEach(r => r.CalculateResults());
           
            Hits = resultsOrdered.Where(r => r.Hit && r.MappedAtMappingStep >= 0).Count();
            Fails = resultsOrdered.Where(r => !r.Hit && r.MappedAtMappingStep >= 0).Count();
            InitiallyMapped = resultsOrdered.Where((r) => r.MappedAtMappingStep < 0).Count();
            Chosen = resultsOrdered.Where((r) => r.Chosen == true).Count();

            AveragePercentileRankGlobally = resultsOrdered.Where(r => r.AveragePercentileRank >= 0)
                                              .Select(r => r.AveragePercentileRank).Average();

            calculatedResults = true;
        }
    }
}
