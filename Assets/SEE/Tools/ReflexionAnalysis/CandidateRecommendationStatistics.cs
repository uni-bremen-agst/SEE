using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static Assets.SEE.Tools.ReflexionAnalysis.CandidateRecommendation;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendationStatistics
    {
        private readonly static int FLUSH_TRESHOLD = 5000;

        int mappingStep = 0;

        List<List<MappingPair>> mappingProcess;

        int numberMappingPairs;

        FilePath csvPath;

        Dictionary<string, CandidateStatisticResult> results = new Dictionary<string, CandidateStatisticResult>();

        public ReflexionGraph ReflexionGraph { get; private set; }
        public ReflexionGraph OracleGraph { get; private set; }

        private static string MAPS_TO_TYPE = "Maps_To";

        public string TargetType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oracleMappingPath"></param>
        public CandidateRecommendationStatistics(FilePath csvPath,
                                                 ReflexionGraph reflexionGraph,
                                                 Graph oracleMapping,
                                                 string targetType)
        {
            TargetType = targetType;
            this.csvPath = csvPath;
            mappingProcess = new List<List<MappingPair>>
            {
                new List<MappingPair>()
            };
            ReflexionGraph = reflexionGraph;
            (Graph implementation, Graph architecture, _) = reflexionGraph.Disassemble();
            OracleGraph = new ReflexionGraph(implementation, architecture, oracleMapping);

            UnityEngine.Debug.Log("Oracle implementation nodes: ");
            implementation.Nodes().ForEach(n => UnityEngine.Debug.Log($"{n.ID} has Parent {n.Parent?.ID}({n.Parent.GetSubgraph()})"));

            OracleGraph.RunAnalysis();
        }

        public void Reset()
        {
            mappingStep = 0;
            mappingProcess.Clear();
            results.Clear();
        }

        public void RecordMappingPairs(List<MappingPair> attractionValues)
        {
            mappingProcess[mappingStep].AddRange(attractionValues);
        }

        public void RecordChosenMappingPair(MappingPair chosenMappingPair)
        {
            // Add chosen pair to the start of the list, so the chosenMappingPair should be contained twice
            mappingProcess[mappingStep].Insert(0, chosenMappingPair);
            numberMappingPairs += mappingProcess[mappingStep].Count;
            mappingStep++;
            if (numberMappingPairs > FLUSH_TRESHOLD)
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
            numberMappingPairs = 0;
            File.WriteAllText(csvPath.Path, csv);
        }

        public void ProcessMappingData(string csvFile, string xmlFile)
        {
            if (!File.Exists(csvFile))
            {
                File.Create(csvFile);
            }
            CreateCandidateResults(csvFile);
            CreateXml(results.Values.ToList(), xmlFile);
        }

        private void CreateCandidateResults(string csvFile)
        {
            Dictionary<string, List<double>> percentileRanks = new Dictionary<string, List<double>>();
            (Graph implementation, _, _) = ReflexionGraph.Disassemble();

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
                        // Calculate statistical Results for the chosen candidate
                        CandidateStatisticResult candidateStatisticResult;
                        if (!results.TryGetValue(mappingPairs[0].CandidateID, out candidateStatisticResult))
                        {
                            candidateStatisticResult = new CandidateStatisticResult(mappingPairs[0]);
                            results.Add(mappingPairs[0].CandidateID, candidateStatisticResult);
                        }

                        candidateStatisticResult.MappedClusterID = mappingPairs[0].ClusterID;
                        candidateStatisticResult.MappedAtMappingStep = mappingStep;
                        candidateStatisticResult.Hit = IsHit(candidateStatisticResult);

                        string expectedClusterID = OracleGraph.MapsTo(OracleGraph.GetNode(candidateStatisticResult.CandidateID))?.ID;
                        candidateStatisticResult.ExpectedClusterID = expectedClusterID != null ? expectedClusterID : "Unknown";

                        // only use remaining mappingPairs
                        mappingPairs = mappingPairs.GetRange(1, mappingPairs.Count - 1);

                        // only one candidate left
                        if (mappingPairs.Count == 0) continue;

                        // sort mappings by attractionValue
                        mappingPairs.Sort();

                        // calculate percentileRanks
                        CalculatePercentileRanks(implementation, percentileRanks, mappingPairs);

                        mappingStep++;
                    }
                }

                // set lists of percentile ranks
                // TODO: remove PercentileRanks as second dictionary, all data should be saved directly to the results objects
                percentileRanks.Keys.ForEach(candidateID => results[candidateID].PercentileRanks = percentileRanks[candidateID]);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
                throw e;
            }
        }

        private bool IsHit(CandidateStatisticResult candidateStatisticResult)
        {
            HashSet<string> candidateAscendants = OracleGraph.GetNode(candidateStatisticResult.CandidateID).Ascendants().Select(n => n.ID).ToHashSet();
            HashSet<string> clusterAscendants = OracleGraph.GetNode(candidateStatisticResult.MappedClusterID).Ascendants().Select(n => n.ID).ToHashSet();
            return OracleGraph.Edges().Any(e => e.IsInMapping()
                                                && candidateAscendants.Contains(e.Source.ID)
                                                && clusterAscendants.Contains(e.Target.ID));
        }

        private void CreateXml(List<CandidateStatisticResult> results, string xmlFile)
        {
            int hits = results.Where(r => r.Hit).Count();
            int fails = results.Count - hits;
            int initiallyMapped = results.Where((r) => r.MappedAtMappingStep < 0).Count();
            int chosen = results.Where((r) => r.Chosen == true).Count();

            XElement mappingStatistic = new XElement("mappingStatistic",
                new XAttribute(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"),
                new XElement("hits", hits),
                new XElement("fails", fails),
                new XElement("initiallyMapped", initiallyMapped),
                new XElement("chosen", chosen),
                new XElement("targetType", TargetType));

            XElement percentileRanksElement = new XElement("percentileRanksElement");

            results.ForEach(result => percentileRanksElement.Add(result.ToXElement()));

            mappingStatistic.Add(percentileRanksElement);
            mappingStatistic.Save(new FileStream(xmlFile, FileMode.Create));
        }

        private void CalculatePercentileRanks(
                                              Graph implementation,
                                              Dictionary<string, List<double>> percentileRanks,
                                              List<MappingPair> mappingPairs)
        {
            List<Node> candidates = implementation.Nodes()
                                .Where(n => n.Type.Equals(TargetType))
                                .ToList();

            // iterate all candidates within oracle mapping
            // to calculate their rank within the suggestions
            foreach (Node candidate in candidates)
            {
                List<Edge> oracleEdges = OracleGraph.Edges().Where(
                    (e) => e.IsInMapping() && e.Source.PostOrderDescendants().Any(n => string.Equals(n.ID, candidate.ID))).ToList();

                if (oracleEdges.Count > 1) throw new Exception("Oracle Mapping is Ambigous.");
                if (oracleEdges.Count == 0)
                {
                    UnityEngine.Debug.LogWarning($"Oracle Mapping is Incomplete. There is no information about the node {candidate.ID}");
                    continue;
                }

                // skip candidates which were already mapped based on the data.
                if (results.ContainsKey(candidate.ID))
                {
                    writePercentileRank(-1.0);
                    continue;
                };

                Edge oracleEdge = oracleEdges[0];

                // Get all clusters 
                HashSet<string> clusterIDs = oracleEdge.Target.PostOrderDescendants().Select(c => c.ID).ToHashSet();
                List<string> suggestedIdsForCluster = mappingPairs.Where(p => clusterIDs.Contains(p.ClusterID))
                                                                  .Select(p => p.CandidateID).ToList();

                // Calculation of percentileRank
                // TODO: divide the list into plateaus, so mappingPairs with the same attraction have the same rank.
                double percentileRank = 1 - (((double)suggestedIdsForCluster.IndexOf(candidate.ID)) / suggestedIdsForCluster.Count);
                percentileRank = Math.Round(percentileRank, 4);

                writePercentileRank(percentileRank);

                void writePercentileRank(double percentileRank)
                {
                    if (!percentileRanks.ContainsKey(candidate.ID))
                    {
                        percentileRanks[candidate.ID] = new List<double>();
                    }
                    percentileRanks[candidate.ID].Add(percentileRank);
                }
            }
        }

        public void StartRecording()
        {
            IEnumerable<Node> implementationNodes = OracleGraph.Nodes().Where(n => n.IsInImplementation()
                                                                            && n.Type.Equals(TargetType));

            foreach (Node node in implementationNodes)
            {
                Node mapsTo = ReflexionGraph.MapsTo(node);
                if (mapsTo != null)
                {
                    Node expectedCluster = OracleGraph.MapsTo(node);
                    CandidateStatisticResult statisticResult = new CandidateStatisticResult(node.ID, expectedCluster?.ID);
                    statisticResult.MappedClusterID = mapsTo.ID;
                    statisticResult.Hit = IsHit(statisticResult);
                    results.Add(node.ID, statisticResult);
                }
            }
        }

        public class CandidateStatisticResult
        {
            public CandidateStatisticResult(MappingPair mappingPair)
            {
                AttractionValue = mappingPair.AttractionValue;
                CandidateID = mappingPair.CandidateID;
                MappedClusterID = mappingPair.ClusterID;
            }

            public CandidateStatisticResult(string candidateID, string expectedClusterID)
            {
                CandidateID = candidateID;
                ExpectedClusterID = expectedClusterID;
            }

            public double AttractionValue { get; set; } = -1;
            public string CandidateID { get; set; }
            public string MappedClusterID { get; set; } = "Unknown";
            public string ExpectedClusterID { get; set; } = "Unknown";
            public List<double> PercentileRanks { get; set; } = new List<double>();
            public int MappedAtMappingStep { get; set; } = -1;
            public bool Hit { get; set; }
            public bool? Chosen { get; set; }

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

                List<double> validpercentileRanks = PercentileRanks.Where(n => n >= 0).ToList();
                double averageRank = validpercentileRanks.Count > 0 ? validpercentileRanks.Average() : -1;
                averageRank = Math.Round(averageRank, 4);
                XAttribute average = new XAttribute("average", averageRank);

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
}
