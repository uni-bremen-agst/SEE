using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendationStatistics
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly static int FLUSH_TRESHOLD = 5000;

        /// <summary>
        /// 
        /// </summary>
        private int mappingStep = 0;

        /// <summary>
        /// 
        /// </summary>
        private int numberMappedPairs;

        /// <summary>
        /// 
        /// </summary>
        private int seed;

        ///// <summary>
        ///// 
        ///// </summary>
        public string CsvFile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        List<List<MappingPair>> mappingProcess;

        /// <summary>
        /// 
        /// </summary>
        public CandidateRecommendation CandidateRecommendation { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Active { get; set; }

        private MappingExperimentStatisticResult mappingResult;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oracleMappingPath"></param>
        public CandidateRecommendationStatistics()
        {
            mappingResult = new MappingExperimentStatisticResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidateRecommendation"></param>
        public void SetCandidateRecommendation(CandidateRecommendation candidateRecommendation)
        {
            this.CandidateRecommendation = candidateRecommendation;
            Reset();
        }

        public void AddConfigInformation(MappingExperimentConfig config)
        {
            this.mappingResult.AddConfigInformation(config);
        }

        public void Reset()
        {
            mappingStep = 0;
            mappingProcess = new List<List<MappingPair>>
            {
                new List<MappingPair>()
            };
            mappingResult.Clear();
        }

        public void RecordMappingPairs(List<MappingPair> attractionValues)
        {
            mappingProcess[mappingStep].AddRange(attractionValues);
        }

        public void RecordChosenMappingPair(MappingPair chosenMappingPair)
        {
            // Add chosen pair to the start of the list, so the chosenMappingPair should be contained twice
            chosenMappingPair.ChosenAt = DateTime.UtcNow;
            mappingProcess[mappingStep].Insert(0, chosenMappingPair);
            numberMappedPairs += mappingProcess[mappingStep].Count;
            mappingStep++;
            if (numberMappedPairs > FLUSH_TRESHOLD)
            {
                this.Flush();
            }
            mappingProcess.Add(new List<MappingPair>());
        }

        public void Flush()
        {
            string csv = string.Empty;
            string line = string.Empty;
            string objectSeparator = ";";
            string lineSeparator = Environment.NewLine;
            for (int i = 0; i < mappingStep; i++)
            {
                foreach (MappingPair mappingPair in mappingProcess[i])
                {
                    line += JsonConvert.SerializeObject(mappingPair) + objectSeparator;
                }
                csv += line + lineSeparator;
                line = string.Empty;
            }
            mappingProcess.Clear();
            mappingStep = 0;
            numberMappedPairs = 0;

            File.AppendAllText(CsvFile, csv);
        }

        public void ProcessMappingData(string csvFile, string xmlFile)
        {
            if (!File.Exists(csvFile))
            {
                // TODO: Inform User here?
                UnityEngine.Debug.LogWarning($"No Data found to be processed. File {csvFile} is not existing.");
                return;
            }

            MappingExperimentStatisticResult result = CalculateResults(csvFile);
            result.CreateXml().Save(new FileStream(xmlFile, FileMode.Create));
        }

        private MappingExperimentStatisticResult CalculateResults(string csvFile)
        {
            Dictionary<string, List<double>> percentileRanks = new Dictionary<string, List<double>>();
           
            try
            {
                int mappingStep = 0;
                string currentLine;

                using (StreamReader reader = new StreamReader(csvFile))
                {
                    while ((currentLine = reader.ReadLine()) != null)
                    {
                        List<MappingPair> mappingPairs = currentLine.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => JsonConvert.DeserializeObject<MappingPair>(s)).ToList();

                        // first mappingPair within the line, was the chosen mappingPair
                        MappingPair chosenMappingPair = mappingPairs[0];

                        // Calculate statistical Results for the chosen candidate
                        CandidateStatisticResult candidateStatisticResult = mappingResult.GetCandidateStatisticResult(chosenMappingPair.CandidateID);

                        // TODO: move into MappingExperimentResult?
                        candidateStatisticResult.AttractionValue = chosenMappingPair.AttractionValue;
                        candidateStatisticResult.MappedClusterID = chosenMappingPair.ClusterID;
                        candidateStatisticResult.MappedAtMappingStep = mappingStep;
                        candidateStatisticResult.Hit = CandidateRecommendation.IsHit(candidateStatisticResult.CandidateID, 
                                                                                     candidateStatisticResult.MappedClusterID);

                        candidateStatisticResult.ExpectedClusterID = CandidateRecommendation.GetExpectedClusterID(candidateStatisticResult.CandidateID);

                        // only use remaining mappingPairs without chosen mapping
                        mappingPairs = mappingPairs.GetRange(1, mappingPairs.Count - 1);

                        List<Node> candidates = CandidateRecommendation.GetCandidates();

                        foreach (Node candidate in candidates)
                        {
                            if (mappingResult.ContainsCandidateStatisticResult(candidate.ID))
                            {
                                double percentileRank = CandidateRecommendation.CalculatePercentileRank(candidate.ID, mappingPairs);
                                mappingResult.GetCandidateStatisticResult(candidate.ID).AddPercentileRank(percentileRank); 
                            }
                        }

                        mappingResult.FinishCandidateStatisticResult(candidateStatisticResult.CandidateID);

                        mappingStep++;
                    }
                }

                mappingResult.CalculateResults();
                return mappingResult;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
                throw e;
            }
        }

        public void StartRecording(string csvFile)
        {
            if (Active) return;

            this.Reset();

            if(this.CandidateRecommendation.OracleGraph == null)
            {
                // TODO: Is it really necessary to have the Oracle Graph selected during starting?
                UnityEngine.Debug.LogWarning("No OracleGraph loaded. No Data will be saved.");
                return;
            }

            try
            {
                if (File.Exists(csvFile))
                {
                    File.Delete(csvFile);
                    File.Create(csvFile).Close();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error occured while creating the output file for the mapping statistics. {e}");
                return;
            }

            this.CsvFile = csvFile;

            IEnumerable<Node> mappedCandidates = CandidateRecommendation.GetCandidates();

            foreach (Node node in mappedCandidates)
            {
                mappingResult.AddCandidateStatisticResult(node, CandidateRecommendation);
            }
            Active = true;
        }

        public void StopRecording()
        {
            if (Active)
            {
                this.Flush();
                Active = false;
            }
        }
    }
}
