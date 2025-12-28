using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils.Config;

namespace SEE.GraphProviders
{
    /// <summary>
    /// MultiGraphProvider is a graph provider for returning a series of graphs (e.g., for an evolution city).
    /// </summary>
    public abstract class MultiGraphProvider : GraphProvider<List<Graph>, MultiGraphProviderKind>
    {
        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">Dictionary of attributes from which to retrieve the settings.</param>
        /// <param name="label">The label for the settings (a key in <paramref name="attributes"/>).</param>
        public static MultiGraphProvider Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                return RestoreProvider(values);
            }
            else
            {
                throw new Exception($"A graph provider could not be found under the label {label}.");
            }
        }

        /// <summary>
        /// Restores the graph provider's attributes from <paramref name="values"/>.
        /// The parameter <paramref name="values"/> is assumed to already be the list
        /// of values. No further label look-up will be needed.
        /// </summary>
        /// <param name="values">List of values to be used to restore a graph provider.</param>
        /// <returns>The resulting graph provider that was restored; it will have the
        /// <paramref name="values"/>.</returns>
        /// <exception cref="Exception">Thrown in case the values are malformed.</exception>
        /// <remarks>This method just creates a new instance using the 'kind' attribute
        /// via the <see cref="GraphProviderFactory"/>. The actual restoration of that
        /// new instance's attributes ist deferred to the subclasses, which need to
        /// implement <see cref="RestoreAttributes(Dictionary{string, object})"/>.</remarks>
        protected internal static MultiGraphProvider RestoreProvider(Dictionary<string, object> values)
        {
            MultiGraphProviderKind kind = MultiGraphProviderKind.MultiPipeline;
            if (ConfigIO.RestoreEnum(values, kindLabel, ref kind))
            {
                MultiGraphProvider provider = GraphProviderFactory.NewMultiGraphProviderInstance(kind);
                provider.RestoreAttributes(values);
                return provider;
            }
            else
            {
                throw new Exception($"Specification of graph provider is malformed: label {kindLabel} is missing.");
            }
        }
    }
}
