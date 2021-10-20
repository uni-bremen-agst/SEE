using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// 
    /// FIXME: Should this class rather derive from <see cref="SEECity"/>?
    /// </summary>
    public class SEECityReflexion : AbstractSEECity
    {
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
        /// The mapping of implementation nodes onto architecture nodes.
        /// Will be created from the three sub-graphs implementation, architecture and mapping.
        /// </summary>
        private Graph FullGraph = null;

        /// <summary>
        /// Redraws the full graph by first deleting its contents and then drawing it again.
        /// </summary>
        public void ReDrawGraph()
        {
            Assert.IsNotNull(FullGraph);

            DeleteGraphGameObjects();
            DrawGraph();
        }

        /// <summary>
        /// First, if a graph was already loaded (<see cref="FullGraph"/> is not null),
        /// everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="FullGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        public void LoadData()
        {
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path))
            {
                Debug.LogError("Architecture graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GxlImplementationPath.Path))
            {
                Debug.LogError("Implementation graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GxlMappingPath.Path))
            {
                //TODO: Allow empty path to create mapping from scratch
                Debug.LogError("Mapping graph path is empty.\n");
            }
            else
            {
                if (FullGraph != null)
                {
                    Reset();
                }
                Graph ArchitectureGraph = LoadGraph(GxlArchitecturePath.Path);
                Graph ImplementationGraph = LoadGraph(GxlImplementationPath.Path);
                Graph MappingGraph = LoadGraph(GxlMappingPath.Path);
                FullGraph = GenerateFullGraph(ArchitectureGraph, ImplementationGraph, MappingGraph);
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
        private static Graph GenerateFullGraph(Graph ArchitectureGraph, Graph ImplementationGraph, Graph MappingGraph)
        {
            if (ImplementationGraph == null || ArchitectureGraph == null || MappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                                    + "the full graph.");
            }

            return ImplementationGraph.MergeWith(ArchitectureGraph, "-A").MergeWith(MappingGraph, "-M");
        }
        
        /// <summary>
        /// Resets everything that is specific to a given graph, including all three sub-graphs.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            FullGraph?.Destroy();
            FullGraph = null;
        }
        
        //------------------------------------------------
        // TODO: Anything below this line not yet updated.
        //------------------------------------------------

        public void DrawGraph()
        {
            Assert.IsNotNull(FullGraph);
            //TODO: Load

            // TODO: Draw the cities next to each other.
            new GraphRenderer(this, FullGraph).Draw(gameObject);
        }

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
