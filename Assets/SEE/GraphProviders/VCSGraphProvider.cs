using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SEE.UI.Window.CodeWindow;
using System.Threading;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Provides a version control system graph based on a git repository.
    /// </summary>
    public class VCSGraphProvider : GraphProvider
    {
        /// <summary>
        /// The path to the git repository.
        /// </summary>
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker]
        public DirectoryPath RepositoryPath = new();

        /// <summary>
        /// The commit id.
        /// </summary>
        [ShowInInspector, Tooltip("The new commit id."), HideReferenceObjectPicker]
        public string CommitID = "";

        /// <summary>
        /// The List of filetypes that get included/excluded.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
            Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "", false }
        };

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.VCS;
        }

        /// <summary>
        /// Loads the metrics and nodes from the given git repository and commitID into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">This parameter is currently ignored.</param>
        /// <param name="token">This parameter is currently ignored.</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city, Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            CheckArguments(city);
            return await UniTask.FromResult<Graph>(GetVCSGraph(PathGlobbing, RepositoryPath.Path, CommitID));
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
            if (string.IsNullOrEmpty(RepositoryPath.Path))
            {
                throw new ArgumentException("Empty repository path.\n");
            }
            if (!Directory.Exists(RepositoryPath.Path))
            {
                throw new ArgumentException($"Directory {RepositoryPath.Path} does not exist.\n");
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
        /// <param name="pathGlobbing">The paths which get included/excluded.</param>
        /// <param name="repositoryPath">The path to the repository.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        /// <returns>the graph.</returns>
        private static Graph GetVCSGraph(Dictionary<string, bool> pathGlobbing, string repositoryPath, string commitID)
        {
            string[] pathSegments = repositoryPath.Split(Path.DirectorySeparatorChar);

            Graph graph = new(repositoryPath, pathSegments[^1]);
            graph.CommitID(commitID);
            graph.RepositoryPath(repositoryPath);

            // The main directory.
            NewNode(graph, pathSegments[^1], directoryNodeType, pathSegments[^1]);

            IEnumerable<string> includedPathGlobs = pathGlobbing
                .Where(path => path.Value)
                .Select(path => path.Key).ToHashSet();

            IEnumerable<string> excludedPathGlobs = pathGlobbing
                .Where(path => !path.Value)
                .Select(path => path.Key).ToHashSet();

            using (Repository repo = new(repositoryPath))
            {
                LibGit2Sharp.Tree tree = repo.Lookup<Commit>(commitID).Tree;
                // Get all files using "git ls-tree -r <CommitID> --name-only".
                IEnumerable<string> files;
                if (includedPathGlobs.Any() && !string.IsNullOrEmpty(includedPathGlobs.First()))
                {
                    files = ListTree(tree).Where(path => includedPathGlobs.Contains(Path.GetExtension(path)));
                }
                else if (excludedPathGlobs.Any())
                {
                    files = ListTree(tree).Where(path => !excludedPathGlobs.Contains(Path.GetExtension(path)));
                }
                else
                {
                    files = ListTree(tree);
                }
                // Build the graph structure.
                foreach (string filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    string[] filePathSegments = filePath.Split('/');
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1]).AddChild(NewNode(graph, filePath, fileNodeType, filePath));
                    }
                    // Other directories/files.
                    else
                    {
                        BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(pathSegments[^1]));
                    }
                }
                AddMetricsToNode(graph, repo, commitID);
            }
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
        /// <param name="mainNode">The root node of the main directory</param>
        public static Node BuildGraphFromPath(string path, Node parent, string parentPath,
            Graph graph, Node mainNode)
        {
            string[] pathSegments = path.Split('/');
            string nodePath = string.Join('/', pathSegments, 1,
                pathSegments.Length - 1);

            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                Node currentSegmentNode = graph.GetNode(pathSegments[0]);

                // Directory already exists.
                if (currentSegmentNode != null)
                {
                    return BuildGraphFromPath(nodePath, currentSegmentNode, pathSegments[0], graph, mainNode);
                }

                // Directory does not exist.
                if (currentSegmentNode == null && pathSegments.Length > 1 && parent == null)
                {
                    mainNode.AddChild(NewNode(graph, pathSegments[0], directoryNodeType, pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]),
                        pathSegments[0], graph, mainNode);
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
                        currentPathSegment, graph, mainNode);
                }

                // The node for the current pathSegment does not exist, and the node is a directory.
                if (currentPathSegmentNode == null &&
                    pathSegments.Length > 1)
                {
                    parent.AddChild(NewNode(graph, currentPathSegment, directoryNodeType, pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(currentPathSegment),
                        currentPathSegment, graph, mainNode);
                }

                // The node for the current pathSegment does not exist, and the node is a file.
                if (currentPathSegmentNode == null &&
                    pathSegments.Length == 1)
                {
                    Node addedFileNode = NewNode(graph, currentPathSegment, fileNodeType, pathSegments[0]);
                    parent.AddChild(addedFileNode);
                    return addedFileNode;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the paths from a repository at the time of a given commitID.
        /// It is equivalent to "git ls-tree --name-only -r commitID"
        /// </summary>
        /// <param name="tree">The tree of the given commit.</param>
        /// <returns>a list of paths.</returns>
        private static IEnumerable<string> ListTree(LibGit2Sharp.Tree tree)
        {
            List<string> fileList = new();

            foreach (TreeEntry entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    fileList.Add(entry.Path);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    LibGit2Sharp.Tree subtree = (LibGit2Sharp.Tree)entry.Target;
                    fileList.AddRange(ListTree(subtree));
                }
            }

            return fileList;
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="PathGlobbing"/> in the configuration file.
        /// </summary>
        private const string pathGlobbingLabel = "PathGlobbing";

        /// <summary>
        /// Label of attribute <see cref="RepositoryPath"/> in the configuration file.
        /// </summary>
        private const string repositoryPathLabel = "RepositoryPath";

        /// <summary>
        /// Label of attribute <see cref="NewCommitID"/> in the configuration file.
        /// </summary>
        private const string commitIDLabel = "CommitID";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(PathGlobbing.ToString()) ? null : PathGlobbing;
            writer.Save(pathGlobbing, pathGlobbingLabel);
            writer.Save(CommitID, commitIDLabel);
            RepositoryPath.Save(writer, repositoryPathLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref PathGlobbing);
            ConfigIO.Restore(attributes, commitIDLabel, ref CommitID);
            RepositoryPath.Restore(attributes, repositoryPathLabel);
        }

        #endregion

        /// <summary>
        /// Retrieves the token stream for given file content from its repository and commit ID.
        /// </summary>
        /// <param name="filePath">The filePath from the node.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        /// <param name="language">The language the given text is written in.</param>
        /// <returns>The token stream for the specified file and commit.</returns>
        private static IEnumerable<SEEToken> RetrieveTokens(string filePath, Repository repository, string commitID, TokenLanguage language)
        {
            Blob blob = repository.Lookup<Blob>($"{commitID}:{filePath}");

            if (blob != null)
            {
                string fileContent = blob.GetContentText();
                return SEEToken.FromString(fileContent, language);
            }
            else
            {
                // Blob does not exist.
                return Enumerable.Empty<SEEToken>();
            }
        }

        /// <summary>
        /// Adds Halstead, McCabe and lines of code metrics to the corresponding node for the supported TokenLanguages in <paramref name="graph"/>.
        /// Otherwise, metrics are not available.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        protected static void AddMetricsToNode(Graph graph, Repository repository, string commitID)
        {
            HalsteadMetrics halsteadMetrics;
            foreach (Node node in graph.Nodes())
            {
                if (node.Type == fileNodeType)
                {
                    string filePath = node.ID.Replace('\\', '/');
                    IEnumerable<SEEToken> tokens;
                    TokenLanguage language;
                    language = TokenLanguage.FromFileExtension(Path.GetExtension(filePath).TrimStart('.'));
                    if (language == TokenLanguage.Plain)
                    {
                            continue;
                    }
                    tokens = RetrieveTokens(filePath, repository, commitID, language);
                    int complexity = CalculateMcCabeComplexity(tokens);
                    int linesOfCode = CalculateLinesOfCode(tokens);
                    halsteadMetrics = CalculateHalsteadMetrics(tokens);
                    node.SetInt("Metrics.LOC", linesOfCode);
                    node.SetInt("Metrics.McCabe_Complexity", complexity);
                    node.SetInt("Metrics.Halstead.Distinct_Operators", halsteadMetrics.DistinctOperators);
                    node.SetInt("Metrics.Halstead.Distinct_Operands", halsteadMetrics.DistinctOperands);
                    node.SetInt("Metrics.Halstead.Total_Operators", halsteadMetrics.TotalOperators);
                    node.SetInt("Metrics.Halstead.Total_Operands", halsteadMetrics.TotalOperands);
                    node.SetInt("Metrics.Halstead.Program_Vocabulary", halsteadMetrics.ProgramVocabulary);
                    node.SetInt("Metrics.Halstead.Program_Length", halsteadMetrics.ProgramLength);
                    node.SetFloat("Metrics.Halstead.Estimated_Program_Length", halsteadMetrics.EstimatedProgramLength);
                    node.SetFloat("Metrics.Halstead.Volume", halsteadMetrics.Volume);
                    node.SetFloat("Metrics.Halstead.Difficulty", halsteadMetrics.Difficulty);
                    node.SetFloat("Metrics.Halstead.Effort", halsteadMetrics.Effort);
                    node.SetFloat("Metrics.Halstead.Time_Required_To_Program", halsteadMetrics.TimeRequiredToProgram);
                    node.SetFloat("Metrics.Halstead.Number_Of_Delivered_Bugs", halsteadMetrics.NumberOfDeliveredBugs);
                }
            }
        }

        /// <summary>
        /// Calculates the McCabe cyclomatic complexity for provided code.
        /// </summary>
        /// <param name="tokens">The tokens used for which the complexity should be calculated.</param>
        /// <returns>Returns the McCabe cyclomatic complexity.</returns>
        private static int CalculateMcCabeComplexity(IEnumerable<SEEToken> tokens)
        {
            int complexity = 1; // Starting complexity for a single method or function.

            // Count decision points (if, for, while, case, &&, ||, ?).
            complexity += tokens.Count(t => t.TokenType == SEEToken.Type.Keyword);

            // Count nested cases (i.e. switch statements).
            complexity += tokens.Count(t => t.TokenType == SEEToken.Type.Keyword && t.Text == "case");

            return complexity;
        }

        /// <summary>
        /// Helper record to store Halstead metrics.
        /// </summary>
        /// <param name="DistinctOperators">The number of distinct operators in the code.</param>
        /// <param name="DistinctOperands">The number of distinct operands in the code.</param>
        /// <param name="TotalOperators">The total number of operators in the code.</param>
        /// <param name="TotalOperands">The total number of operands in the code.</param>
        /// <param name="ProgramVocabulary">The program vocabulary, i.e. the sum of distinct operators and operands.</param>
        /// <param name="ProgramLength">The program length, i.e. the sum of total operators and operands.</param>
        /// <param name="EstimatedProgramLength">The estimated program length based on the program vocabulary.</param>
        /// <param name="Volume">The program volume, which is a measure of the program's size and complexity.</param>
        /// <param name="Difficulty">The program difficulty, which is a measure of how difficult the code is to understand and modify.</param>
        /// <param name="Effort">The program effort, which is a measure of the effort required to understand and modify the code.</param>
        /// <param name="TimeRequiredToProgram">The estimated time required to program the code, based on the program effort.</param>
        /// <param name="NumberOfDeliveredBugs">The estimated number of delivered bugs in the code, based on the program volume.</param>
        private record HalsteadMetrics(

            int DistinctOperators,
            int DistinctOperands,
            int TotalOperators,
            int TotalOperands,
            int ProgramVocabulary,
            int ProgramLength,
            float EstimatedProgramLength,
            float Volume,
            float Difficulty,
            float Effort,
            float TimeRequiredToProgram,
            float NumberOfDeliveredBugs
        );

        /// <summary>
        /// Calculates the Halstead metrics for provided code.
        /// </summary>
        /// <param name="tokens">The tokens for which the metrics should be calculated.</param>
        /// <returns>Returns the Halstead metrics.</returns>
        private static HalsteadMetrics CalculateHalsteadMetrics(IEnumerable<SEEToken> tokens)
        {
            // Set of token types which are operands.
            HashSet<SEEToken.Type> operandTypes = new()
            {
                SEEToken.Type.Identifier,
                SEEToken.Type.Keyword,
                SEEToken.Type.NumberLiteral,
                SEEToken.Type.StringLiteral
            };

            // Identify operands.
            HashSet<string> operands = new(tokens.Where(t => operandTypes.Contains(t.TokenType)).Select(t => t.Text));

            // Identify operators.
            HashSet<string> operators = new(tokens.Where(t => t.TokenType == SEEToken.Type.Punctuation).Select(t => t.Text));

            // Count the total number of operands and operators.
            int totalOperands = tokens.Count(t => operandTypes.Contains(t.TokenType));
            int totalOperators = tokens.Count(t => t.TokenType == SEEToken.Type.Punctuation);

            // Derivative Halstead metrics.
            int programVocabulary = operators.Count + operands.Count;
            int programLength = totalOperators + totalOperands;
            float estimatedProgramLength = (float)((operators.Count * Mathf.Log(operators.Count, 2) + operands.Count * Mathf.Log(operands.Count, 2)));
            float volume = (float)(programLength * Mathf.Log(programVocabulary, 2));
            float difficulty = operators.Count == 0 ? 0 : operators.Count / 2.0f * (totalOperands / (float)operands.Count);
            float effort = difficulty * volume;
            float timeRequiredToProgram = effort / 18.0f; // Formula: Time T = effort E / S, where S = Stroud's number of psychological 'moments' per second; typically a figure of 18 is used in Software Science.
            float numberOfDeliveredBugs = volume / 3000.0f; // Formula: Bugs B = effort E^(2/3) / 3000 or bugs B = volume V / 3000 are both used. 3000 is an empirical estimate.

            return new HalsteadMetrics(

                operators.Count,
                operands.Count,
                totalOperators,
                totalOperands,
                programVocabulary,
                programLength,
                estimatedProgramLength,
                volume,
                difficulty,
                effort,
                timeRequiredToProgram,
                numberOfDeliveredBugs
            );
        }

        /// <summary>
        /// Calculates the number of lines of code for the provided token stream, excluding comments.
        /// </summary>
        /// <param name="tokens">The tokens for which the lines of code should be counted.</param>
        /// <returns>Returns the number of lines of code.</returns>
        private static int CalculateLinesOfCode(IEnumerable<SEEToken> tokens)
        {
            int linesOfCode = 0;
            bool comment = false;

            foreach (SEEToken token in tokens)
            {
                if (token.TokenType == SEEToken.Type.Newline)
                {
                    if (!comment)
                    {
                        linesOfCode++;
                    }
                }
                else if (token.TokenType == SEEToken.Type.Comment)
                {
                    comment = true;
                }
                else if (token.TokenType != SEEToken.Type.Whitespace)
                {
                    comment = false;
                }
            }
            return linesOfCode;
        }
    }
}
