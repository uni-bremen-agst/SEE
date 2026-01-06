using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SEE.GraphProviders
{
    /// <summary>
    /// The <see cref="GraphProvider"/> class is an abstract base class that provides a framework
    /// for creating and managing graph data. Concrete subclasses of <see cref="GraphProvider"/>
    /// are responsible for providing a graph (or a list of graphs) based on the input graph
    /// (or a list of graphs) by implementing the method
    /// <see cref="ProvideAsync(x,AbstractSEECity,Action{float},CancellationToken)"/>.
    ///
    /// It is not recomended to inherit a class directly from <see cref="GraphProvider"/>.
    /// Instead, you should use <see cref="SingleGraphProvider"/> and <see cref="MultiGraphProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of data which should be provided (e.g. a single <see cref="Graph"/> for
    /// simple code cities or a list of <see cref="Graph}"/>s for evolution cities)</typeparam>
    /// <typeparam name="K">
    /// This type specifies the graph provider kind enum type.
    /// This can either be a <see cref="SingleGraphProviderKind"/> or <see cref="MultiGraphProviderKind"/>.
    /// This type will be returned by <see cref="GetKind"/>
    /// </typeparam>
    public abstract class GraphProvider<T, K> where K : Enum
    {
        /// <summary>
        /// Yields a new graph based on the input <paramref name="graph"/>.
        /// The input <paramref name="graph"/> may be empty. Subclasses are free to
        /// ignore the parameter <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Input graph.</param>
        /// <param name="city">Settings possibly necessary to provide a graph.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Provided graph based on <paramref name="graph"/>.</returns>
        public abstract UniTask<T> ProvideAsync(T graph, AbstractSEECity city,
                                                Action<float> changePercentage = null,
                                                CancellationToken token = default);

        /// <summary>
        /// The fold out group for the graph provider in the runtime configuration
        /// of a code city.
        /// </summary>
        protected const string GraphProviderFoldoutGroup = "Data";

        /// <summary>
        /// Returns the kind of graph provider.
        /// </summary>
        /// <returns>Kind of graph provider.</returns>
        public abstract K GetKind();

        #region Config I/O

        /// <summary>
        /// Saves the settings in the configuration file.
        ///
        /// Because we have different types of graph providers, a key-value
        /// pair will be stored first that specifies the type of provider.
        /// Only then, its attributes follow. For instance, a <see cref="GXLSingleGraphProvider"/>
        /// would be emitted as follows:
        ///
        /// {
        ///   kind : "GXL";
        ///   path : {
        ///      Root : "Absolute";
        ///      RelativePath : "";
        ///      AbsolutePath : "mydir/myfile.gxl";
        /// };
        ///
        /// where 'kind' is the label for the kind of graph provider and "GXL" in this
        /// example would be used for a <see cref="GXLSingleGraphProvider"/>. What value is
        /// used for 'kind' is decided by <see cref="GraphProviderFactory"/>.
        /// </summary>
        /// <param name="writer">To be used for writing the settings.</param>
        /// <param name="label">The outer label grouping the settings.</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(GetKind().ToString(), kindLabel);
            SaveAttributes(writer);
            writer.EndGroup();
        }

        /// <summary>
        /// Subclasses must implement this so save their attributes. This class takes
        /// care only to begin and end the grouping and to emit the key-value pair
        /// for the 'kind'.
        /// </summary>
        /// <param name="writer">To be used for writing the settings.</param>
        protected abstract void SaveAttributes(ConfigWriter writer);

        /// <summary>
        /// Must be implemented by subclasses to restore their attributes.
        /// </summary>
        /// <param name="attributes">Attributes that should be restored.</param>
        protected abstract void RestoreAttributes(Dictionary<string, object> attributes);

        /// <summary>
        /// The label for kind of graph provider in the configuration file.
        /// </summary>
        protected const string kindLabel = "kind";

#endregion
    }
}
