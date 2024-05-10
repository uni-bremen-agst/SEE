using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GraphProviders
{
    public class GXLEvolutionGraphProvider : GraphProvider<List<Graph>>
    {
        private const string evolutionFoldoutGroup = "Evolution settings";

        /// <summary>
        /// Label of attribute <see cref="MaxRevisionsToLoad"/> in the configuration file.
        /// </summary>
        private const string maxRevisionsToLoadLabel = "MaxRevisionsToLoad";


        /// <summary>
        /// Label of attribute <see cref="GXLDirectory"/> in the configuration file.
        /// </summary>
        private const string gxlDirectoryLabel = "GXLDirectory";

        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker]
        public DirectoryPath GXLDirectory = new();

        /// <summary>
        /// Sets the maximum number of revisions to load.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Maximum number of revisions to load."),
         FoldoutGroup(evolutionFoldoutGroup), RuntimeTab(evolutionFoldoutGroup)]
        public int MaxRevisionsToLoad = 500; // serialized by Unity

        public override UniTask<List<Graph>> ProvideAsync(List<Graph> graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default) =>
            UniTask.FromResult(LoadGraph(city));

        private List<Graph> LoadGraph(AbstractSEECity city)
        {
            GraphsReader reader = new();
            reader.Load(GXLDirectory.Path, city.HierarchicalEdges, basePath: city.SourceCodeDirectory.Path,
                rootName: GXLDirectory.Path, MaxRevisionsToLoad);
            return reader.Graphs;
        }


        public override GraphProviderKind GetKind() => GraphProviderKind.GXLEvolution;

        protected override void SaveAttributes(ConfigWriter writer)
        {
            GXLDirectory.Save(writer, gxlDirectoryLabel);
            writer.Save(MaxRevisionsToLoad, maxRevisionsToLoadLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GXLDirectory.Restore(attributes, gxlDirectoryLabel);
            ConfigIO.Restore(attributes, maxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
        }
    }
}