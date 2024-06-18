using MathNet.Numerics.Financial;
using SEE.DataModel.DG;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class MappingExperimentResult
    {
        public int Seed { get; set; }

        private RecommendationSettings config;

        public double Hits { get; private set; }
        public double Fails { get; private set; }
        public double InitiallyMapped { get; private set; }
        public double LeftOver { get; private set; }
        public int Chosen { get; private set; }
        public double AveragePercentileRankGlobally { get; private set; }

        private bool calculatedResults;

        public int FinishedResultsCount
        {
            get { return this.resultsOrdered.Count; }
        }

        public int ActiveResultsCount
        {
            get { return this.results.Count; }
        }

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

        public MappingExperimentResult()
        {
            results = new Dictionary<string, CandidateStatisticResult>();
            resultsOrdered = new List<CandidateStatisticResult>(); 
        }

        //public void Clear()
        //{
        //    this.results.Clear();
        //    this.resultsOrdered.Clear();
        //}

        public void AddConfigInformation(RecommendationSettings config)
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

            Node expectedCluster = recommendation.OracleGraph.MapsTo(candidate);
            statisticResult.ExpectedClusterID = expectedCluster?.ID;

            if (mapsTo != null)
            {
                statisticResult.MappedClusterID = mapsTo.ID;
                statisticResult.Hit = recommendation.IsHit(statisticResult.CandidateID, statisticResult.MappedClusterID);
                FinishCandidateStatisticResult(statisticResult.CandidateID);
            }
        }

        public void FinishCandidateStatisticResult(string candidateID)
        {
            if (!results.ContainsKey(candidateID))
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
                new XElement("leftOver", LeftOver),
                new XElement("chosen", Chosen),
                new XElement("candidateType", config.AttractFunctionConfig.CandidateType),
                new XElement("clusterType", config.AttractFunctionConfig.ClusterType),
                new XElement("masterSeed", config.MasterSeed));

            mappingStatisticXml.Add(this.config.AttractFunctionConfig.ToXElement());

            XElement CandidateResult = new XElement("candidateStatisticResults");

            UnityEngine.Debug.Log($"Generating xml for {resultsOrdered.Count} finished results.");
            resultsOrdered.ForEach(r => CandidateResult.Add(r.ToXElement()));

            mappingStatisticXml.Add(CandidateResult);

            return mappingStatisticXml;
        }

        public void CalculateResults()
        {
            calculatedResults = true;

            if(resultsOrdered.Count == 0)
            {
                return;
            }

            resultsOrdered.ForEach(r => r.CalculateResults());         
            Hits = resultsOrdered.Where(r => r.Hit && r.MappedAtMappingStep >= 0).Count();
            Fails = resultsOrdered.Where(r => !r.Hit && r.MappedAtMappingStep >= 0).Count();
            InitiallyMapped = resultsOrdered.Where((r) => r.MappedAtMappingStep == -1).Count();
            LeftOver = resultsOrdered.Where((r) => r.MappedAtMappingStep == -2).Count();
            Chosen = resultsOrdered.Where((r) => r.Chosen == true).Count();

            IEnumerable<double> validPercentileRankValues = resultsOrdered.Where(r => r.AveragePercentileRank >= 0)
                                                                         .Select(r => r.AveragePercentileRank);

            AveragePercentileRankGlobally = validPercentileRankValues.Count() > 0 ? validPercentileRankValues.Average() : -1; 
        }

        public static MappingExperimentResult AverageResults(IEnumerable<MappingExperimentResult> results, 
                                                             RecommendationSettings config)
        {
            MappingExperimentResult averageResult = new MappingExperimentResult();

            averageResult.AveragePercentileRankGlobally = results.Select(r => r.AveragePercentileRankGlobally).Average();
            averageResult.Hits = results.Select(r => r.Hits).Average();
            averageResult.Fails = results.Select(r => r.Fails).Average();
            averageResult.InitiallyMapped = results.Select(r => r.InitiallyMapped).Average();
            averageResult.LeftOver = results.Select(r => r.LeftOver).Average();

            averageResult.AddConfigInformation(config);
            averageResult.CalculateResults();
            return averageResult;
        }

        public void FinishCandidateStatisticResults()
        {
            IEnumerable<string> candidateIds = results.Keys.ToList();
            foreach (string candidateID in candidateIds)
            {
                if (results[candidateID] != null)
                {
                    results[candidateID].MappedAtMappingStep = -2;
                    this.FinishCandidateStatisticResult(candidateID);
                }
            }
        }
    }
}