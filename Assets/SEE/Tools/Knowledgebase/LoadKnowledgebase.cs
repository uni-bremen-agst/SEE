using UnityEngine;
using SEE.Controls;
using SEE.Game.City;
using SEE.Game;
using SEE.GO;
using SEE.DataModel.DG;
using Neo4j.Driver;
using System.Collections.Generic;
using UnityEditor.Search;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;

namespace Assets.SEE.Tools.Knowledgebase
{
    public class LoadKnowledgebase : MonoBehaviour, IDisposable
    {
        private IDriver _driver;
        private readonly string uri = "bolt://localhost:7687";
        private readonly string user = "neo4j";
        private readonly string password = "MiniExample";
        private readonly string knowledgeFolder = Application.streamingAssetsPath + "/KnowledgeBase/";

        // Prework for generating trainingsdata
        Dictionary<string, string> metrics = new Dictionary<string, string>
        {
            { "Lines Of Code", "Metric.Lines.LOC" }
        };

        // will be filled with knowledgebase
        Dictionary<string, string> places = new Dictionary<string, string>{};

        private void Update()
        {
            if (SEEInput.LoadDB())
            {
                Debug.Log("Load was pressed");
                LoadNodes();
                //GenerateTrainingData();
            }
        }


        private async Task LoadNodes()
        {
            _driver = GraphDatabase.Driver(this.uri, AuthTokens.Basic(this.user, this.password));
            GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
            int nodeCounter = 0;
            int hierachyEdgesCounter = 0;
            int nonhierarchyEdgesCounter = 0;

            List<string> names = new List<string>{};

            // We will search in each code city.
            foreach (GameObject cityObject in cities)
                if (cityObject.TryGetComponentOrLog(out AbstractSEECity city))
                {
                    // but only search in Graph/Tables that are loaded
                    if (city.LoadedGraph == null)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (Node node in city.LoadedGraph.Nodes())
                        {
                            names.Add(node.SourceName);
                            Debug.Log("Node-Type: " + node.Type +  "id: " + node.ID  + " SourceName: " + node.SourceName + "parent: " + node.Parent);
                            string attributes = LoadMetrics(node);
                            try
                            {
                                await using var session = _driver.AsyncSession();

                                // Create Nodes in (Neo4j) Graph Database
                                var query = $"CREATE (n:{node.Type} {attributes}) RETURN n";
                                //Debug.Log(query);

                                await session.ExecuteWriteAsync(async tx =>
                                {
                                    var result = await tx.RunAsync(query);

                                    // Log each node creation for debugging purposes
                                    await result.ForEachAsync(record =>
                                    {
                                        //Debug.Log("Maybe created :D");
                                    });
                                });
                                nodeCounter++;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"An error occurred while creating a node: {ex.Message}");
                            }
                        }
                        ///// LOAD RASA
                        // File path to save the generated .yml file
                        string filePath = "rasa_synonyms.yml";

                        // Generate the YAML content
                        string yamlContent = GenerateYamlContent(names);

                        // Write to .yml file
                        File.WriteAllText(knowledgeFolder + filePath, yamlContent);

                        Console.WriteLine($"YAML file generated at {filePath}");

                        foreach (Node node in city.LoadedGraph.Nodes())
                        {
                            // add an edge between parent and child for the hierarchy
                            if (node.Parent != null)
                            {
                                try
                                {
                                    await using var session = _driver.AsyncSession();

                                    // Create Edge in graph Database
                                     var query2 = $"MATCH(n1: {node.Type} {{ id: '{node.ID}'}}) MATCH(n2: {node.Parent.Type} {{ id: '{node.Parent.ID}'}}) WITH n1, n2 CREATE(n2) - [r:HIERARCHY_PARENT_OF]->(n1) RETURN r";


                                    Debug.Log("Query for edge: " + query2);

                                    // Execute the query
                                    await session.ExecuteWriteAsync(async tx =>
                                    {
                                        var result = await tx.RunAsync(query2);

                                        // Log each node creation for debugging purposes
                                        await result.ForEachAsync(record =>
                                        {

                                        });
                                    });
                                hierachyEdgesCounter++;
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"An error occurred while creating a node: {ex.Message}");
                                }
                            }
                        }
                        // add also edges that are non-hierachy
                        foreach (Edge edge in city.LoadedGraph.Edges())
                        {
                            Debug.Log("Edge-Type: " + edge.Type + "Source: " + edge.Source.ID + " Target: " + edge.Target.ID);
                            // not used now by rasa, so uncomment to shorten load time
                            /*try
                            {
                                await using var session = _driver.AsyncSession();

                                // Create Edge in (Neo4j) Graph Database
                                var query = $"MATCH(n1: Node {{ id: '{edge.Source.ID}'}}) MATCH(n2: Node {{ id: '{edge.Target.ID}'}}) WITH n1, n2 CREATE(n1) - [r:NONHIERARCHY_{edge.Type}]->(n2) RETURN r";



                                // Execute the query with parameters
                                await session.ExecuteWriteAsync(async tx =>
                                {
                                    var result = await tx.RunAsync(query);

                                    // Log each node creation for debugging purposes
                                    await result.ForEachAsync(record =>
                                    {
                                        //Debug.Log("Maybe created :D");
                                    });
                                });
                                nonhierarchyEdgesCounter++;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"An error occurred while creating a node: {ex.Message}");
                            }*/
                        }
                    }
                    Debug.Log("Loading of " + nodeCounter + " Nodes, " + hierachyEdgesCounter + " hierarchy edges and " + nonhierarchyEdgesCounter + " nonhierarchy edges was successfull");
                }

        }
        /// <summary>
        /// For every node we need to create all the metrics and attributes
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public string LoadMetrics(Node node)
        {
            string attributes = $"{{id: '{node.ID}', type: '{node.Type}', sourceName: '{node.SourceName}'";
            float floatMetricValue;
            string stringMetricValue;

            foreach (string metricName in node.AllMetrics())
            {
                if (node.TryGetNumeric(metricName, out floatMetricValue) == true)
                {
                    attributes = attributes + $",`{metricName}`: '{floatMetricValue}'";
                }
            }
            foreach(string metricName in node.AllFloatAttributeNames())
            {
                if (node.TryGetNumeric(metricName, out floatMetricValue) == true)
                {
                    attributes = attributes + $",`{metricName}`: '{floatMetricValue}'";
                }
            }
            foreach (string metricName in node.AllStringAttributeNames())
            {
                if (node.TryGetString(metricName, out stringMetricValue) == true)
                {
                    attributes = attributes + $",`{metricName}`: '{stringMetricValue}'";
                }
            }
            attributes = attributes + "}";

            return attributes;
        }

        /// Following parts should be for setting up training data for RASA - not finished yet

        static string GenerateYamlContent(List<string> names)
        {
            StringBuilder yamlBuilder = new StringBuilder();

            // Add the main structure
            yamlBuilder.AppendLine("version: \"3.1\"");
            yamlBuilder.AppendLine("nlu:");

            // Add the lookup table for Place with the original names
            yamlBuilder.AppendLine("  - lookup: Place");
            yamlBuilder.AppendLine("    examples: |");
            HashSet<string> uniqueExamples = new HashSet<string>(); // To avoid duplicate examples in Look-up

            foreach (string name in names)
            {
                // Always add the original name to the lookup examples
                if (uniqueExamples.Add(name))
                {
                    yamlBuilder.AppendLine($"      - {name}");
                }
                // Generate synonyms for the name and add to lookup examples too
                List<string> synonyms = GenerateSynonyms(name);
                foreach (string synonym in synonyms)
                {

                        // Only add unique synonyms to the lookup examples
                        if (uniqueExamples.Add(synonym))
                        {
                            yamlBuilder.AppendLine($"      - {synonym}");
                        }


                }
            }


            // append synynoms
            foreach (string name in names)
            {


                    // Create variations for each name
                    List<string> synonyms = GenerateSynonyms(name);
                foreach(string synonym in synonyms)
                {
                    if (name != synonym)
                    {
                        // Format as YAML
                        yamlBuilder.AppendLine($"- synonym: {name}");
                        yamlBuilder.AppendLine("  examples: |");
                        foreach (string realSynonym in synonyms)
                        {
                            yamlBuilder.AppendLine($"    - {realSynonym}");
                        }
                    }
                }


            }

            return yamlBuilder.ToString();
        }

        static List<string> GenerateSynonyms(string name)
        {
            List<string> synonyms = new List<string>
        {
            AddSpacesBetweenCamelCase(name)
        };

            return synonyms;
        }

        static string AddSpacesBetweenCamelCase(string input)
        {
            // Inserts spaces between camel case words, e.g. "GraphProvider" to "Graph Provider"
            StringBuilder result = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsUpper(c) && result.Length > 0)
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }



        public List<string> GenerateTrainingData()
        {
            List<string> trainingsData = new List<string>();

            // Define the template variations
            string[] templates = {
            "- Give me {Metric} of {Place}.",
            "- How is {Metric} of {Place}?",
            "- How much {Metric} is in {Place}?",
            "- Show the {Metric} for {Place}.",
            "- What is the current {Metric} of {Place}?",
            "- Provide the {Metric} for {Place}.",
            "- How many {Metric} are there in {Place}?",
            "- What’s the {Metric} in {Place}?",
            "- Get the {Metric} in {Place}.",
            "- Can you show the {Metric} for {Place}?",
            "- What is the {Metric} for {Place}?",
            "- Give me the {Metric} in {Place}."
        };

            // Loop through each metric and place to create sentences
            foreach (var metric in metrics)
            {
                foreach (var place in places)
                {
                    foreach (var template in templates)
                    {
                        // Replace placeholders with actual values from dictionaries
                        string sentence = template
                            .Replace("{Metric}", $"[{metric.Key}]{{\"entity\": \"Metric\", \"value\": \"{metric.Value}\"}}")
                            .Replace("{Place}", $"[{place.Key}]{{\"entity\": \"Place\", \"value\": \"{place.Value}\"}}");

                        trainingsData.Add(sentence);
                        Debug.Log("Trainingsdata: " + sentence);
                    }
                }
            }


            return trainingsData;
        }



    public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}
