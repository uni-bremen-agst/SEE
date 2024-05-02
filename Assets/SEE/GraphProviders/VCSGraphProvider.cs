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
using Dissonance;
using UnityEngine;
using SEE.Utils;
using SEE.UI.Window.CodeWindow;

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
         Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup),
         HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "", false }
        };

        /// <summary>
        /// Loads the metrics and nodes from the given git repository and commitID into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        public override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            CheckArguments(city);
            UniTask<Graph> graphTask = UniTask.FromResult<Graph>(GetVCSGraph(PathGlobbing, RepositoryPath.Path, CommitID));
    
            return graphTask;
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
                throw new ArgumentException("Empty newCommitID.\n");
            }

            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
        }

        /// <summary>
        /// Builds the VCS graph with specific metrics.
        /// </summary>
        /// <param name="pathGlobbing">The paths which get included/excluded.</param>
        /// <param name="repositoryPath">The path to the repository.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        /// <returns>the graph.</returns>
        static Graph GetVCSGraph(Dictionary<string, bool> pathGlobbing, string repositoryPath, string commitID)
        {
            string[] pathSegments = repositoryPath.Split(Path.DirectorySeparatorChar);
            Debug.Log(repositoryPath);
            Graph graph = new(repositoryPath, pathSegments[^1]);
            // The main directory.
            GraphUtils.NewNode(graph, pathSegments[^1], "directory", pathSegments[^1]);

            IEnumerable<string> includedFiles = pathGlobbing
                .Where(path => path.Value == true)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = pathGlobbing
                .Where(path => path.Value == false)
                .Select(path => path.Key);

            using (Repository repo = new(repositoryPath))
            {
                LibGit2Sharp.Tree tree = repo.Lookup<Commit>(commitID).Tree;
                // Get all files using "git ls-files".
                //TODO: I limited the output to 200 for testing, because SEE is huge.
                IEnumerable<string> files;
                if (includedFiles.Any() && !string.IsNullOrEmpty(includedFiles.First()))
                {
                    files = ListTree(tree).Where(path => includedFiles.Contains(Path.GetExtension(path))).Take(200);
                }
                else if (excludedFiles.Any())
                {
                    //files = repo.Index.Select(entry => entry.Path).Where(path => !excludedFiles.Contains(Path.GetExtension(path)));
                    files = ListTree(tree).Where(path => !excludedFiles.Contains(Path.GetExtension(path))).Take(200);
                }
                else
                {
                    files = ListTree(tree).Take(200);
                }

                // Build the graph structure.
                foreach (string filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    string[] filePathSegments = filePath.Split(Path.AltDirectorySeparatorChar);
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1]).AddChild(GraphUtils.NewNode(graph, filePath, "file", filePath));
                    }
                    // Other directorys/files.
                    else
                    {
                        GraphUtils.BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(pathSegments[^1]));
                    }
                }

                AddMetricsToNode(graph, repo, commitID);
                //TODO: Only for testing.
                Debug.Log(graph.ToString());
            }

            return graph;
        }

        /// <summary>
        /// Gets the paths from a repository at the time of a given commitID.
        /// It is equivalent to "git ls-tree --name-only -r commitID"
        /// </summary>
        /// <param name="tree">The tree of the given commit.</param>
        /// <returns>a list of paths.</returns>
        static List<string> ListTree(LibGit2Sharp.Tree tree)
        {
            var fileList = new List<string>();

            foreach (var entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    fileList.Add(entry.Path);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    var subtree = (LibGit2Sharp.Tree)entry.Target;
                    fileList.AddRange(ListTree(subtree));
                }
            }

            return fileList;
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.VCS;
        }

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
        private const string newCommitIDLabel = "NewCommitID";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(PathGlobbing.ToString()) ? null : PathGlobbing;
            writer.Save(pathGlobbing, pathGlobbingLabel);
            writer.Save(CommitID, newCommitIDLabel);
            RepositoryPath.Save(writer, repositoryPathLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref PathGlobbing);
            ConfigIO.Restore(attributes, newCommitIDLabel, ref CommitID);
            RepositoryPath.Restore(attributes, repositoryPathLabel);
        }

 

       

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/> as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="parent">The parent of the new node; must not be null</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The name of the node</param>
        /// <param name="length">The length of the graph element, measured in number of lines</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node Child(Graph graph, Node parent, string id, string type = "Routine", string name = null,
            int? length = null)
        {
            Node child = GraphUtils.NewNode(graph, id, type, name, length);
            parent.AddChild(child);
            return child;
        }

        /// <summary>
        /// Retrieves the token stream for given file content from its repository and commit ID.
        /// </summary>
        /// <param name="filePath">The filePath from the node.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        /// <returns>The token stream for the specified file and commit.</returns>
        private static IEnumerable<SEEToken> RetrieveTokens(string filePath, Repository repository, string commitID)
        {
            Blob blob = repository.Lookup<Blob>($"{commitID}:{filePath}");

            if (blob != null)
            {
                string fileContent = blob.GetContentText();

                return SEEToken.FromString(fileContent,
                    TokenLanguage.FromFileExtension(Path.GetExtension(filePath)?[1..]));
            }
            else
            {
                // Token does not exist.
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
                if (node.Type == "file")
                {
                    string filePath = node.ID.Replace('\\', '/');
                    IEnumerable<SEEToken> tokens;
                    TokenLanguage language;

                    try
                    {
                        tokens = RetrieveTokens(filePath, repository, commitID);
                        language = TokenLanguage.FromFileExtension(Path.GetExtension(filePath).TrimStart('.'));
                    }
                    catch (Exception)
                    {
                        UnityEngine.Debug.LogError($"Unknown token type");
                        continue;
                    }

                    //if(language == TokenLanguage.AllTokenLanguages)
                    if (TokenLanguage.AllTokenLanguages.Contains(language))
                    {
                        int complexity = CalculateMcCabeComplexity(tokens);
                        int linesOfCode = CalculateLinesOfCode(tokens);
                        halsteadMetrics = CalculateHalsteadMetrics(tokens);
                        node.SetInt("Lines of Code", linesOfCode);
                        node.SetInt("McCabe Complexity", complexity);
                        node.SetInt("Halstead Distinct Operators", halsteadMetrics.DistinctOperators);
                        node.SetInt("Halstead Distinct Operands", halsteadMetrics.DistinctOperands);
                        node.SetInt("Halstead Total Operators", halsteadMetrics.TotalOperators);
                        node.SetInt("Halstead Total Operands", halsteadMetrics.TotalOperands);
                        node.SetInt("Halstead Program Vocabulary", halsteadMetrics.ProgramVocabulary);
                        node.SetInt("Halstead Program Length", halsteadMetrics.ProgramLength);
                        node.SetFloat("Halstead Calculated Estimated Program Length",
                            halsteadMetrics.EstimatedProgramLength);
                        node.SetFloat("Halstead Volume", halsteadMetrics.Volume);
                        node.SetFloat("Halstead Difficulty", halsteadMetrics.Difficulty);
                        node.SetFloat("Halstead Effort", halsteadMetrics.Effort);
                        node.SetFloat("Halstead Time Required to Program", halsteadMetrics.TimeRequiredToProgram);
                        node.SetFloat("Halstead Number of Delivered Bugs", halsteadMetrics.NumberOfDeliveredBugs);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Unknown token type for file {filePath}");
                        // UnknownTokens, no metrics available
                        node.SetInt("Lines of Code", -1);
                        node.SetInt("McCabe Complexity", -1);
                        node.SetInt("Halstead Distinct Operators", -1);
                        node.SetInt("Halstead Distinct Operands", -1);
                        node.SetInt("Halstead Total Operators", -1);
                        node.SetInt("Halstead Total Operands", -1);
                        node.SetInt("Halstead Program Vocabulary", -1);
                        node.SetInt("Halstead Program Length", -1);
                        node.SetFloat("Halstead Calculated Estimated Program Length", -1);
                        node.SetFloat("Halstead Volume", -1);
                        node.SetFloat("Halstead Difficulty", -1);
                        node.SetFloat("Halstead Effort", -1);
                        node.SetFloat("Halstead Time Required to Program", -1);
                        node.SetFloat("Halstead Number of Delivered Bugs", -1);
                    }
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
        /// Helper struct to store Halstead metrics.
        /// </summary>
        private struct HalsteadMetrics
        {
            public int DistinctOperators;
            public int DistinctOperands;
            public int TotalOperators;
            public int TotalOperands;
            public int ProgramVocabulary;
            public int ProgramLength;
            public float EstimatedProgramLength;
            public float Volume;
            public float Difficulty;
            public float Effort;
            public float TimeRequiredToProgram;
            public float NumberOfDeliveredBugs;
        }

        /// <summary>
        /// Calculates the Halstead metrics for provided code.
        /// </summary>
        /// <param name="tokens">The tokens for which the metrics should be calculated.</param>
        /// <returns>Returns the Halstead metrics.</returns>
        private static HalsteadMetrics CalculateHalsteadMetrics(IEnumerable<SEEToken> tokens)
        {
            // Identify operands (identifiers, keywords and literals).
            HashSet<string> operands = new(tokens.Where(t =>
                    t.TokenType == SEEToken.Type.Identifier || t.TokenType == SEEToken.Type.Keyword ||
                    t.TokenType == SEEToken.Type.NumberLiteral || t.TokenType == SEEToken.Type.StringLiteral)
                .Select(t => t.Text));

            // Identify operators.
            HashSet<string> operators =
                new(tokens.Where(t => t.TokenType == SEEToken.Type.Punctuation).Select(t => t.Text));

            // Count the total number of operands and operators.
            int totalOperands = tokens.Count(t =>
                t.TokenType == SEEToken.Type.Identifier || t.TokenType == SEEToken.Type.Keyword ||
                t.TokenType == SEEToken.Type.NumberLiteral || t.TokenType == SEEToken.Type.StringLiteral);
            int totalOperators = tokens.Count(t => t.TokenType == SEEToken.Type.Punctuation);

            // Derivative Halstead metrics.
            int programVocabulary = operators.Count + operands.Count;
            int programLength = totalOperators + totalOperands;
            float estimatedProgramLength = (float)((operators.Count * Math.Log(operators.Count, 2) +
                                                    operands.Count * Math.Log(operands.Count, 2)));
            float volume = (float)(programLength * Math.Log(programVocabulary, 2));
            float difficulty = operators.Count == 0
                ? 0
                : operators.Count / 2.0f * (totalOperands / (float)operands.Count);
            float effort = difficulty * volume;
            float timeRequiredToProgram = effort / 18.0f;
            float numberOfDeliveredBugs = volume / 3000.0f;

            return new HalsteadMetrics
            {
                DistinctOperators = operators.Count,
                DistinctOperands = operands.Count,
                TotalOperators = totalOperators,
                TotalOperands = totalOperands,
                ProgramVocabulary = programVocabulary,
                ProgramLength = programLength,
                EstimatedProgramLength = estimatedProgramLength,
                Volume = volume,
                Difficulty = difficulty,
                Effort = effort,
                TimeRequiredToProgram = timeRequiredToProgram,
                NumberOfDeliveredBugs = numberOfDeliveredBugs
            };
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