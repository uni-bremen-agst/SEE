using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// NOTE: It is assumed the implementation and architecture graphs are not edited!
    /// TODO: In fact, we should disallow this programmatically, too.
    /// </summary>
    public class SEEReflexionCity : SEECity
    {
        /// <summary>
        /// Label for the architecture toggle added to each graph element of the architecture city.
        /// </summary>
        private const string ArchitectureLabel = "Architecture";
        
        /// <summary>
        /// Label for the implementation toggle added to each graph element of the implementation city.
        /// </summary>
        private const string ImplementationLabel = "Implementation";

        /// <summary>
        /// The path to the GXL file containing the implementation graph data.
        /// </summary>
        public DataPath GxlImplementationPath = new DataPath();

        /// <summary>
        /// The path to the GXL file containing the architecture graph data.
        /// </summary>
        public DataPath GxlArchitecturePath = new DataPath();

        /// <summary>
        /// The path to the GXL file containing the mapping graph data.
        /// </summary>
        public DataPath GxlMappingPath = new DataPath();
        
        /// <summary>
        /// The path to the CSV file containing the implementation metric data.
        /// </summary>
        public DataPath CsvImplementationPath = new DataPath();

        /// <summary>
        /// The path to the CSV file containing the architecture metric data.
        /// </summary>
        public DataPath CsvArchitecturePath = new DataPath();

        /// <summary>
        /// Name of this code city.
        /// </summary>
        public string CityName = "Reflexion Analysis";

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        public override void LoadData()
        {
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path))
            {
                Debug.LogError("Architecture graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GxlImplementationPath.RootPath))
            {
                Debug.LogError("Implementation graph path is empty.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }

                LoadAllGraphs().Forget(); // needs to be async call due to metric retrieval
            }

            #region Local Functions
            
            async UniTaskVoid LoadAllGraphs()
            {
                Graph ArchitectureGraph = LoadGraph(GxlArchitecturePath.Path, "");
                Graph ImplementationGraph = LoadGraph(GxlImplementationPath.Path, "");
                Graph MappingGraph;
                if (string.IsNullOrEmpty(GxlMappingPath.Path))
                {
                    Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                    MappingGraph = new Graph();
                }
                else
                {
                    MappingGraph = LoadGraph(GxlMappingPath.Path, "");
                }

                // MappingGraph needn't be labeled, as any remaining/new edge automatically belongs to it
                MarkGraphNodes(ArchitectureGraph, ArchitectureLabel);
                MarkGraphNodes(ImplementationGraph, ImplementationLabel);

                // We collect the tasks here so we can wait on them both at the same time instead of sequentially
                IList<UniTask> tasks = new List<UniTask>();
                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }
                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }
                await UniTask.WhenAll(tasks);
                
                LoadedGraph = GenerateFullGraph(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName);
                Debug.Log($"Loaded graph {LoadedGraph.Name}.\n");
            }

            #endregion
        }
        
        /// <summary>
        /// Adds a toggle attribute <paramref name="label"/> to each node and edge of the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes and edges shall be marked with a toggle attribute</param>
        /// <param name="label">The value of the toggle attribute</param>
        private static void MarkGraphNodes(Graph graph, string label)
        {
            IEnumerable<GraphElement> graphElements = graph.Nodes().Concat<GraphElement>(graph.Edges());
            foreach (GraphElement graphElement in graphElements)
            {
                graphElement.SetToggle(label);
            }
        }

        /// <summary>
        /// Generates the full graph from the three sub-graphs <see cref="ImplementationGraph"/>,
        /// <see cref="ArchitectureGraph"/> and <see cref="MappingGraph"/> by combining them into one, returning
        /// the result. Note that the name of the three graphs may be modified.
        /// 
        /// Pre-condition: <see cref="ImplementationGraph"/>, <see cref="ArchitectureGraph"/> and
        /// <see cref="MappingGraph"/> are not <c>null</c> (i.e. have been loaded).
        /// </summary>
        /// <returns>Full graph obtained by combining architecture, implementation and mapping</returns>
        private static Graph GenerateFullGraph(Graph ArchitectureGraph, Graph ImplementationGraph, Graph MappingGraph, 
                                               string Name)
        {
            if (ImplementationGraph == null || ArchitectureGraph == null || MappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                            + "the full graph.");
            }

            // We set the name for the implementation graph, because its name will be used for the merged graph.
            ImplementationGraph.Name = Name;

            // We merge architecture and implementation first.
            // If there are duplicate IDs, try to remedy this by appending a suffix to the ID.
            List<string> graphsOverlap = GraphsOverlap(ImplementationGraph, ArchitectureGraph);
            string archSuffix = null;
            if (graphsOverlap.Count > 0)
            {
                archSuffix = "-A";
                Debug.LogWarning($"Overlapping graph elements found, will append '{archSuffix}' suffix."
                                 + $"Offending elements: {string.Join(", ", graphsOverlap)}");
            }
            Graph mergedGraph = ImplementationGraph.MergeWith(ArchitectureGraph, archSuffix);
            
            // Then we add the mappings, again checking if any IDs overlap.
            graphsOverlap = GraphsOverlap(mergedGraph, MappingGraph);
            string mapSuffix = null;
            if (graphsOverlap.Count > 0)
            {
                mapSuffix = "-M";
                Debug.LogWarning($"Overlapping graph elements found, will append '{mapSuffix}' suffix."
                                 + $"Offending elements: {string.Join(", ", graphsOverlap)}");
            }
            return mergedGraph.MergeWith(MappingGraph, mapSuffix);

            
            #region Local Functions

            // Returns any intersecting elements (node IDs, edge IDs) between the two given graphs.
            List<string> GraphsOverlap(Graph aGraph, Graph anotherGraph) => NodeIntersection(aGraph, anotherGraph).Concat(EdgeIntersection(aGraph, anotherGraph)).ToList();

            // Returns the intersection of the node IDs of the two graphs.
            IEnumerable<string> NodeIntersection(Graph aGraph, Graph anotherGraph) => aGraph.Nodes().Select(x => x.ID).Intersect(anotherGraph.Nodes().Select(x => x.ID));

            // Returns the intersection of the edge IDs of the two graphs.
            IEnumerable<string> EdgeIntersection(Graph aGraph, Graph anotherGraph) => aGraph.Edges().Select(x => x.ID).Intersect(anotherGraph.Edges().Select(x => x.ID));
            
            #endregion
        }

        /// <summary>
        /// Disassembles the given <paramref name="FullGraph"/> into implementation, architecture, and mapping graphs,
        /// and returns them in this order.
        ///
        /// Pre-condition: The given graph must have been assembled by <see cref="GenerateFullGraph"/>.
        /// </summary>
        /// <param name="FullGraph">Graph generated by <see cref="GenerateFullGraph"/></param>
        /// <returns>3-tuple consisting of (implementation, architecture, mapping) graph</returns>
        private static (Graph implementation, Graph architecture, Graph mapping) DisassembleFullGraph(Graph FullGraph)
        {
            Graph ImplementationGraph = FullGraph.SubgraphByToggleAttributes(new[] {ImplementationLabel});
            Graph ArchitectureGraph = FullGraph.SubgraphByToggleAttributes(new[] {ArchitectureLabel});
            // Mapping graph elements will have neither architecture nor implementation label.
            // However, we must not include the artificial root node, if one was generated for this graph.
            Graph MappingGraph = FullGraph.SubgraphBy(x => !x.HasToggle(ImplementationLabel) 
                                                           && !x.HasToggle(ArchitectureLabel) 
                                                           && x.Type != GraphRenderer.RootType);
            return (ImplementationGraph, ArchitectureGraph, MappingGraph);
        }

        public override void SaveData()
        {
            IList<string> NoPathGraphs = new[]
            {
                GxlArchitecturePath.Path, GxlImplementationPath.Path, GxlMappingPath.Path
            }.Where(string.IsNullOrEmpty).ToList();
            if (NoPathGraphs.Count > 0)
            {
                Debug.LogError($"Couldn't find any graph at path{(NoPathGraphs.Count > 1 ? "s" : "")} " +
                    string.Join(", ", NoPathGraphs) + ".\n");
            }
            else
            {
                string hierarchicalType = HierarchicalEdges.First();
                (Graph implementation, Graph architecture, Graph mapping) = DisassembleFullGraph(LoadedGraph);
                GraphWriter.Save(GxlArchitecturePath.Path, architecture, hierarchicalType);
                Debug.Log($"Architecture graph saved at {GxlArchitecturePath.Path}.\n");
                GraphWriter.Save(GxlImplementationPath.Path, implementation, hierarchicalType);
                Debug.Log($"Implementation graph saved at {GxlImplementationPath.Path}.\n");
                GraphWriter.Save(GxlMappingPath.Path, mapping, hierarchicalType);
                Debug.Log($"Mapping graph saved at {GxlMappingPath.Path}.\n");
            }
        }

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GxlArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string GxlArchitectureLabel = "ArchitectureGXL";
        
        /// <summary>
        /// Label of attribute <see cref="GxlImplementationPath"/> in the configuration file.
        /// </summary>
        private const string GxlImplementationLabel = "ImplementationGXL";
        
        /// <summary>
        /// Label of attribute <see cref="GxlMappingPath"/> in the configuration file.
        /// </summary>
        private const string GxlMappingLabel = "MappingGXL";

        /// <summary>
        /// Label of attribute <see cref="CsvArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string CsvArchitectureLabel = "ArchitectureCSV";
        
        /// <summary>
        /// Label of attribute <see cref="CsvImplementationPath"/> in the configuration file.
        /// </summary>
        private const string CsvImplementationLabel = "ImplementationCSV";

        /// <summary>
        /// Label of attribute <see cref="CityName"/> in the configuration file.
        /// </summary>
        private const string CityNameLabel = "CityName";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GxlArchitecturePath.Save(writer, GxlArchitectureLabel);
            GxlImplementationPath.Save(writer, GxlImplementationLabel);
            GxlMappingPath.Save(writer, GxlMappingLabel);
            CsvArchitecturePath.Save(writer, CsvArchitectureLabel);
            CsvImplementationPath.Save(writer, CsvImplementationLabel);
            writer.Save(CityName, CityNameLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GxlArchitecturePath.Restore(attributes, GxlArchitectureLabel);
            GxlImplementationPath.Restore(attributes, GxlImplementationLabel);
            GxlMappingPath.Restore(attributes, GxlMappingLabel);
            CsvArchitecturePath.Restore(attributes, CsvArchitectureLabel);
            CsvImplementationPath.Restore(attributes, CsvImplementationLabel);
            ConfigIO.Restore(attributes, CityNameLabel, ref CityName);
        }
    }
}