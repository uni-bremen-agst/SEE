using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SEE.GameObjects;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides a reflexion graph based on three GXL files (architecture,
    /// implementation, mapping).
    /// </summary>
    [Serializable]
    public class ReflexionGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The path to the GXL file containing the architecture.
        /// </summary>
        [Tooltip("Path to the GXL file containing the architecture."), HideReferenceObjectPicker]
        public DataPath Architecture = new();

        /// <summary>
        /// The path to the GXL file containing the implementation.
        /// </summary>
        [Tooltip("Path to the GXL file containing the implementation."), HideReferenceObjectPicker]
        public DataPath Implementation = new();

        /// <summary>
        /// The path to the GXL file containing the mapping.
        /// </summary>
        [Tooltip("Path to the GXL file containing the mapping. Can be left undefined."), HideReferenceObjectPicker]
        public DataPath Mapping = new();

        /// <summary>
        /// Name of resulting reflexion city.
        /// </summary>
        [Tooltip("The name of the resulting reflexion city.")]
        public string CityName = "Reflexion Analysis";

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.Reflexion;
        }

        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                    Action<float> changePercentage = null,
                                                    CancellationToken token = default)
        {
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
            Graph architectureGraph = await LoadGraphAsync(Architecture, city, token);
            changePercentage?.Invoke(0.33f);
            Graph implementationGraph = await LoadGraphAsync(Implementation, city, token);
            changePercentage?.Invoke(0.66f);
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
                mappingGraph = await LoadGraphAsync(Mapping, city, token);
                changePercentage?.Invoke(1.0f);
            }

            // If the initial reflexion city is loaded, replace the initial key value in the name
            // with the selected city name.
            if (city.ConfigurationPath.Path.Equals(new DataPath(CitySelectionManager.InitialReflexionCityPath).Path))
            {
                CityName = CityName.Replace("initial", city.gameObject.name);
                architectureGraph.Name = architectureGraph.Name.Replace("initial", city.gameObject.name);
                implementationGraph.Name = implementationGraph.Name.Replace("initial", city.gameObject.name);
            }

            return new ReflexionGraph(implementationGraph, architectureGraph, mappingGraph, CityName);
        }

        /// <summary>
        /// Returns a graph loaded from the GXL file with given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">the path of the GXL data from which to load</param>
        /// <param name="city">where the <see cref="AbstractSEECity.HierarchicalEdges"/>
        /// and <see cref="AbstractSEECity.SourceCodeDirectory"/> will be retrieved</param>
        /// <param name="token">token with which the loading can be cancelled</param>
        /// <returns>loaded graph</returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="path"/> is null</exception>
        internal async UniTask<Graph> LoadGraphAsync(DataPath path, AbstractSEECity city, CancellationToken token = default)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            return await GraphReader.LoadAsync(path, city.HierarchicalEdges, city.SourceCodeDirectory.Path);
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
