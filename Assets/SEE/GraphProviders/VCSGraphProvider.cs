using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.GraphProviders
{

    /// <summary>
    /// Implements the functionality for adding file paths with a specified inclusion type.
    /// </summary>
    [Serializable]
    public class AddPath
    {
        /// <summary>
        /// The inclusiontype.
        /// </summary>
        public enum InclusionType
        {
            Included,
            Excluded
        }

        /// <summary>
        /// The fileType that gets included/excluded.
        /// </summary>
        public string fileType;

        /// <summary>
        /// The inclusiontype that gets selected.
        /// </summary>
        [HorizontalGroup("InclusionType")]
        [EnumToggleButtons]
        public InclusionType inclusionType;
    }

    /// <summary>
    /// Provides a version control system graph based on a git repository.
    /// </summary>
    public class VCSGraphProvider : GraphProvider
    {
        //ToDo: Include, and fix. Currently it does not get safed.
        /// <summary>
        /// The path to the git repository.
        /// </summary>
        //[ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker]
        //private static readonly FilePath RepositoryPath = new();

        /// <summary>
        /// The commit id against which to compare.
        /// </summary>
        //[ShowInInspector, Tooltip("The commit id against which to compare."), HideReferenceObjectPicker]
        //private static string OldCommitID;

        /// <summary>
        /// The new commit id.
        /// </summary>
        //[ShowInInspector, Tooltip("The new commit id."), HideReferenceObjectPicker]
        //private static string NewCommitID;

        /// <summary>
        /// The List of filetypes that get included/excluded.
        /// </summary>
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true), Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        private static List<AddPath> pathGlobbing = new();

        /// <summary>
        /// Loads the metrics available at the Axivion Dashboard into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            return await UniTask.FromResult<Graph>(GetVCSGraph());
            //return GetVCSGraph();
        }

        static Graph GetVCSGraph()
        {
            //TODO: Only for test, we need to get the repositoryPath from the user.
            string assetsPfad = Application.dataPath;
            string repositoryPath = System.IO.Path.GetDirectoryName(assetsPfad);
            string[] pathSegments = repositoryPath.Split(Path.DirectorySeparatorChar);
            string oldCommit = ""; // give the 2 commits you wanna compare
            string newCommit = "";
            Debug.Log(repositoryPath);
            Graph graph = new(repositoryPath, pathSegments[^1]);
            // The main directory.
            NewNode(graph, pathSegments[^1], "directory");

            IEnumerable<string> includedFiles = pathGlobbing
                .Where(path => path.inclusionType == AddPath.InclusionType.Included)
                .Select(path => path.fileType);

            IEnumerable<string> excludedFiles = pathGlobbing
                .Where(path => path.inclusionType == AddPath.InclusionType.Excluded)
                .Select(path => path.fileType);
            using (Repository repo = new(repositoryPath))
            {
                // Get all files using "git ls-files".
                //TODO: I limited the output to 200 for testing, because SEE is huge.
                IEnumerable<string> files;
                if (includedFiles.Any() && !string.IsNullOrEmpty(includedFiles.First()))
                {
                    files = repo.Index.Select(entry => entry.Path).Where(path => includedFiles.Contains(Path.GetExtension(path))).Take(200);
                }
                else if (excludedFiles.Any())
                {
                    files = repo.Index.Select(entry => entry.Path).Where(path => !excludedFiles.Contains(Path.GetExtension(path))).Take(200);
                }
                else
                {
                    files = repo.Index.Select(entry => entry.Path).Where(path => !string.IsNullOrEmpty(path)).Take(200);
                }

                Debug.Log(files.Count());
                Debug.Log(graph.BasePath);
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

                    AddMcCabeMetric(graph, repositoryPath);
                    AddHalsteadMetrics(graph, repositoryPath);
                }
                //TODO: Only for testing.
                Debug.Log(graph.ToString());
            }

            AddLineofCodeChurnMetric(graph, repositoryPath, oldCommit, newCommit);
            AddNumberofDevelopersMetric(graph, repositoryPath, oldCommit, newCommit);
            AddCommitFrequencyMetric(graph, repositoryPath, oldCommit, newCommit);

            return graph;
        }
        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.VCS;
        }
        /// <summary>
        /// Label of attribute <see cref="OverrideMetrics"/> in the configuration file.
        /// </summary>
        private const string overrideMetricsLabel = "OverrideMetrics";

        /// <summary>
        /// Label of attribute <see cref="IssuesAddedFromVersion"/> in the configuration file.
        /// </summary>
        private const string issuesAddedFromVersionLabel = "IssuesAddedFromVersion";
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(GetVCSGraph(), overrideMetricsLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {

        }
        /// <summary>
        /// Creates a new node for each element of a filepath, that does not
        /// already exists in the graph.
        /// </summary>
        /// <param name="path">the remaining part of the path</param>
        /// <param name="parent">the parent node from the current element of the path</param>
        /// <param name="parentPath">the path of the current parent, which will eventually be part of the ID</param>
        /// <param name="graph">the graph to which the new node belongs to</param>
        /// <param name="mainNode">the root node of the main directory</param>
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
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory"));
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
        /// <param name="graph">where to add the node</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <param name="name">the name of the node</param>
        /// <param name="length">the length of the graph element, measured in number of lines</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node NewNode(Graph graph, string id, string type = "Routine", string name = null, int? length = null)
        {
            Node result = new()
            {
                SourceName = id,
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
        /// <param name="graph">where to add the node</param>
        /// <param name="parent">the parent of the new node; must not be null</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <param name="name">the name of the node</param>
        /// <param name="length">the length of the graph element, measured in number of lines</param>
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
        /// Calculates the McCabe cyclomatic complexity metric for a given file and adds it as a metric to the corresponding node.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="filePath">The path to the file for which the metric should be calculated.</param>
        protected static void AddMcCabeMetric(Graph graph, string filePath)
        {
            string fileContent = File.ReadAllText(filePath);
            int complexity = CalculateMcCabeComplexity(fileContent);

            foreach (var node in graph.Nodes())
            {
                if (node.ID.Replace('\\', '/') == filePath)
                {
                    node.SetInt("McCabe Complexity", complexity);
                }
            }
        }

        /// <summary>
        /// Calculates the Halstead metrics for a given file and adds them as metrics to the corresponding node.
        /// </summary>
        /// <param name="graph">The graph where the metrics should be added.</param>
        /// <param name="filePath">The path to the file for which the metrics should be calculated.</param>
        protected static void AddHalsteadMetrics(Graph graph, string filePath)
        {
            string fileContent = File.ReadAllText(filePath);
            (int distinctOperators, int distinctOperands, int totalOperators, int totalOperands) = CalculateHalsteadMetrics(fileContent);

            foreach (var node in graph.Nodes())
            {
                if (node.ID.Replace('\\', '/') == filePath)
                {
                    node.SetInt("Halstead Distinct Operators", distinctOperators);
                    node.SetInt("Halstead Distinct Operands", distinctOperands);
                    node.SetInt("Halstead Total Operators", totalOperators);
                    node.SetInt("Halstead Total Operands", totalOperands);
                }
            }
        }

        /// <summary>
        /// Calculates the McCabe cyclomatic complexity for provided code.
        /// </summary>
        /// <param name="code">The code for which the complexity should be calculated.</param>
        /// <returns>Returns the McCabe cyclomatic complexity.</returns>
        private static int CalculateMcCabeComplexity(string code)
        {
            int complexity = 1; // Starting complexity for a single method or function.

            // Count decision points (if, for, while, case, &&, ||, ?, ternary operator).
            complexity += Regex.Matches(code, @"\b(if|else|for|while|case|&&|\|\||\?)\b").Count;

            // Count nested cases (i.e. switch statements).
            complexity += Regex.Matches(code, @"\bcase\b").Count;

            return complexity;
        }

        /// <summary>
        /// Calculates the Halstead metrics for provided code.
        /// </summary>
        /// <param name="code">The code for which the metrics should be calculated.</param>
        /// <returns>Returns distinct operators and operands and their total amounts.</returns>
        private static (int, int, int, int) CalculateHalsteadMetrics(string code)
        {
            // Remove comments and string literals.
            code = Regex.Replace(code, @"/\*.*?\*/|//.*?$|"".*?""", string.Empty, RegexOptions.Multiline);

            // Identify operands (identifiers and literals).
            var operands = new HashSet<string>(Regex.Matches(code, @"\b\w+\b|\d+(\.\d+)?")
            // Convert into matches.
            .Cast<Match>()
            // Projects each match object to a value, containing matched text as string.
            .Select(m => m.Value));

            // Identify operators (everything else but whitespace).
            var operators = new HashSet<string>(Regex.Matches(code, @"\S+")
            .Cast<Match>()
            .Select(m => m.Value)
            .Except(operands));

            return (operators.Count, operands.Count, operators.Count, operands.Count);
        }
    }
}