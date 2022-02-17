using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Utils;
using SEE.Tools.ReflexionAnalysis;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;
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
                ArchitectureGraph.MarkGraphNodes(ArchitectureLabel);
                ImplementationGraph.MarkGraphNodes(ImplementationLabel);

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

                LoadedGraph = Assemble(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName);
                Debug.Log($"Loaded graph {LoadedGraph.Name}.\n");
            }

            #endregion
        }

        /// <summary>
        /// Maps the node <paramref name="from"/> to the node <paramref name="to"/>.
        /// This will result in a "Maps_To" edge being created between the two, and will cause
        /// the <paramref name="from"/> node to be moved to the <paramref name="to"/> node.
        /// Because "Maps_To" should be a mapping corresponding to an injective function,
        /// existing outgoing "Maps_To" edges from the <paramref name="from"/> node and incoming "Maps_To" edges to the
        /// <paramref name="to"/> node will be replaced.
        /// </summary>
        /// <param name="from">The node to map from.</param>
        /// <param name="to">The node to map to.</param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="from"/> and <paramref name="to"/> are not in the same graph, or if
        /// the mapping isn't from an implementation node to an architecture node.
        /// </exception>
        public static void Map(Node from, Node to)
        {
            from.AssertNotNull(nameof(from));
            to.AssertNotNull(nameof(to));
            if (!from.HasToggle(ImplementationLabel) || !to.HasToggle(ArchitectureLabel))
            {
                throw new ArgumentException($"{nameof(from)} must be an implementation node, and"
                                            + $"{nameof(to)} must be an architecture node!");
            }

            if (!ReferenceEquals(from.ItsGraph, to.ItsGraph))
            {
                throw new ArgumentException("The two nodes must be in the same graph!");
            }
            
            RemoveOutgoing();
            
            RemoveIncoming();

            Edge mapEdge = new Edge(from, to, Reflexion.MapsToType);
            from.ItsGraph.AddEdge(mapEdge);

            #region Local Functions
            
            void RemoveOutgoing()
            {
                // Note: We use "Single" instead of "First" because it's an invariant that "Maps_To" is a function.
                Edge outgoing = from.Outgoings.SingleOrDefault(x => x.Type == Reflexion.MapsToType);
                if (outgoing != null && from.ItsGraph.ContainsEdge(outgoing))
                {
                    from.ItsGraph.RemoveEdge(outgoing);
                }
            }

            void RemoveIncoming()
            {
                // Note: We use "Single" instead of "First" because it's an invariant that "Maps_To" is injective.
                Edge incoming = to.Incomings.SingleOrDefault(x => x.Type == Reflexion.MapsToType);
                if (incoming != null && to.ItsGraph.ContainsEdge(incoming))
                {
                    to.ItsGraph.RemoveEdge(incoming);
                }
            }
            
            #endregion
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
                (Graph implementation, Graph architecture, Graph mapping) = LoadedGraph.Disassemble();
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