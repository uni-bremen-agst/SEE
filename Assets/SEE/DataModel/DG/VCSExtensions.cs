using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Provides convenience extension properties for the VCS related attributes.
    /// </summary>
    public static class VCSExtensions
    {
        /// <summary>
        /// The attribute name for the commitID..
        /// </summary>
        public const string CommitIDAttribute = "CommitID";

        /// <summary>
        /// Returns the commit ID of the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph whose commit ID is requested</param>
        public static bool TryGetCommitID(this Graph graph, out string commitID)
        {
            return graph.TryGetString(CommitIDAttribute, out commitID);
        }

        /// <summary>
        /// Sets the commit ID of the <paramref name="graph"/> to <paramref name="value"/>
        /// </summary>
        /// <param name="graph">graph whose commit ID is to be set</param>
        /// <param name="value">value to be set</param>
        public static void CommitID(this Graph graph, string value)
        {
            graph.SetString(CommitIDAttribute, value);
        }

        /// <summary>
        /// Returns the commit ID of the <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="graphElement">graph element whose commit ID is requested</param>
        public static bool TryGetCommitID(this GraphElement graphElement, out string commitID)
        {
            return graphElement.ItsGraph.TryGetString(CommitIDAttribute, out commitID);
        }

        /// <summary>
        /// The attribute name for the repository path.
        /// </summary>
        public const string RepositoryPathAttribute = "RepositoryPath";

        /// <summary>
        /// Returns the repository path of the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph whose repository path is requested</param>
        public static bool TryGetRepositoryPath(this Graph graph, out string repositoryPath)
        {
            return graph.TryGetString(RepositoryPathAttribute, out repositoryPath);
        }

        /// <summary>
        /// Sets the repository path of the <paramref name="graph"/> to <paramref name="value"/>
        /// </summary>
        /// <param name="graph">graph whose repository path is to be set</param>
        /// <param name="value">value to be set</param>
        [Obsolete("Will be removed soon.")]
        public static void RepositoryPath(this Graph graph, string value)
        {
            // FIXME: Removed this method.
            graph.SetString(RepositoryPathAttribute, value);
        }

        /// <summary>
        /// Returns the repository path of the <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="graphElement">graph element whose repository path is requested</param>
        [Obsolete("Will be removed soon.")]
        public static bool TryGetRepositoryPath(this GraphElement graphElement, out string repositoryPath)
        {
            // FIXME: Removed this method.
            return graphElement.ItsGraph.TryGetString(RepositoryPathAttribute, out repositoryPath);
        }
    }
}