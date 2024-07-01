using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;

namespace SEE.GraphProviders
{
    /// <summary>
    /// SingleGraphProvider is a graph provider for returning a single graph.
    ///
    /// This kind of graph provider is used in <see cref="SEECity"/>.
    /// It can also be used in <see cref="SingleGraphPipelineProvider"/> to create a pipeline.
    /// </summary>
    public abstract class SingleGraphProvider : GraphProvider<Graph, SingleGraphProviderKind>
    {
        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        public static SingleGraphProvider Restore(Dictionary<string, object> attributes, string label)
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
        /// <param name="values">list of values to be used to restore a graph provider</param>
        /// <returns>the resulting graph provider that was restored; it will have the
        /// <paramref name="values"/></returns>
        /// <exception cref="Exception">thrown in case the values are malformed</exception>
        /// <remarks>This method just creates a new instance using the 'kind' attribute
        /// via the <see cref="GraphProviderFactory"/>. The actual restoration of that
        /// new instance's attributes ist deferred to the subclasses, which need to
        /// implement <see cref="RestoreAttributes(Dictionary{string, object})"/>.</remarks>
        protected internal static SingleGraphProvider RestoreProvider(Dictionary<string, object> values)
        {
            SingleGraphProviderKind kind = SingleGraphProviderKind.SinglePipeline;
            if (ConfigIO.RestoreEnum(values, kindLabel, ref kind))
            {
                SingleGraphProvider provider = GraphProviderFactory.NewSingleGraphProviderInstance(kind);
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
