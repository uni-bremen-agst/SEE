using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Utils;
using SEE.Utils.Paths;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Loads and stores multiple GXL files from a directory.
    /// </summary>
    public class GraphsReader
    {
        /// <summary>
        /// Contains all loaded graphs after calling Load().
        /// </summary>
        public readonly List<Graph> Graphs = new();

        /// <summary>
        /// Loads all GXL and their associated CSV files (limited to <paramref name="maxRevisionsToLoad"/> many
        /// files) from <paramref name="directory"/> and saves these in <see cref="Graphs"/>.
        ///
        /// For every GXL file, F.gxl , contained in <paramref name="directory"/>, the graph
        /// data therein will be loaded into a new graph that is then added to <see cref="Graphs"/>.
        /// If there is a file F.csv contained in <paramref name="directory"/>, this file is assumed
        /// to carry additional metrics for the graph nodes. These metrics will be read and added to
        /// the nodes in the loaded graph where the unique node ID is used to identify the node to
        /// which the metrics are to be added.
        /// </summary>
        /// <param name="directory">The directory path where the GXL file are located in.</param>
        /// <param name="hierarchicalEdgeTypes">The set of edge-type names for edges considered to represent nesting.</param>
        /// <param name="basePath">The base path of the graphs.</param>
        /// <param name="rootName">Name of the root node if any needs to be added to have a unique root.</param>
        /// <param name="maxRevisionsToLoad">The upper limit of files to be loaded.</param>
        public async UniTask LoadAsync(string directory, HashSet<string> hierarchicalEdgeTypes, string basePath,
                                       string rootName, int maxRevisionsToLoad)
        {
            IEnumerable<string> sortedGraphNames = Filenames.GXLFilenames(directory).ToList();
            if (!sortedGraphNames.Any())
            {
                throw new Exception($"Directory '{directory}' has no GXL files.");
            }
            Graphs.Clear();

            GraphReader graphCreator = new(hierarchicalEdgeTypes,
                               basePath: basePath,
                               rootID: rootName,
                               logger: new SEELogger());

            Performance p = Performance.Begin($"Loading GXL files from {directory}");
            // for all found GXL files load and save the graph data
            foreach (string gxlPath in sortedGraphNames)
            {
                // load graph (we can safely assume that the file exists because we retrieved its
                // name just from the directory
                DataPath dataPath = new()
                {
                    Path = gxlPath
                };

                await graphCreator.LoadAsync(await dataPath.LoadAsync(), dataPath.Path);
                Graph graph = graphCreator.GetGraph();

                // if graph was loaded, put in graph list
                if (graph == null)
                {
                    Debug.LogError($"Graph {gxlPath} could not be loaded.\n");
                }
                else
                {
                    string csvFilename = Path.ChangeExtension(gxlPath, Filenames.CSVExtension);
                    if (File.Exists(csvFilename))
                    {
                        Debug.Log($"Loading CSV file {csvFilename}.\n");
                        int numberOfErrors = await MetricImporter.LoadCsvAsync(graph, csvFilename);
                        if (numberOfErrors > 0)
                        {
                            Debug.LogError($"CSV file {csvFilename} has {numberOfErrors} many errors.\n");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"CSV file {csvFilename} does not exist.\n");
                    }
                    maxRevisionsToLoad--;
                    Graphs.Add(graph);
                }
                if (maxRevisionsToLoad <= 0)
                {
                    break;
                }
            }
            p.End();
            Debug.Log($"Number of graphs loaded: {Graphs.Count}\n");
        }
    }
}
