using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// </summary>
    public class SEECityReflexion : SEECity
    {
        private const string ArchitectureAttributeLabel = "Architecture";

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

                Graph ArchitectureGraph = LoadGraph(GxlArchitecturePath.Path);
                Graph ImplementationGraph = LoadGraph(GxlImplementationPath.Path);
                Graph MappingGraph;
                if (string.IsNullOrEmpty(GxlMappingPath.Path))
                {
                    Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                    MappingGraph = new Graph();
                }
                else
                {
                    MappingGraph = LoadGraph(GxlMappingPath.Path);
                }

                LoadedGraph = GenerateFullGraph(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName);
                Debug.Log($"Loaded graph {LoadedGraph.Name}");
            }
        }

        /// <summary>
        /// Generates the full graph from the three sub-graphs <see cref="ImplementationGraph"/>,
        /// <see cref="ArchitectureGraph"/> and <see cref="MappingGraph"/> by combining them into one, returning
        /// the result.
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

            foreach (Node node in ArchitectureGraph.Nodes())
            {
                node.ToggleAttributes.Add(ArchitectureAttributeLabel);
            }

            // We set the name for the implementation graph, because its name will be used for the merged graph.
            ImplementationGraph.Name = Name;

            // We merge architecture and implementation first.
            // If there are duplicate IDs, try to remedy this by appending a suffix to the ID.
            string archSuffix = GraphsOverlap(ImplementationGraph, ArchitectureGraph) ? "-A" : null;
            Graph mergedGraph = ImplementationGraph.MergeWith(ArchitectureGraph, archSuffix);
            
            // Then we add the mappings, again checking if any IDs overlap.
            string mapSuffix = GraphsOverlap(mergedGraph, MappingGraph) ? "-M" : null;
            return mergedGraph.MergeWith(MappingGraph, mapSuffix);

            #region Local Functions

            // Checks if the given graphs share any IDs in nodes or edges
            bool GraphsOverlap(Graph aGraph, Graph anotherGraph) => 
                NodesOverlap(aGraph, anotherGraph) || EdgesOverlap(aGraph, anotherGraph);

            // Checks if the given graphs share any node IDs
            bool NodesOverlap(Graph aGraph, Graph anotherGraph) => 
                new HashSet<string>(aGraph.Nodes().Select(x => x.ID))
                    .Overlaps(anotherGraph.Nodes().Select(x => x.ID));
            
            // Checks if the given graphs share any edge IDs
            bool EdgesOverlap(Graph aGraph, Graph anotherGraph) => 
                new HashSet<string>(aGraph.Edges().Select(x => x.ID))
                    .Overlaps(anotherGraph.Nodes().Select(x => x.ID));
            
            #endregion
        }

        private static (Graph, Graph, Graph) DisassembleFullGraph(Graph FullGraph)
        {
            //TODO
            throw new NotImplementedException();
        }

        public override void SaveData()
        {
            //TODO
            throw new NotImplementedException();
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