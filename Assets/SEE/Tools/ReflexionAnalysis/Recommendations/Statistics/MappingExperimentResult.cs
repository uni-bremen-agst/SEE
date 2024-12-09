using MathNet.Numerics.Statistics;
using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class is an data object used to hold statistical results about an mapping experiment.
    /// Additional it is used to generate an xml to save an experiment result.
    /// 
    /// This class can reference to one specific run during the experiment, but also 
    /// to the averaged results.
    /// 
    /// </summary>
    public class MappingExperimentResult
    {
        /// <summary>
        /// Master seed which is used to derive all randomness during an experiment
        /// </summary>
        public int MasterSeed { get; set; }

        /// <summary>
        /// Seed used during a specific experiment run
        /// </summary>
        public int CurrentSeed { get; set; }

        /// <summary>
        /// Settings used to configure the mapping experiment and the attract function used.
        /// </summary>
        private RecommendationSettings config;

        /// <summary>
        /// Number of candidates considered for automated mapping during the run of an experiment. 
        /// </summary>
        private double CandidatesConsidered { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private double CandidatesTotal { get; set; }
        
        /// <summary>
        /// Hit rate refering to correctly mapped candidates per all considered candidates 
        /// </summary>
        private double HitRateAll { get; set; }

        /// <summary>
        /// Standard deviation of Hit rate refering to correctly mapped candidates per all considered candidates 
        /// </summary>
        private double HitRateAllStd { get; set; } = -1.0;

        /// <summary>
        /// Hit rate refering to correctly mapped candidates per actually mapped candidates 
        /// </summary>
        private double HitRateMapped { get; set; } 

        /// <summary>
        /// Standard deviation of Hit rate refering to correctly mapped candidates per actually mapped candidates 
        /// </summary>
        private double HitRateMappedStd { get; set; } = -1.0;

        /// <summary>
        /// Mapping rate refering to the number of mapped candidates per all considered candidates 
        /// </summary>

        private double MappingRate { get; set; }

        /// <summary>
        /// Standard deviation of Mapping rate refering to the number of mapped candidates per all considered candidates 
        /// </summary>

        private double MappingRateStd { get; set; } = -1.0;

        /// <summary>
        /// Total hits within the experiment run.
        /// 
        /// If this result was averaged this number is the cumulation of 
        /// the total hits of all runs.
        /// </summary>
        public double TotalHits { get; private set; }

        /// <summary>
        /// Total fails within the experiment run.
        /// 
        /// If this result was averaged this number is the cumulation of 
        /// the total fails of all runs.
        /// </summary>
        public double TotalFails { get; private set; }

        /// <summary>
        /// Number of initially mapped nodes, which would have been candidates, but where mapped from the start. 
        /// </summary>
        public double InitiallyMapped { get; private set; }

        /// <summary>
        /// Number of initially mapped nodes, which would have been candidates, but where mapped from the start. 
        /// </summary>
        public double CandidatesLeftOver { get; private set; }

        /// <summary>
        /// Number of runs this experiment were iterated
        /// </summary>
        public int Iterations { get; set; } = 1;

        /// <summary>
        /// Average of all average percentile ranks from each run
        /// </summary>
        public double AveragePercentileRankGlobally { get; private set; }
        
        // TODO: Doc
        public double Precision { get; private set; }
        public double Recall { get; private set; }
        public double FScore { get; private set; }
        public double PrecisionStd { get; private set; }
        public double RecallStd { get; private set; }
        
        public double FScoreStd { get; private set; }

        /// <summary>
        /// If this is set to true, the results were already calculated.
        /// </summary>
        private bool calculatedResults;

        /// <summary>
        /// Dictionary holding the active <see cref="CandidateStatistic"/> result objects.
        /// </summary>
        private Dictionary<string, CandidateStatistic> results;

        /// <summary>
        /// List holding the finished <see cref="CandidateStatistic"/> result objects.
        /// <see cref="CandidateStatistic"/> objects are add to this list 
        /// at the moment <see cref="FinishCandidateStatisticResult"/> is called 
        /// for a corresponding object. So the results can be ordered by the moment they were mapped.
        /// </summary>
        private List<CandidateStatistic> resultsOrdered;


        /// <summary>
        /// This constructor initializes a new instance of <see cref="MappingExperimentResult"/>.
        /// </summary>
        public MappingExperimentResult()
        {
            results = new Dictionary<string, CandidateStatistic>();
            resultsOrdered = new List<CandidateStatistic>(); 
        }

        /// <summary>
        /// Add used config this result objects to copy configuration parameters to
        /// write them to the output file.
        /// </summary>
        /// <param name="config"></param>
        public void AddConfigInformation(RecommendationSettings config)
        {
            this.config = config;
        }

        /// <summary>
        /// Adds a <see cref="CandidateStatistic"/> object for a give candidate node to this object.
        /// The add object is considered active after the call.
        /// 
        /// But if a given candidate node is already mapped during this call, it will 
        /// be considered as initially mapped and will be finished directly.
        /// 
        /// </summary>
        /// <param name="candidate">Given candidate node</param>
        /// <param name="recommendation">recommendation object associated with the given candidate</param>
        /// <exception cref="Exception">Throws if a <see cref="CandidateStatistic"/> object 
        /// was already initialized for the given candidate</exception>
        public void AddCandidateStatisticResult(Node candidate, Recommendations recommendation)
        {
            CandidateStatistic statisticResult = new CandidateStatistic(candidate.ID);

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

        /// <summary>
        /// Finishes the <see cref="CandidateStatistic"/> for a given 
        /// node id. The <see cref="CandidateStatistic"/> object is 
        /// no longer active and is add to the results.
        /// </summary>
        /// <param name="candidateID">Given node id</param>
        /// <exception cref="Exception">Throws if no statistic object could be found for the given id.</exception>
        public void FinishCandidateStatisticResult(string candidateID)
        {
            if (!results.ContainsKey(candidateID))
            {
                throw new Exception($"Cannot finish candidate statistic result for ID {candidateID}. ID is unknown.");
            }
            this.resultsOrdered.Add(this.results[candidateID]);
            this.results.Remove(candidateID);
        }

        /// <summary>
        /// Finishes all remaining active <see cref="CandidateStatistic"/> objects.
        /// 
        /// Sets the <see cref="CandidateStatistic.MappedAtMappingStep"/> to 
        /// <see cref="int.MaxValue"/> for unmapped candidates. 
        /// </summary>
        public void FinishUnmappedCandidates()
        {
            IEnumerable<string> candidateIds = results.Keys.ToList();
            foreach (string candidateID in candidateIds)
            {
                if (results[candidateID] != null)
                {
                    results[candidateID].MappedAtMappingStep = int.MaxValue;
                    this.FinishCandidateStatisticResult(candidateID);
                }
            }
        }

        /// <summary>
        /// Finishes all remaining active <see cref="CandidateStatistic"/> objects.
        /// 
        /// Sets the <see cref="CandidateStatistic.MappedAtMappingStep"/> to 
        /// <see cref="int.MaxValue"/> for unmapped candidates.
        /// <param name="idQueue">stack containing the candidate ids that were mapped last to 
        /// maintain the mapping order within the result file.</param>
        /// </summary>
        public void FinishMappedCandidates(List<string> idQueue)
        {
            int mappingStep = 0;
            while (idQueue.Count > 0)
            {
                string candidateID = idQueue[idQueue.Count-1];
                idQueue.RemoveAt(idQueue.Count-1);
                if (results.ContainsKey(candidateID))
                {
                    // TODO: find a better way mark the value for unmapped nodes.
                    // results[candidateID].MappedAtMappingStep = int.MaxValue;
                    results[candidateID].MappedAtMappingStep = mappingStep;
                    this.FinishCandidateStatisticResult(candidateID);
                    mappingStep++;
                }
            }
        }

        /// <summary>
        /// Returns for a given node id if an associated <see cref="CandidateStatistic"/> 
        /// object is active.
        /// </summary>
        /// <param name="candidateID"></param>
        /// <returns>True if the statistic object is active. Otherwise false.</returns>
        public bool ContainsCandidateStatisticResult(string candidateID)
        {
            return this.results.ContainsKey(candidateID);
        }

        /// <summary>
        /// Returns an active CandidateStatistic object for 
        /// a given node id.
        /// </summary>
        /// <param name="candidateID">Given node id</param>
        /// <returns>The refering statistic object</returns>
        /// <exception cref="Exception">Throws if no active candidate statistic object exists for the given id.</exception>
        public CandidateStatistic GetCandidateStatisticResult(string candidateID)
        {
            if (!results.ContainsKey(candidateID))
            {
                throw new Exception($"No candidate statistic result found for ID {candidateID}");
            }
            return results[candidateID];
        }

        /// <summary>
        /// Writes all data of this object to an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public XElement CreateXml()
        {
            if(!calculatedResults)
            {
                throw new Exception("Cannot generate XML before the results were caculated.");
            }

            XElement mappingStatisticXml = new XElement("MappingStatistic",
                new XAttribute(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"),
                new XElement("RootSeed", MasterSeed),
                new XElement("CurrentSeed", CurrentSeed),
                new XElement("Iterations", Iterations),
                new XElement("CandidatesTotal", CandidatesTotal),
                new XElement("CandidatesConsidered", CandidatesConsidered),
                new XElement("InitiallyMapped", InitiallyMapped),
                new XElement("LeftOver", CandidatesLeftOver),
                new XElement("MappingRate", MappingRate),
                new XElement("MappingRateStd", MappingRateStd),
                new XElement("HitRateMapped", HitRateMapped),
                new XElement("HitRateMappedStd", HitRateMappedStd),
                new XElement("HitRateAll", HitRateAll),
                new XElement("HitRateAllStd", HitRateAllStd),
                new XElement("Precision", Precision),
                new XElement("PrecisionStd", PrecisionStd),
                new XElement("Recall", Recall),
                new XElement("RecallStd", RecallStd),
                new XElement("FScore", FScore),
                new XElement("FScoreStd", FScoreStd),
                new XElement("TotalHits", TotalHits),
                new XElement("TotalFails", TotalFails),
                new XElement("AveragePercentileRankGlobally", AveragePercentileRankGlobally),
                new XElement("CandidateType", config.AttractFunctionConfig.CandidateType),
                new XElement("ClusterType", config.AttractFunctionConfig.ClusterType));

            mappingStatisticXml.Add(this.config.AttractFunctionConfig.ToXElement());

            XElement CandidateResult = new XElement("candidateStatisticResults");

            UnityEngine.Debug.Log($"Generating xml for {resultsOrdered.Count} finished results.");
            resultsOrdered.ForEach(r => CandidateResult.Add(r.ToXElement()));

            mappingStatisticXml.Add(CandidateResult);

            return mappingStatisticXml;
        }

        /// <summary>
        /// Calculates results based on the finished CandidateStatistic objects.
        /// </summary>
        public void CalculateResults()
        {
            calculatedResults = true;

            if(resultsOrdered.Count == 0)
            {
                return;
            }

            resultsOrdered.ForEach(r => r.CalculateResults());
            CandidatesTotal = resultsOrdered.Count();
            InitiallyMapped = resultsOrdered.Where((r) => r.MappedAtMappingStep == -1).Count();
            CandidatesConsidered = CandidatesTotal - InitiallyMapped;
            TotalHits = resultsOrdered.Where(r => r.Hit && r.MappedAtMappingStep >= 0).Count();
            TotalFails = resultsOrdered.Where(r => !r.Hit && r.MappedAtMappingStep >= 0 && r.MappedAtMappingStep < int.MaxValue).Count();
            CandidatesLeftOver = resultsOrdered.Where((r) => r.MappedAtMappingStep == int.MaxValue).Count();
            
            if (CandidatesConsidered > 0)
            {
                MappingRate = (CandidatesConsidered - CandidatesLeftOver) / CandidatesConsidered;
                HitRateAll = TotalHits / CandidatesConsidered;
            } 
            else
            {
                MappingRate = 0;
                HitRateAll = 0;
            }

            if(CandidatesConsidered - CandidatesLeftOver > 0)
            {
                HitRateMapped = TotalHits / (CandidatesConsidered - CandidatesLeftOver);
            } 
            else
            {
                HitRateMapped = 0;
            }

            Precision = (TotalHits + TotalFails) > 0 ? TotalHits / (TotalHits + TotalFails) : 0;
            
            Recall = (TotalHits + CandidatesLeftOver) > 0 ? TotalHits / (TotalHits + CandidatesLeftOver) : 0;

            FScore = (Precision + Recall) > 0 ? (2 * Precision * Recall) / (Precision + Recall) : 0;

            IEnumerable<double> validPercentileRankValues = resultsOrdered.Where(r => r.AveragePercentileRank >= 0)
                                                                         .Select(r => r.AveragePercentileRank);

            AveragePercentileRankGlobally = validPercentileRankValues.Count() > 0 ? validPercentileRankValues.Average() : -1; 
        }

        /// <summary>
        /// Averages the results for a given list of 
        /// <see cref="MappingExperimentResult"/> objects and creates
        /// based on the averaged values a new <see cref="MappingExperimentResult"/> 
        /// object.
        /// </summary>
        /// <param name="results">List of given reults</param>
        /// <param name="config">Configuration parameters add to the created result.</param>
        /// <returns></returns>
        public static MappingExperimentResult AverageResults(IEnumerable<MappingExperimentResult> results, 
                                                             RecommendationSettings config)
        {
            MappingExperimentResult averageResult = new MappingExperimentResult();

            averageResult.AveragePercentileRankGlobally = results.Select(r => r.AveragePercentileRankGlobally).Average();
            
            // Total values
            averageResult.TotalHits = results.Select(r => r.TotalHits).Sum();
            averageResult.CandidatesTotal = results.Select(r => r.CandidatesTotal).Sum();
            averageResult.CandidatesConsidered = results.Select(r => r.CandidatesConsidered).Sum();
            averageResult.CandidatesLeftOver = results.Select(r => r.CandidatesLeftOver).Sum();
            averageResult.TotalFails = results.Select(r => r.TotalFails).Sum();
            averageResult.InitiallyMapped = results.Select(r => r.InitiallyMapped).Sum();

            // Average values
            averageResult.MappingRate = results.Select(r => r.MappingRate).Average();
            averageResult.MappingRateStd = results.Select(r => r.MappingRate).StandardDeviation();
            averageResult.HitRateAll = results.Select(r => r.HitRateAll).Average();
            averageResult.HitRateAllStd = results.Select(r => r.HitRateAll).StandardDeviation();
            averageResult.HitRateMapped = results.Select(r => r.HitRateMapped).Average();
            averageResult.HitRateMappedStd = results.Select(r => r.HitRateMapped).StandardDeviation();
          
            averageResult.Recall = results.Select(r => r.Recall).Average();
            averageResult.RecallStd = results.Select(r => r.Recall).StandardDeviation();
            averageResult.Precision = results.Select(r => r.Precision).Average();
            averageResult.PrecisionStd = results.Select(r => r.Precision).StandardDeviation();
            averageResult.FScore = results.Select(r => r.FScore).Average();
            averageResult.FScoreStd = results.Select(r => r.FScore).StandardDeviation();

            averageResult.AddConfigInformation(config);
            averageResult.CalculateResults();
            return averageResult;
        }
    }
}