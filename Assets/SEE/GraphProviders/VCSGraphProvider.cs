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
using SEE.VCS;
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
        public readonly DirectoryPath RepositoryPath = new();

        /// <summary>
        /// The commit id against which to compare.
        /// </summary>
        [ShowInInspector, Tooltip("The commit id against which to compare."), HideReferenceObjectPicker]
        public string OldCommitID = "";

        /// <summary>
        /// The new commit id.
        /// </summary>
        [ShowInInspector, Tooltip("The new commit id."), HideReferenceObjectPicker]
        public string NewCommitID = "";

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

        /// <summary>
        /// Loads the metrics and nodes from the given git repository and commitID into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            CheckArguments(city);
            return await UniTask.FromResult<Graph>(GetVCSGraph(PathGlobbing, RepositoryPath.Path, OldCommitID, NewCommitID));
        }

        /// <summary>
        /// Checks whether the assumptions on <see cref="RepositoryPath"/>, <see cref="OldCommitID"/>,
        /// <see cref="NewCommitID"/> and <paramref name="city"/> hold.
        /// If not, exceptions are thrown accordingly.
        /// </summary>
        /// <param name="city">To be checked</param>
        /// <exception cref="ArgumentException">thrown in case <see cref="RepositoryPath"/>,
        /// <see cref="OldCommitID"/> or <see cref="NewCommitID"/>
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
            if (string.IsNullOrEmpty(OldCommitID))
            {
                throw new ArgumentException("Empty oldCommitID.\n");
            }
            if (string.IsNullOrEmpty(NewCommitID))
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
        /// <returns>the graph.</returns>
        static Graph GetVCSGraph(Dictionary<string, bool> pathGlobbing, string repositoryPath, string oldCommitID, string newCommitID)
        {
            string[] pathSegments = repositoryPath.Split(Path.DirectorySeparatorChar);
            Debug.Log(repositoryPath);
            Graph graph = new(repositoryPath, pathSegments[^1]);
            // The main directory.
            NewNode(graph, pathSegments[^1], "directory", pathSegments[^1]);

            IEnumerable<string> includedFiles = pathGlobbing
                .Where(path => path.Value == true)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = pathGlobbing
                .Where(path => path.Value == false)
                .Select(path => path.Key);

            using (Repository repo = new(repositoryPath))
            {
                LibGit2Sharp.Tree tree = repo.Lookup<Commit>(newCommitID).Tree;
                // Get all files using "git ls-files".
                //TODO: I limited the output to 200 for testing, because SEE is huge.
                IEnumerable<string> files;
                if (includedFiles.Any() && !string.IsNullOrEmpty(includedFiles.First()))
                {
                    files = ListTree(tree).Where(path => includedFiles.Contains(Path.GetExtension(path))).Take(5);
                }
                else if (excludedFiles.Any())
                {
                    //files = repo.Index.Select(entry => entry.Path).Where(path => !excludedFiles.Contains(Path.GetExtension(path)));
                    files = ListTree(tree).Where(path => !excludedFiles.Contains(Path.GetExtension(path))).Take(5);
                }
                else
                {
                    files = ListTree(tree).Take(5);
                }

                // Build the graph structure.
                foreach (string filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    string[] filePathSegments = filePath.Split(Path.AltDirectorySeparatorChar);
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1]).AddChild(NewNode(graph, filePath, "file", filePath));
                    }
                    // Other directorys/files.
                    else
                    {
                        BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(pathSegments[^1]));
                    }

                    AddMcCabeMetric(graph, repo, newCommitID);
                    AddHalsteadMetrics(graph, repo, newCommitID);
                    AddLinesOfCodeMetric(graph, repo, newCommitID);
                }
                //TODO: Only for testing.
                Debug.Log(graph.ToString());
            }

            AddLineofCodeChurnMetric(graph, repositoryPath, oldCommitID, newCommitID);
            AddNumberofDevelopersMetric(graph, repositoryPath, oldCommitID, newCommitID);
            AddCommitFrequencyMetric(graph, repositoryPath, oldCommitID, newCommitID);

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
        /// Label of attribute <see cref="OldCommitID"/> in the configuration file.
        /// </summary>
        private const string oldCommitIDLabel = "OldCommitID";

        /// <summary>
        /// Label of attribute <see cref="NewCommitID"/> in the configuration file.
        /// </summary>
        private const string newCommitIDLabel = "NewCommitID";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(PathGlobbing.ToString()) ? null : PathGlobbing;
            writer.Save(pathGlobbing, pathGlobbingLabel);
            writer.Save(OldCommitID, oldCommitIDLabel);
            writer.Save(NewCommitID, newCommitIDLabel);
            RepositoryPath.Save(writer, repositoryPathLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref PathGlobbing);
            ConfigIO.Restore(attributes, oldCommitIDLabel, ref OldCommitID);
            ConfigIO.Restore(attributes, newCommitIDLabel, ref NewCommitID);
            RepositoryPath.Restore(attributes, repositoryPathLabel);
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
        static void BuildGraphFromPath(string path, Node parent, string parentPath, Graph graph, Node mainNode)
        {
            string[] pathSegments = path.Split(Path.AltDirectorySeparatorChar);
            string nodePath = string.Join(Path.AltDirectorySeparatorChar.ToString(), pathSegments, 1, pathSegments.Length - 1);
            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                // Directory already exists.
                if (graph.GetNode(pathSegments[0]) != null)
                {
                    BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), pathSegments[0], graph, mainNode);
                }
                // Directory does not exist.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length > 1 && parent == null)
                {
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory", pathSegments[0]));
                    BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), pathSegments[0], graph, mainNode);
                }
                // I dont know, if this code ever gets used -> I dont know, how to handle empty directorys.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length == 1 && parent == null)
                {
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory", pathSegments[0]));
                }
            }
            // Current pathSegment is not in the main directory.
            if (parentPath != null)
            {
                // The node for the current pathSegment exists.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) != null)
                {
                    BuildGraphFromPath(nodePath, graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
                }
                // The node for the current pathSegment does not exist, and the node is a directory.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null && pathSegments.Length > 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "directory", pathSegments[0]));
                    BuildGraphFromPath(nodePath, graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
                }
                // The node for the current pathSegment does not exist, and the node is file.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null && pathSegments.Length == 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "file", pathSegments[0]));
                }
            }
        }

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The name of the node</param>
        /// <param name="length">The length of the graph element, measured in number of lines</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node NewNode(Graph graph, string id, string type = "Routine", string name = null, int? length = null)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type,
                SourceLength = length
            };

            graph.AddNode(result);
            return result;
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
        protected static Node Child(Graph graph, Node parent, string id, string type = "Routine", string name = null, int? length = null)
        {
            Node child = NewNode(graph, id, type, name, length);
            parent.AddChild(child);
            return child;
        }

        /// <summary>
        /// Calculates the number of lines of code added and deleted for each file changed between two commits and adds them as metrics to <paramref name="graph"/>.
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="repositoryPath">Path of the repository</param>
        /// <param name="oldCommit">Commit hash of the older Commit</param>
        /// <param name="newCommit">Commit hash of the newer Commit</param>
        protected static void AddLineofCodeChurnMetric(Graph graph, String repositoryPath, String oldCommit, String newCommit)
        {
            using (var repo = new Repository(repositoryPath))
            {
                var commit1 = repo.Lookup<Commit>(oldCommit);
                var commit2 = repo.Lookup<Commit>(newCommit);

                var changes = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree);

                foreach (var change in changes)
                {
                    foreach (var node in graph.Nodes())
                    {
                        if (node.ID.Replace('\\', '/') == change.Path)
                        {
                            node.SetInt("Lines Added", change.LinesAdded);
                            node.SetInt("Lines Deleted", change.LinesDeleted);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the number of unique developers who contributed to each file for each file changed between two commits and adds it as a metric to <paramref name="graph"/>
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="repositoryPath">Path of the repository</param>
        /// <param name="oldCommit">Commit hash of the older Commit</param>
        /// <param name="newCommit">Commit hash of the newer Commit</param>
        protected static void AddNumberofDevelopersMetric(Graph graph, String repositoryPath, String oldCommit, String newCommit)
        {
            using (var repo = new Repository(repositoryPath))
            {
                var commit1 = repo.Lookup<Commit>(oldCommit);
                var commit2 = repo.Lookup<Commit>(newCommit);

                var changes = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree);

                Dictionary<string, HashSet<string>> fileAuthors = new Dictionary<string, HashSet<string>>();

                foreach (var change in changes)
                {
                    string filePath = change.Path;

                    HashSet<string> authors = new HashSet<string>();

                    foreach (LogEntry commitLogEntry in repo.Commits.QueryBy(filePath))
                    {
                        Commit commit = commitLogEntry.Commit;
                        authors.Add(commit.Author.Name);
                    }

                    if (fileAuthors.ContainsKey(filePath))
                    {
                        fileAuthors[filePath].UnionWith(authors);
                    }
                    else
                    {
                        fileAuthors[filePath] = authors;
                    }
                }
                foreach (var entry in fileAuthors)
                {
                    foreach (var node in graph.Nodes())
                    {

                        if (node.ID.Replace('\\', '/') == entry.Key)
                        {
                            node.SetInt("Number of Developers", entry.Value.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the number of times each file was changed for each file changed between two commits and adds it as a metric to <paramref name="graph"/>
        /// </summary>
        /// <param name="graph">Graph where to add the metrics</param>
        /// <param name="repositoryPath">Path of the repository</param>
        /// <param name="oldCommit">Commit hash of the older Commit</param>
        /// <param name="newCommit">Commit hash of the newer Commit</param>
        protected static void AddCommitFrequencyMetric(Graph graph, String repositoryPath, String oldCommit, String newCommit)
        {
            using (var repo = new Repository(repositoryPath))
            {
                var commit1 = repo.Lookup<Commit>(oldCommit);
                var commit2 = repo.Lookup<Commit>(newCommit);

                var commitsBetween = repo.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = commit2,
                    ExcludeReachableFrom = commit1
                });

                Dictionary<string, int> fileCommitCounts = new Dictionary<string, int>();

                foreach (var commit in commitsBetween)
                {
                    foreach (var parent in commit.Parents)
                    {
                        var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                        foreach (var change in changes)
                        {
                            var filePath = change.Path;
                            if (fileCommitCounts.ContainsKey(filePath))
                                fileCommitCounts[filePath]++;
                            else
                                fileCommitCounts.Add(filePath, 1);
                        }
                    }
                }

                foreach (var entry in fileCommitCounts.OrderByDescending(x => x.Value))
                {
                    foreach (var node in graph.Nodes())
                    {
                        if (node.ID.Replace('\\', '/') == entry.Key)
                        {
                            node.SetInt("Commit Frequency", entry.Value);
                        }
                    }
                }
            }
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

                return SEEToken.FromString(fileContent, TokenLanguage.FromFileExtension(Path.GetExtension(filePath)?[1..]));
            }
            else
            {
                // Token does not exist.
                return Enumerable.Empty<SEEToken>();
            }
        }

        /// <summary>
        /// Calculates the McCabe cyclomatic complexity metric for a given file and adds it as a metric to the corresponding node in <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        protected static void AddMcCabeMetric(Graph graph, Repository repository, string commitID)
        {
            IEnumerable<SEEToken> tokens;

            foreach (Node node in graph.Nodes())
            {
                if (node.Type == "file")
                {
                    string filePath = node.ID.Replace('\\', '/');
                    tokens = RetrieveTokens(filePath, repository, commitID);
                    int complexity = CalculateMcCabeComplexity(tokens);
                    node.SetInt("McCabe Complexity", CalculateMcCabeComplexity(tokens));
                }
            }
        }

        /// <summary>
        /// Calculates the Halstead metrics for a given file and adds them as metrics to the corresponding node in <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph where the metrics should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        protected static void AddHalsteadMetrics(Graph graph, Repository repository, string commitID)
        {
            IEnumerable<SEEToken> tokens;

            foreach (Node node in graph.Nodes())
            {
                if (node.Type == "file")
                {
                    string filePath = node.ID.Replace('\\', '/');
                    tokens = RetrieveTokens(filePath, repository, commitID);
                    (int distinctOperators, int distinctOperands, int totalOperators, int totalOperands, int programVocabulary, int programLength, float estimatedProgramLength, float volume, float difficulty, float effort, float timeRequiredToProgram, float numberOfDeliveredBugs) = CalculateHalsteadMetrics(tokens);
                    node.SetInt("Halstead Distinct Operators", distinctOperators);
                    node.SetInt("Halstead Distinct Operands", distinctOperands);
                    node.SetInt("Halstead Total Operators", totalOperators);
                    node.SetInt("Halstead Total Operands", totalOperands);
                    node.SetInt("Halstead Program Vocabulary", programVocabulary);
                    node.SetInt("Halstead Program Length", programLength);
                    node.SetFloat("Halstead Calculated Estimated Program Length", estimatedProgramLength);
                    node.SetFloat("Halstead Volume", volume);
                    node.SetFloat("Halstead Difficulty", difficulty);
                    node.SetFloat("Halstead Effort", effort);
                    node.SetFloat("Halstead Time Required to Program", timeRequiredToProgram);
                    node.SetFloat("Halstead Number of Delivered Bugs", numberOfDeliveredBugs);
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
            complexity += tokens.Count(t => t.TokenType == SEEToken.Type.Keyword && (t.Text == "if" || t.Text == "for" || t.Text == "else" || t.Text == "while" || t.Text == "case" || t.Text == "&&" || t.Text == "||" || t.Text == "?"));

            // Count nested cases (i.e. switch statements).
            complexity += tokens.Count(t => t.TokenType == SEEToken.Type.Keyword && t.Text == "case");

            return complexity;
        }

        /// <summary>
        /// Calculates the Halstead metrics for provided code.
        /// </summary>
        /// <param name="tokens">The tokens for which the metrics should be calculated.</param>
        /// <returns>Returns the Halstead metrics.</returns>
        private static (int, int, int, int, int, int, float, float, float, float, float, float) CalculateHalsteadMetrics(IEnumerable<SEEToken> tokens)
        {
            // Identify operands (identifiers, keywords and literals).
            HashSet<string> operands = new(tokens.Where(t => t.TokenType == SEEToken.Type.Identifier || t.TokenType == SEEToken.Type.Keyword || t.TokenType == SEEToken.Type.NumberLiteral ||  t.TokenType == SEEToken.Type.StringLiteral).Select(t => t.Text));

            // Identify operators.
            HashSet<string> operators = new(tokens.Where(t => t.TokenType == SEEToken.Type.Punctuation).Select(t => t.Text));

            // Count the total number of operands and operators.
            int totalOperands = tokens.Count(t => t.TokenType == SEEToken.Type.Identifier || t.TokenType == SEEToken.Type.Keyword || t.TokenType == SEEToken.Type.NumberLiteral || t.TokenType == SEEToken.Type.StringLiteral);
            int totalOperators = tokens.Count(t => t.TokenType == SEEToken.Type.Punctuation);

            // Derivative Halstead metrics.
            int programVocabulary = operators.Count + operands.Count;
            int programLength = totalOperators + totalOperands;
            float estimatedProgramLength = (float)((operators.Count * Math.Log(operators.Count, 2) + operands.Count * Math.Log(operands.Count, 2)));
            float volume = (float)(programLength * Math.Log(programVocabulary, 2));
            float difficulty = operators.Count == 0 ? 0 : operators.Count / 2.0f * (totalOperands / (float)operands.Count);
            float effort = difficulty * volume;
            float timeRequiredToProgram = effort / 18.0f;
            float numberOfDeliveredBugs = volume / 3000.0f;

            return (operators.Count, operands.Count, totalOperators, totalOperands, programVocabulary, programLength, estimatedProgramLength, volume, difficulty, effort, timeRequiredToProgram, numberOfDeliveredBugs);
        }

        /// <summary>
        /// Calculates the number of lines of code for the provided token stream, excluding comments and adds it as a metric to the corresponding node in <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the fileContent is retrieved.</param>
        /// <param name="commitID">The commitID where the files exist.</param>
        protected static void AddLinesOfCodeMetric(Graph graph, Repository repository, string commitID)
        {
            IEnumerable<SEEToken> tokens;

            foreach (Node node in graph.Nodes())
            {
                if (node.Type == "file")
                {
                    string filePath = node.ID.Replace('\\', '/');
                    tokens = RetrieveTokens(filePath, repository, commitID);
                    int linesOfCode = CalculateLinesOfCode(tokens);
                    node.SetInt("Lines of Code", linesOfCode);
                }
            }
        }

        /// <summary>
        /// Calculates the number of lines of code for the provided token stream, excluding comments.
        /// </summary>
        /// <param name="tokens">The tokens for which the lines of code should be counted.</param>
        /// <returns>Returns the number of lines of code.</returns>
        private static int CalculateLinesOfCode(IEnumerable<SEEToken> tokens)
        {
            int linesOfCode = 0;

            foreach (SEEToken token in tokens)
            {
                if (token.TokenType == SEEToken.Type.Newline)
                {
                    linesOfCode++;
                }
                else if (token.TokenType == SEEToken.Type.Comment)
                {
                    linesOfCode--;
                }
            }

            return linesOfCode;
        }
    }
}