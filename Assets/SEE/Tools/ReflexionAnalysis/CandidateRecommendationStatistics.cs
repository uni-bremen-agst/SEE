using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Newtonsoft.Json;
using SEE.DataModel;
using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class provides operations to record the mapping process 
    /// to data objects.
    /// </summary>
    public class CandidateRecommendationStatistics
    {
        /// <summary>
        /// Threshold defining after how many mapping operations recorded data 
        /// will be flushed to a file.
        /// </summary>
        private readonly static int FLUSH_TRESHOLD = 50000;

        /// <summary>
        /// Current mapping step of the current recording
        /// </summary>
        private int mappingStep = 0;

        /// <summary>
        /// Total number of mapped pairs.
        /// </summary>
        private int numberMappedPairs;

        ///// <summary>
        ///// Csv File the recorded data is written to.
        ///// </summary>
        public string csvFile { get; private set; }

        /// <summary>
        /// List containing a list of current attraction values represented as mapping pairs for each mapping step
        /// </summary>
        List<List<MappingPair>> mappingProcess;

        /// <summary>
        /// Associated candidate recommendation object
        /// </summary>
        public CandidateRecommendation CandidateRecommendation { get; private set; }

        /// <summary>
        /// Is set to true if the recording is currently active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Result object managing the statistical data and objects to calculate results
        /// </summary>
        private MappingExperimentResult mappingResult;

        /// <summary>
        /// Recommendation settings used while recording.
        /// </summary>
        RecommendationSettings config;

        /// <summary>
        /// Determines if all mapping pairs should be recorded during each 
        /// mapping step or only the mapped one.
        /// </summary>
        // private bool recordAllMappingPairs;

        /// <summary>
        /// Construction initializes a new instance of <see cref="CandidateRecommendationStatistics"/>
        /// </summary>
        /// <param name="recommendation">recommendation object associated with recording the data</param>
        public CandidateRecommendationStatistics(CandidateRecommendation recommendation)
        {
            CandidateRecommendation = recommendation;
            mappingResult = new MappingExperimentResult();
        }

        /// <summary>
        /// Sets the recommendation settings used during recording.
        /// </summary>
        /// <param name="config">Given recommendation settings</param>
        public void SetConfigInformation(RecommendationSettings config)
        {
            this.mappingResult.AddConfigInformation(config);
            this.config = config;
            // this.recordAllMappingPairs = config.measurePercentileRanks;
        }

        /// <summary>
        /// Resets this object, so <see cref="StartRecording"/>() 
        /// can be called again.
        /// </summary>
        public void Reset()
        {
            mappingStep = 0;
            mappingProcess = new List<List<MappingPair>>
            {
                new List<MappingPair>()
            };
            mappingResult = new MappingExperimentResult();

            // TODO: Set configuration here?
            mappingResult.AddConfigInformation(config);
        }

        /// <summary>
        /// Records a given list of mapping pairs for the current mapping step.
        /// </summary>
        /// <param name="mappingPairs">Given list of mapping pairs</param>
        //public void RecordMappingPairs(IEnumerable<MappingPair> mappingPairs)
        //{
        //    // TODO: Measure all mapping pairs to calculate percentile ranks, delete later, 
        //    //if (recordAllMappingPairs)
        //    //{
        //    //    mappingProcess[mappingStep].AddRange(mappingPairs); 
        //    //}
        //}

        /// <summary>
        /// Records the changed mapping pair during the mapping process.
        /// After this call the current mapping stepp will be incremented.
        /// 
        /// This chosen mapping pair is add to the beginning of the 
        /// current list in <see cref="mappingProcess"/>.
        /// 
        /// </summary>
        /// <param name="chosenMappingPair"></param>
        public void RecordChosenMappingPair(MappingPair chosenMappingPair)
        {
            // Adds chosen pair to the start of the list, so the chosenMappingPair should be contained twice
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

        /// <summary>
        /// Flushes all currently recorded mapping pairs to the specified 
        /// <see cref="csvFile"/>.
        /// </summary>
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

            File.AppendAllText(csvFile, csv);
        }

        /// <summary>
        /// Reads the mapping process from a given csv file, processes 
        /// the data and writes the results to a given xml file.
        /// </summary>
        /// <param name="csvFile"></param>
        /// <param name="xmlFile"></param>
        public void WriteResultsToXml(string csvFile, string xmlFile)
        {
            MappingExperimentResult result = CalculateResults(csvFile);
            result.CreateXml().Save(new FileStream(xmlFile, FileMode.Create));
        }

        /// <summary>
        /// Calculates the results given an csv file containing the mapping process, 
        /// represented as recorded mapping pairs per mapping step.
        /// 
        /// To resolve data the <see cref="CandidateRecommendation"/> object within 
        /// this class is used.
        /// 
        /// </summary>
        /// <param name="csvFile">Given .csv file containing the recorded mapping process.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Throws if the csv file is not found.</exception>
        public MappingExperimentResult CalculateResults(string csvFile)
        {
            if (!File.Exists(csvFile))
            {
                throw new Exception($"No Data found to be processed. File {csvFile} is not existing.");
            }

            Dictionary<string, List<double>> percentileRanks = new Dictionary<string, List<double>>();
            
            int mappingStep = 0;
            string currentLine;

            List<string> idQueue = new();

            using (StreamReader reader = new StreamReader(csvFile))
            {
                while ((currentLine = reader.ReadLine()) != null)
                {
                    List<MappingPair> mappingPairs = currentLine.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => JsonConvert.DeserializeObject<MappingPair>(s)).ToList();

                    // first mappingPair within the line, was the chosen mappingPair
                    MappingPair chosenMappingPair = mappingPairs[0];

                    // Calculate statistical Results for the chosen candidate
                    CandidateStatistic candidateStatisticResult = mappingResult.GetCandidateStatisticResult(chosenMappingPair.CandidateID);

                    // TODO: move into MappingExperimentResult?
                    candidateStatisticResult.AttractionValue = chosenMappingPair.AttractionValue;
                    candidateStatisticResult.MappedClusterID = chosenMappingPair.ClusterID;
                    candidateStatisticResult.Hit = CandidateRecommendation.IsHit(candidateStatisticResult.CandidateID, 
                                                                                    candidateStatisticResult.MappedClusterID);

                    candidateStatisticResult.ExpectedClusterID = CandidateRecommendation.GetExpectedClusterID(candidateStatisticResult.CandidateID);

                    // only use remaining mappingPairs without chosen mapping
                    mappingPairs = mappingPairs.GetRange(1, mappingPairs.Count - 1);

                    // TODO: code for measuring percentile ranks, still necessary?
                    //if (recordAllMappingPairs)
                    //{
                    //    List<Node> candidates = CandidateRecommendation.GetCandidates();
                    //    foreach (Node candidate in candidates)
                    //    {
                    //        if (mappingResult.ContainsCandidateStatisticResult(candidate.ID))
                    //        {
                    //            double percentileRank = CandidateRecommendation.CalculatePercentileRank(candidate.ID, mappingPairs);
                    //            mappingResult.GetCandidateStatisticResult(candidate.ID).AddPercentileRank(percentileRank);
                    //        }
                    //    } 
                    //}

                    if (chosenMappingPair.ChangeType == ChangeType.Addition)
                    {
                        idQueue.Add(candidateStatisticResult.CandidateID); 
                    } 
                    else if(chosenMappingPair.ChangeType == ChangeType.Removal)
                    {
                        idQueue.Remove(candidateStatisticResult.CandidateID);
                    }
                }
            }

            mappingResult.FinishMappedCandidates(idQueue);
            mappingResult.FinishUnmappedCandidates();
            mappingResult.CalculateResults();
            return mappingResult;
        }

        /// <summary>
        /// Starts the recording of mapping pairs and writes them
        /// to the set <see cref="csvFile"/>
        /// </summary>
        public void StartRecording()
        {
            this.StartRecording(this.csvFile);
        }

        /// <summary>
        /// Starts the recording of mapping pairs and writes them
        /// to the set <see cref="csvFile"/>
        /// 
        /// Initializes all <see cref="CandidateStatistic"/> objects for 
        /// the current mapping constellation.
        /// 
        /// </summary>
        /// <param name="csvFile">given csvFile</param>
        public void StartRecording(string csvFile)
        {
            if (Active)
            {
                return;
            }

            this.Reset();

            // TODO: Is it really necessary to have the Oracle Graph selected during starting?
            //if(this.CandidateRecommendation.OracleGraph == null)
            //{
            //    UnityEngine.Debug.LogWarning("No OracleGraph loaded. No Data will be saved.");
            //    return;
            //}

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

            this.csvFile = csvFile;

            IEnumerable<Node> candidates = CandidateRecommendation.GetCandidates();

            foreach (Node node in candidates)
            {
                mappingResult.AddCandidateStatisticResult(node, CandidateRecommendation);
            }

            Active = true;
        }

        /// <summary>
        /// Stops the recording process and flushes all data to the <see cref="csvFile"/>.
        /// </summary>
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
