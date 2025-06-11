using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading;
using SEE.Utils;
using SEE.VCS;
using SEE.GraphProviders.VCS;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Creates a graph based on the content of a version control system
    /// between two commits.
    /// Nodes represent directories and files. Their nesting corresponds to
    /// the directory structure of the repository. Files are leaf nodes.
    /// File nodes contain metrics that can be gathered based on a simple
    /// lexical analysis, such as Halstead, McCabe and lines of code, as
    /// well as from the version control system, such as number of developers,
    /// number of commits, or code churn.
    /// </summary>
    internal class BetweenCommitsGraphProvider : GitGraphProvider
    {
        /// <summary>
        /// The commit id.
        /// </summary>
        [ShowInInspector, Tooltip("The commit id for which to generate the graph."), HideReferenceObjectPicker]
        public string CommitID = string.Empty;

        /// <summary>
        /// The commit id of the baseline. The VCS metrics will be gathered for the time
        /// between <see cref="BaselineCommitID"/> and <see cref="CommitID"/>.
        /// If <see cref="BaselineCommitID"/> is null or empty, no VCS metrics are gathered.
        /// </summary>
        [ShowInInspector, Tooltip("VCS metrics will be gathered relative to this commit id. If undefined, no VCS metrics will be gathered"),
            HideReferenceObjectPicker]
        public string BaselineCommitID = string.Empty;

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.VCS;
        }

        /// <summary>
        /// Loads the metrics and nodes from <see cref="GitRepository"/> and
        /// <see cref="CommitID"/> into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        public override async UniTask<Graph> ProvideAsync
            (Graph graph,
             AbstractSEECity city,
             Action<float> changePercentage = null,
             CancellationToken token = default)
        {
            CheckArguments(city);
            return await UniTask.FromResult<Graph>(GitGraphGenerator.AddNodesForCommit
                                                      (graph, SimplifyGraph, GitRepository, CommitID, BaselineCommitID,
                                                       changePercentage, token));
        }

        /// <summary>
        /// Checks whether the assumptions on <see cref="RepositoryPath"/> and
        /// <see cref="CommitID"/> and <paramref name="city"/> hold.
        /// If not, exceptions are thrown accordingly.
        /// </summary>
        /// <param name="city">To be checked</param>
        /// <exception cref="ArgumentException">thrown in case <see cref="RepositoryPath"/>,
        /// or <see cref="CommitID"/>
        /// is undefined or does not exist or <paramref name="city"/> is null</exception>
        protected void CheckArguments(AbstractSEECity city)
        {
            if (GitRepository == null)
            {
                throw new ArgumentException("GitRepository is null.\n");
            }
            if (string.IsNullOrEmpty(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException("Empty repository path.\n");
            }
            if (!Directory.Exists(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException($"Directory {GitRepository.RepositoryPath.Path} does not exist.\n");
            }
            if (string.IsNullOrEmpty(CommitID))
            {
                throw new ArgumentException("Empty CommitID.\n");
            }
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
        }



        /// <summary>
        /// Creates and adds a new node to <paramref name="graph"/> and returns it.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The source name of the node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        private static Node NewNode(Graph graph, string id, string type, string name)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type
            };
            result.Filename = result.SourceName;
            result.Directory = Path.GetDirectoryName(result.ID);
            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Creates a new node for each element of a filepath, that does not
        /// already exists in the graph.
        /// </summary>
        /// <param name="path">The remaining part of the path</param>
        /// <param name="parent">The parent node from the current element of the path</param>
        /// <param name="parentPath">The path of the current parent, which will eventually be part of the ID</param>
        /// <param name="graph">The graph to which the new node belongs to</param>
        /// <param name="rootNode">The root node of the main directory</param>
        /// <returns>The node for the given path or null.</returns>
        private static Node BuildGraphFromPath(string path, Node parent, string parentPath,
            Graph graph, Node rootNode)
        {
            string[] pathSegments = path.Split('/');
            string nodePath = string.Join('/', pathSegments, 1, pathSegments.Length - 1);

            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                Node currentSegmentNode = graph.GetNode(pathSegments[0]);

                // Directory exists already.
                if (currentSegmentNode != null)
                {
                    return BuildGraphFromPath(nodePath, currentSegmentNode, pathSegments[0], graph, rootNode);
                }

                // Directory does not exist.
                if (pathSegments.Length > 1 && parent == null)
                {
                    rootNode.AddChild(NewNode(graph, pathSegments[0], DataModel.DG.VCS.DirectoryType, pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]),
                        pathSegments[0], graph, rootNode);
                }
            }

            // Current pathSegment is not in the main directory.
            if (parentPath != null)
            {
                string currentPathSegment = parentPath + '/' + pathSegments[0];
                Node currentPathSegmentNode = graph.GetNode(currentPathSegment);

                // The node for the current pathSegment exists.
                if (currentPathSegmentNode != null)
                {
                    return BuildGraphFromPath(nodePath, currentPathSegmentNode,
                        currentPathSegment, graph, rootNode);
                }

                // The node for the current pathSegment does not exist, and the node is a directory.
                if (pathSegments.Length > 1)
                {
                    parent.AddChild(NewNode(graph, currentPathSegment, DataModel.DG.VCS.DirectoryType, pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(currentPathSegment),
                        currentPathSegment, graph, rootNode);
                }

                // The node for the current pathSegment does not exist, and the node is a file.
                if (pathSegments.Length == 1)
                {
                    Node addedFileNode = NewNode(graph, currentPathSegment, DataModel.DG.VCS.FileType, pathSegments[0]);
                    parent.AddChild(addedFileNode);
                    return addedFileNode;
                }
            }

            return null;
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="CommitID"/> in the configuration file.
        /// </summary>
        private const string commitIDLabel = "CommitID";
        /// <summary>
        /// Label of attribute <see cref="BaselineCommitID"/> in the configuration file.
        /// </summary>
        private const string baselineCommitIDLabel = "BaselineCommitID";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            base.SaveAttributes(writer);
            writer.Save(CommitID, commitIDLabel);
            writer.Save(BaselineCommitID, baselineCommitIDLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            base.RestoreAttributes(attributes);
            ConfigIO.Restore(attributes, commitIDLabel, ref CommitID);
            ConfigIO.Restore(attributes, baselineCommitIDLabel, ref BaselineCommitID);
        }

        #endregion
    }
}
