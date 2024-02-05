using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides a reflexion graph based on three GXL files (architecture,
    /// implementation, mapping).
    /// </summary>
    [Serializable]
    public class ReflexionGraphProvider : GraphProvider
    {
        /// <summary>
        /// The path to the GXL file containing the architecture.
        /// </summary>
        [Tooltip("Path to the GXL file containing the architecture."), HideReferenceObjectPicker]
        public FilePath Architecture = new();

        /// <summary>
        /// The path to the GXL file containing the implementation.
        /// </summary>
        [Tooltip("Path to the GXL file containing the implementation."), HideReferenceObjectPicker]
        public FilePath Implementation = new();

        /// <summary>
        /// The path to the GXL file containing the mapping.
        /// </summary>
        [Tooltip("Path to the GXL file containing the mapping. Can be left undefined."), HideReferenceObjectPicker]
        public FilePath Mapping = new();

        /// <summary>
        /// Name of resulting reflexion city.
        /// </summary>
        [Tooltip("The name of the resulting reflexion city.")]
        public string CityName = "Reflexion Analysis";

        public override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
            Graph architectureGraph = LoadGraph(Architecture.Path, city);
            Graph implementationGraph = LoadGraph(Implementation.Path, city);
            Graph mappingGraph;
            if (string.IsNullOrEmpty(Mapping.Path))
            {
                Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                /// The mapping graph may contain nodes and edges from the implementation. Possibly, their
                /// <see cref="GraphElement.AbsolutePlatformPath()"/> will be retrieved. That is why we
                /// will set the base path to <see cref="ProjectPath.Path"/>.
                mappingGraph = new Graph(city.SourceCodeDirectory.Path);
            }
            else
            {
                mappingGraph = LoadGraph(Mapping.Path, city);
            }

            return UniTask.FromResult<Graph>(new ReflexionGraph(implementationGraph, architectureGraph, mappingGraph, CityName));
        }

        /// <summary>
        /// Returns a graph loaded from the GXL file with given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">the path of the GXL data from which to load</param>
        /// <param name="city">where the <see cref="AbstractSEECity.HierarchicalEdges"/>
        /// and <see cref="AbstractSEECity.SourceCodeDirectory"/> will be retrieved</param>
        /// <returns>loaded graph</returns>
        /// <exception cref="ArgumentException">thrown if <paramref name="path"/> is null or empty
        /// or does not exist</exception>
        private Graph LoadGraph(string path, AbstractSEECity city)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Empty GXL path.\n");
            }
            if (!File.Exists(path))
            {
                throw new ArgumentException($"File {path} does not exist.\n");
            }
            GraphReader graphCreator = new(path, city.HierarchicalEdges,
                                           basePath: city.SourceCodeDirectory.Path,
                                           logger: new SEELogger());
            graphCreator.Load();
            return graphCreator.GetGraph();
        }

        #region Configuration file input/output

        /// <summary>
        /// Label of attribute <see cref="Architecture"/> in the configuration file.
        /// </summary>
        private const string architectureLabel = "Architecture";

        /// <summary>
        /// Label of attribute <see cref="Implementation"/> in the configuration file.
        /// </summary>
        private const string implementationLabel = "Implementation";

        /// <summary>
        /// Label of attribute <see cref="Mapping"/> in the configuration file.
        /// </summary>
        private const string mappingLabel = "Mapping";

        /// <summary>
        /// Label of attribute <see cref="CityName"/> in the configuration file.
        /// </summary>
        private const string cityNameLabel = "CityName";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Architecture.Save(writer, architectureLabel);
            Implementation.Save(writer, implementationLabel);
            Mapping.Save(writer, mappingLabel);
            writer.Save(CityName, cityNameLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            Architecture.Restore(attributes, architectureLabel);
            Implementation.Restore(attributes, implementationLabel);
            Mapping.Restore(attributes, mappingLabel);
            ConfigIO.Restore(attributes, cityNameLabel, ref CityName);
        }

        #endregion
    }
}
