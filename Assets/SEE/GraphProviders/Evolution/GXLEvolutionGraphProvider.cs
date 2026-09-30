using Cysharp.Threading.Tasks;
using SEE.Game.City;
using SEE.Graphs;
using SEE.Graphs.IO.GXL;
using SEE.Graphs.Utils;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Evolution graph provider for GXL files
    /// </summary>
    public class GXLEvolutionGraphProvider : MultiGraphProvider
    {
        /// <summary>
        /// The directory where the GXL file are stored.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the directory containing the GXL files."), HideReferenceObjectPicker]
        public DataPath GXLDirectory = new();

        /// <summary>
        /// Sets the maximum number of revisions to load.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Maximum number of revisions to load."),
         FoldoutGroup(evolutionFoldoutGroup), RuntimeTab(evolutionFoldoutGroup)]
        public int MaxRevisionsToLoad = 500;

        /// <summary>
        /// Provides an evolution graph series for GXL files.
        /// </summary>
        /// <param name="graphs">The graph series of the previous provider.</param>
        /// <param name="city">The city where the evolution should be displayed.</param>
        /// <param name="changePercentage">.</param>
        /// <param name="token">Can be used to cancel the action.</param>
        /// <returns>The graph series generated from the GXL files <see cref="UniTask{T}"/>.</returns>
        public override UniTask<List<Graph>> ProvideAsync(List<Graph> graphs, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default) =>
            LoadGraphAsync(city);

        /// <summary>
        /// Loads the actual graph series from the GXL files in <see cref="GXLDirectory"/>
        /// </summary>
        /// <param name="city">The city where the evolution should be displayed.</param>
        /// <returns>The graph series generated from the GXL files.</returns>
        private async UniTask<List<Graph>> LoadGraphAsync(AbstractSEECity city)
        {
            GraphsReader reader = new();
            await reader.LoadAsync(GXLDirectory.Path, city.HierarchicalEdges, basePath: city.SourceCodeDirectory.Path,
                rootName: GXLDirectory.Path, MaxRevisionsToLoad);
            return reader.Graphs;
        }

        /// <summary>
        /// Loads and stores multiple GXL files from a directory.
        /// </summary>
        private class GraphsReader
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
                        Logging.Logger.LogError($"Graph {gxlPath} could not be loaded.\n");
                    }
                    else
                    {
                        maxRevisionsToLoad--;
                        Graphs.Add(graph);
                    }
                    if (maxRevisionsToLoad <= 0)
                    {
                        break;
                    }
                }
                Logging.Logger.LogInfo($"Number of graphs loaded: {Graphs.Count}\n");
            }
        }

        /// <summary>
        /// Returns the kind of this provider.
        /// </summary>
        /// <returns>Returns <see cref="MultiGraphProviderKind.GXLEvolution"/>.</returns>
        public override MultiGraphProviderKind GetKind()
            => MultiGraphProviderKind.GXLEvolution;

        /// <summary>
        /// Name of the foldout group for the evolution settings.
        /// </summary>
        private const string evolutionFoldoutGroup = "Evolution settings";

        #region Config I/O
        /// <summary>
        /// Label of attribute <see cref="MaxRevisionsToLoad"/> in the configuration file.
        /// </summary>
        private const string maxRevisionsToLoadLabel = "MaxRevisionsToLoad";

        /// <summary>
        /// Label of attribute <see cref="GXLDirectory"/> in the configuration file.
        /// </summary>
        private const string gxlDirectoryLabel = "GXLDirectory";

        /// <summary>
        /// Saves the attributes of this provider
        /// </summary>
        /// <param name="writer">The writer to where the attributes should be saved.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GXLDirectory.Save(writer, gxlDirectoryLabel);
            writer.Save(MaxRevisionsToLoad, maxRevisionsToLoadLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider
        /// </summary>
        /// <param name="attributes">The attributes to restore.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GXLDirectory.Restore(attributes, gxlDirectoryLabel);
            ConfigIO.Restore(attributes, maxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
        }

        #endregion
    }
}
