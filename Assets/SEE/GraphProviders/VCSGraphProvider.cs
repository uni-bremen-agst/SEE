using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SEE.Scanner;
using System.Threading;
using SEE.Scanner.Antlr;
using SEE.Utils;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Creates a graph based on the content of a version control system.
    /// Nodes represent directories and files. Their nesting corresponds to
    /// the directory structure of the repository. Files are leaf nodes.
    /// File nodes contain metrics that can be gathered based on a simple
    /// lexical analysis, such as Halstead, McCabe and lines of code, as
    /// well as from the version control system, such as number of developers,
    /// number of commits, or code churn.
    /// </summary>
    public class VCSGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The git repository which should be analyzed.
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"),
            RuntimeTab("Data")]
        public GitRepository GitRepository = new();

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
        /// Loads the metrics and nodes from the given git repository and commitID into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            CheckArguments(city);
            return await UniTask.FromResult<Graph>(GetVCSGraph(GitRepository, CommitID, BaselineCommitID,
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
        /// The node type for directories.
        /// </summary>
        private const string directoryNodeType = "Directory";
        /// <summary>
        /// The node type for files.
        /// </summary>
        private const string fileNodeType = "File";

        /// <summary>
        /// Builds the VCS graph with specific metrics.
        /// </summary>
        /// <param name="commitID">The commit id where the files exist.</param>
        /// <param name="baselineCommitID">The commit id of the baseline against which to gather
        /// the VCS metrics</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>the resulting graph</returns>
        private static Graph GetVCSGraph
            (GitRepository repository,
            string commitID,
            string baselineCommitID,
            Action<float> changePercentage,
            CancellationToken token)
        {
            string repositoryPath = repository.RepositoryPath.Path;
            string rootDirectory = Filenames.InnermostDirectoryName(repositoryPath);

            Graph graph = new(repositoryPath, rootDirectory);
            graph.CommitID(commitID);
            graph.RepositoryPath(repositoryPath);

            // The main directory.
            NewNode(graph, rootDirectory, directoryNodeType, rootDirectory);

            {
                // Get all files using "git ls-tree -r <CommitID> --name-only".
                ICollection<string> files = repository.AllFiles(commitID);

                float totalSteps = files.Count;
                int currentStep = 0;
                // Build the graph structure.
                foreach (string filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(token);
                    }
                    string[] filePathSegments = filePath.Split('/');
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(rootDirectory).AddChild(NewNode(graph, filePath, fileNodeType, filePath));
                    }
                    // Other directories/files.
                    else
                    {
                        BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(rootDirectory));
                    }
                    currentStep++;
                    changePercentage?.Invoke(currentStep / totalSteps);
                }
                AddCodeMetrics(graph, repository, commitID);
                ADDVCSMetrics(graph, repository, baselineCommitID, commitID);
            }
            graph.FinalizeNodeHierarchy();
            changePercentage?.Invoke(1f);
            return graph;
        }

        /// <summary>
        /// Creates and adds a new node to <paramref name="graph"/> and returns it.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The source name of the node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        public static Node NewNode(Graph graph, string id, string type, string name)
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
        public static Node BuildGraphFromPath(string path, Node parent, string parentPath,
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
                    rootNode.AddChild(NewNode(graph, pathSegments[0], directoryNodeType, pathSegments[0]));
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
                    parent.AddChild(NewNode(graph, currentPathSegment, directoryNodeType, pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(currentPathSegment),
                        currentPathSegment, graph, rootNode);
                }

                // The node for the current pathSegment does not exist, and the node is a file.
                if (pathSegments.Length == 1)
                {
                    Node addedFileNode = NewNode(graph, currentPathSegment, fileNodeType, pathSegments[0]);
                    parent.AddChild(addedFileNode);
                    return addedFileNode;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the token stream for given file content from its repository and commit ID.
        /// </summary>
        /// <param name="repositoryFilePath">The file path from the node. This must be a relative path
        /// in the syntax of the repository regarding the directory separator</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        /// <param name="language">The language the given text is written in.</param>
        /// <returns>The token stream for the specified file and commit.</returns>
        public static ICollection<AntlrToken> RetrieveTokens
            (string repositoryFilePath,
            GitRepository repository,
             string commitID,
             AntlrLanguage language)
        {
            try
            {
                return AntlrToken.FromString(repository.GetFileContent(repositoryFilePath, commitID), language);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error retrieving file content for {repositoryFilePath} at commit {commitID}: {e.Message}");
                return new List<AntlrToken>();
            }
        }

        /// <summary>
        /// Adds Halstead, McCabe and lines of code metrics to the corresponding node for the supported TokenLanguages
        /// in <paramref name="graph"/>.
        /// Otherwise, metrics are not available.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        private static void AddCodeMetrics(Graph graph, GitRepository repository, string commitID)
        {
            foreach (Node node in graph.Nodes())
            {
                if (node.Type == fileNodeType)
                {
                    string repositoryFilePath = node.ID;
                    AntlrLanguage language = AntlrLanguage.FromFileExtension(Path.GetExtension(repositoryFilePath).TrimStart('.'));
                    if (language != AntlrLanguage.Plain)
                    {
                        ICollection<AntlrToken> tokens = RetrieveTokens(repositoryFilePath, repository, commitID, language);
                        node.SetInt(Metrics.Prefix + "LOC", TokenMetrics.CalculateLinesOfCode(tokens));
                        node.SetInt(Metrics.Prefix + "McCabe_Complexity", TokenMetrics.CalculateMcCabeComplexity(tokens));
                        TokenMetrics.HalsteadMetrics halsteadMetrics = TokenMetrics.CalculateHalsteadMetrics(tokens);
                        node.SetInt(Halstead.DistinctOperators, halsteadMetrics.DistinctOperators);
                        node.SetInt(Halstead.DistinctOperands, halsteadMetrics.DistinctOperands);
                        node.SetInt(Halstead.TotalOperators, halsteadMetrics.TotalOperators);
                        node.SetInt(Halstead.TotalOperands, halsteadMetrics.TotalOperands);
                        node.SetInt(Halstead.ProgramVocabulary, halsteadMetrics.ProgramVocabulary);
                        node.SetInt(Halstead.ProgramLength, halsteadMetrics.ProgramLength);
                        node.SetFloat(Halstead.EstimatedProgramLength, halsteadMetrics.EstimatedProgramLength);
                        node.SetFloat(Halstead.Volume, halsteadMetrics.Volume);
                        node.SetFloat(Halstead.Difficulty, halsteadMetrics.Difficulty);
                        node.SetFloat(Halstead.Effort, halsteadMetrics.Effort);
                        node.SetFloat(Halstead.TimeRequiredToProgram, halsteadMetrics.TimeRequiredToProgram);
                        node.SetFloat(Halstead.NumberOfDeliveredBugs, halsteadMetrics.NumberOfDeliveredBugs);
                    }
                }
            }
        }

        /// <summary>
        /// Adds VCS metrics to all nodes in <paramref name="graph"/> based on the
        /// VCS information derived from <paramref name="repository"/>. The metrics are gathered
        /// in between the <paramref name="oldCommit"/> and <paramref name="newCommit"/>.
        /// If <paramref name="oldCommit"/> is null or empty, no metrics will be
        /// gathered.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="oldCommit">The starting commit ID (baseline).</param>
        /// <param name="newCommit">The ending commit.</param>
        private static void ADDVCSMetrics(Graph graph, GitRepository repository, string oldCommit, string newCommit)
        {
            if (!string.IsNullOrWhiteSpace(oldCommit))
            {
                VCSMetrics.AddMetrics(graph, repository, oldCommit, newCommit);
            }
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="GitRepository"/> in the configuration file.
        /// </summary>
        private const string gitRepositoryLabel = "GitRepository";

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
            writer.Save(CommitID, commitIDLabel);
            writer.Save(BaselineCommitID, baselineCommitIDLabel);
            GitRepository.Save(writer, gitRepositoryLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, commitIDLabel, ref CommitID);
            ConfigIO.Restore(attributes, baselineCommitIDLabel, ref BaselineCommitID);
            GitRepository.Restore(attributes, gitRepositoryLabel);
        }

        #endregion
    }
}
