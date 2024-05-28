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
    /// <summary>
    /// Evolution graph provider for gxl files 
    /// </summary>
    public class GXLEvolutionGraphProvider : MultiGraphProvider
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

        /// <summary>
        /// The directory where the GXL file are stored
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker]
        public DirectoryPath GXLDirectory = new();

        /// <summary>
        /// Sets the maximum number of revisions to load.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Maximum number of revisions to load."),
         FoldoutGroup(evolutionFoldoutGroup), RuntimeTab(evolutionFoldoutGroup)]
        public int MaxRevisionsToLoad = 500; // serialized by Unity

        /// <summary>
        /// Provides an evolution graph series for gxl files
        /// </summary>
        /// <param name="graphs">The graph series of the previous provider</param>
        /// <param name="city">The city where the evolution should be displayed</param>
        /// <param name="changePercentage"></param>
        /// <param name="token">Can be used to cancel the action</param>
        /// <returns>The graph series generated from the gxl files <see cref="UniTask{T}"/></returns>
        public override UniTask<List<Graph>> ProvideAsync(List<Graph> graphs, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default) =>
            LoadGraph(city);

        /// <summary>
        /// Loads the actual graph series from the gxl files in <see cref="GXLDirectory"/>
        /// </summary>
        /// <param name="city">The city where the evolution should be displayed</param>
        /// <returns>The graph series generated from the gxl files</returns>
        private async UniTask<List<Graph>> LoadGraph(AbstractSEECity city)
        {
            GraphsReader reader = new();
            await reader.LoadAsync(GXLDirectory.Path, city.HierarchicalEdges, basePath: city.SourceCodeDirectory.Path,
                rootName: GXLDirectory.Path, MaxRevisionsToLoad);
            return reader.Graphs;
        }

        /// <summary>
        /// Returns the kind of this provider
        /// </summary>
        /// <returns>returns <see cref="MultiGraphProviderKind.GXLEvolution"/></returns>
        public override MultiGraphProviderKind GetKind()
            => MultiGraphProviderKind.GXLEvolution;

        /// <summary>
        /// Saves the attributes of this provider
        /// </summary>
        /// <param name="writer">The writer to where the attributes should be saved</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GXLDirectory.Save(writer, gxlDirectoryLabel);
            writer.Save(MaxRevisionsToLoad, maxRevisionsToLoadLabel);
        }

        /// <summary>
        /// Restors the attributes of this provider
        /// </summary>
        /// <param name="attributes">The attributes to restore</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GXLDirectory.Restore(attributes, gxlDirectoryLabel);
            ConfigIO.Restore(attributes, maxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
        }
    }
}
