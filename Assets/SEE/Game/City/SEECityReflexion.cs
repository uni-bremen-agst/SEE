using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SEECityReflexion : SEECity
    {
        private const string ArchitectureLabel = "Architecture";
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
            else if (string.IsNullOrEmpty(GxlImplementationPath.Path))
            {
                Debug.LogError("Implementation graph path is empty.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }

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
                
                LoadedGraph = GenerateFullGraph(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName);
                Debug.Log($"Loaded graph {LoadedGraph.Name}");
            }
        }
        
        /// <summary>
        /// Adds a toggle attribute <paramref name="label"/> to each node and edge of the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes and edges shall be marked with a toggle attribute</param>
        /// <param name="label">The value of the toggle attribute</param>
        public static void MarkGraphNodes(Graph graph, string label)
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
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path)
                || string.IsNullOrEmpty(GxlImplementationPath.Path)
                || string.IsNullOrEmpty(GxlMappingPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                string hierarchicalType = HierarchicalEdges.First();
                (Graph implementation, Graph architecture, Graph mapping) = DisassembleFullGraph(LoadedGraph);
                GraphWriter.Save(GxlImplementationPath.Path, implementation, hierarchicalType);
                GraphWriter.Save(GxlArchitecturePath.Path, architecture, hierarchicalType);
                GraphWriter.Save(GxlMappingPath.Path, mapping, hierarchicalType);
                Debug.Log("Three graphs written to disk.\n");
            }
            //TODO: Also write test for reading three GXLs as reflexion city, then writing them, then reading them again
        }

        //------------------------------------------------
        // TODO: Anything below this line not yet updated.
        //------------------------------------------------

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            // If any attribute is added to this class that should be contained in the
            // configuration file, then do not forget to add the necessary
            // statements here.
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            // If any attribute is added to this class that should be restored from the
            // configuration file, then do not forget to add the necessary
            // statements here.
        }
    }
}