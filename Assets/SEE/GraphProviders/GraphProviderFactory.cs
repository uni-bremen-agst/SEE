using System;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A factory for instances of <see cref="GraphProvider"/> and its subclasses.
    /// </summary>
    static class GraphProviderFactory
    {
        /// <summary>
        /// Label for the kind of provider for <see cref="GXLGraphProvider"/> in the config file.
        /// </summary>
        private const string GXLGraphProviderKind = "GXL";
        /// <summary>
        /// Label for the kind of provider for <see cref="CSVGraphProvider"/> in the config file.
        /// </summary>
        private const string CSVGraphProviderKind = "CSV";
        /// <summary>
        /// Label for the kind of provider for <see cref="PipelineGraphProvider"/> in the config file.
        /// </summary>
        private const string PipelineGraphProviderKind = "Pipeline";

        /// <summary>
        /// Returns the label for the kind of provider of <paramref name="graphProvider"/>
        /// to be used in the config file to state which graph provider specification follows.
        /// </summary>
        /// <param name="graphProvider">a graph provider whose label is requested</param>
        /// <returns>label for <paramref name="graphProvider"/></returns>
        /// <exception cref="NotImplementedException">thrown if this method does not expect
        /// this kind of <see cref="GraphProvider"/></exception>
        internal static string GetKind(GraphProvider graphProvider)
        {
            if (graphProvider.GetType() == typeof(GXLGraphProvider))
            {
                return GXLGraphProviderKind;
            }
            if (graphProvider.GetType() == typeof(CSVGraphProvider))
            {
                return CSVGraphProviderKind;
            }
            if (graphProvider.GetType() == typeof(PipelineGraphProvider))
            {
                return PipelineGraphProviderKind;
            }
            throw new NotImplementedException($"Not implemented for {graphProvider.GetType().Name}");
        }

        internal static GraphProvider NewInstance(string kind)
        {
            return kind switch
            {
                GXLGraphProviderKind => new GXLGraphProvider(),
                CSVGraphProviderKind => new CSVGraphProvider(),
                PipelineGraphProviderKind => new PipelineGraphProvider(),
                _ => throw new NotImplementedException($"Not implemented for {kind}")
            };
        }
    }
}
