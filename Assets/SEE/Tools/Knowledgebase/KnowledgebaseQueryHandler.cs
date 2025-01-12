using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.SEE.Tools.Knowledgebase
{
    internal class KnowledgebaseQueryHandler : IDisposable
    {
        private readonly IDriver _driver;
        // These credationals must match your database. TODO: Let the user enter this in UI
        private readonly string uri = "bolt://localhost:7687";
        private readonly string user = "neo4j";
        private readonly string password = "MiniExample";

        public KnowledgebaseQueryHandler()
        {
            _driver = GraphDatabase.Driver(this.uri, AuthTokens.Basic(this.user, this.password));
        }
        /// <summary>
        /// This Method creates Cyber-Queries for neo4j depending on the intent and entities
        /// </summary>
        /// <param name="json">Json Object with Data from Rasa</param>
        /// <returns>Query String</returns>
        public async Task<string> CreateQueryAsync(JObject json)
        {
            Dictionary<string, string> entities = new Dictionary<string, string>();

            // add entities only if they are present in the JSON
            if (json["intent"] != null)
            {
                entities.Add("intent", json["intent"].ToString());
            }
            if (json["entities"]?["subject"] != null)
            {
                entities.Add("subject", json["entities"]["subject"].ToString());
            }

            if (json["entities"]?["place"] != null)
            {
                entities.Add("place", json["entities"]["place"].ToString());
            }

            if (json["entities"]?["metric"] != null)
            {
                entities.Add("metric", json["entities"]["metric"].ToString());
            }

            if (json["entities"]?["operation"] != null)
            {
                entities.Add("operation", json["entities"]["operation"].ToString());
            }

            Debug.Log($"Received request - Intent: {entities["intent"]}, Place: {entities["place"] ?? "unknown"}, Subject: {entities["subject"] ?? "unknown"}, Metric: {entities["metric"] ?? "unknown"}, operation: {entities["operation"] ?? "unknown"} "); ;
            string query = null;
            if (json["intent"].ToString() == "QueryMetricInSubject")
            {
                if (entities["subject"] == null || entities["metric"] == null)
                {
                    return null;
                }
                else
                {
                    query = $"MATCH(n:{entities["subject"]}) " +
                    $"WHERE n.`{entities["metric"]}` IS NOT NULL " +
                    $"RETURN n.`{entities["metric"]}`  AS Metric, n.`Source.Name` AS Source_Name, n.id AS id " +
                    $"ORDER BY Metric";

                    if (entities["operation"] == "max")
                    {
                        query = query + " DESC " + "LIMIT 1 ";
                    }
                    else
                    {
                        query = query + " ASC " + "LIMIT 1 ";
                    }
                }

                Debug.Log("Query is: " + query);
            }
            if (json["intent"].ToString() == "QueryMetricInPlace")
            {
                Debug.Log("Intent is QueryMetricInPlace, with place: " + entities["place"] + "and Metric: " + entities["metric"]);
                if (entities["place"] == null || entities["metric"] == null)
                {
                    return null;
                }
                else
                {
                    query = $"MATCH(n)" +
                            $"WHERE n.`{entities["metric"]}` IS NOT NULL " +
                              $"AND n.`Source.Name` = '{entities["place"]}' " +
                            $"RETURN n.`{entities["metric"]}` AS Metric, " +
                                   $"n.`Source.Name` AS Source_Name, " +
                                   "n.id AS id " +
                            "ORDER BY Metric DESC " +
                            "LIMIT 1";

                }
                Debug.Log("Query is: " + query);
            }

            if (json["intent"].ToString() == "ProjectOverview")
            {
                query = "MATCH(c: Class) " +
                        "WITH COUNT(c) AS numberOfClasses " +
                        "MATCH(m: Method) " +
                        "RETURN numberOfClasses, COUNT(m) AS numberOfMethods;";
            }

            return query;
        }

        /// <summary>
        /// This methods finally runs the created Query. Depending on the intent it stores the relevant information.
        /// </summary>
        /// <param name="intent">Intent of the user</param>
        /// <param name="query">Query which is created before</param>
        /// <returns></returns>
        public async Task<List<QueryInfo>> RunQueryAsync(string intent, string query)
        {
            try
            {
                await using var session = _driver.AsyncSession();
                var QueryInfo = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            query
                            );

                        var QueryInfoList = new List<QueryInfo>();
                        string numberOfClasses = null;
                        string numberOfMethods = null;
                        string id = null;
                        string metric = null;
                        string sourcename = null;

                        // Iterate over each record in the result
                        await result.ForEachAsync(record =>
                        {
                            if(intent == "ProjectOverview")
                            {
                                numberOfClasses = record["numerOfClasses"]?.As<string>() ?? null;
                                numberOfMethods = record["numerOfMethods"]?.As<string>() ?? null;
                            }
                            if(intent == "QueryMetricInSubject" || intent == "QueryMetricInPlace")
                            {
                                metric = record["Metric"]?.As<string>() ?? "Unknown complexity";
                                sourcename = record["Source_Name"]?.As<string>() ?? "Unknown Source";
                                id = record["id"]?.As<string>() ?? null;
                            }
                            if(intent == "findPlace")
                            {
                                sourcename = record["Source_Name"]?.As<string>();
                                id = record["id"]?.As<string>() ?? null;
                            }
                            if (intent == "countIn")
                            {
                                numberOfClasses = record["totalClasses"]?.As<string>() ?? null;
                            }


                            // Add each information to the list
                            QueryInfoList.Add(new QueryInfo(numberOfClasses, numberOfMethods, id, metric, sourcename));
                        });

                        return QueryInfoList;

                    }
                );
                return QueryInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred: {ex.Message}");
                return new List<QueryInfo>(); // Return an empty list in case of error
            }

        }


        public void Dispose()
        {
            _driver?.Dispose();
        }

    }

    /// <summary>
    /// Structure to store Information from the database
    /// </summary>
    public class QueryInfo
    {
        public string Id { get; set; }
        public string Metric { get; set; }
        public string Sourcename { get; set; }
        public string NumberOfClasses { get; set; }
        public string NumberOfMethods { get; set; }

        public  QueryInfo(string numberOfClasses, string numberOfMethods, string id, string metric, string sourcename)
        {
            Id = id;
            Metric = metric;
            Sourcename = sourcename;
            NumberOfClasses = numberOfClasses;
            NumberOfMethods = numberOfMethods;
        }

    }

}
