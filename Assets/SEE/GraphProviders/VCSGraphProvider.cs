using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.GraphProviders
{
    // Enum für die Auswahl zwischen Inklusion und Exklusion
    public enum InclusionType
    {
        Included,
        Excluded
    }

    [Serializable]
    public class AddPath
    {
        public string fileType;
        [HorizontalGroup("InclusionType")]
        [EnumToggleButtons]
        public InclusionType inclusionType;
    }

    public class VCSGraphProvider : GraphProvider
    {
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true), Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        private static List<AddPath> pathGlobbing = new List<AddPath>();

        /// <summary>
        /// Loads the metrics available at the Axivion Dashboard into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            Debug.Log(GetVCSGraph().ToString());

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

            Graph graph = NewGraph();
            // The main directory.
            NewNode(graph, pathSegments[^1], "directory");

            var includedFiles = pathGlobbing
                .Where(path => path.inclusionType == InclusionType.Included)
                .Select(path => path.fileType);

            var excludedFiles = pathGlobbing
                .Where(path => path.inclusionType == InclusionType.Excluded)
                .Select(path => path.fileType);

            using (var repo = new Repository(repositoryPath))
            {
                // Get all files using "git ls-files".
                //TODO: I limited the output to 200 for testing, because SEE is huge.
                var files = repo.Index
                    .Select(entry => entry.Path)
                    .Where(path => !string.IsNullOrEmpty(path) &&
                    ((excludedFiles.Any() && excludedFiles.Contains(Path.GetExtension(path))) &&
                    (includedFiles.Any() && includedFiles.Contains(Path.GetExtension(path)))) ||
                    ((excludedFiles.Any() && !excludedFiles.Contains(Path.GetExtension(path))) ||
                    (!excludedFiles.Any() && (includedFiles.Any() && includedFiles.Contains(Path.GetExtension(path)))) ||
                    (!excludedFiles.Any() && !includedFiles.Any())))
                    .ToList().Take(200);
                Debug.Log(files.Count());
                // Build the graph structure.
                foreach (var filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    string[] filePathSegments = filePath.Split(Path.AltDirectorySeparatorChar);
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1]).AddChild(NewNode(graph, filePath, "file"));
                    }
                    // Other directorys/files.
                    else
                    {
                        BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(pathSegments[^1]));
                    }
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
            return GraphProviderKind.Dashboard;
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
        //TODO: Documentation.
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
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory"));
                    BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
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
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "directory"));
                    BuildGraphFromPath(nodePath, graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
                }
                // The node for the current pathSegment does not exist, and the node is file.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null && pathSegments.Length == 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "file"));
                }
            }
        }

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">where to add the node</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node NewNode(Graph graph, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node result = new()
            {
                SourceName = id,
                ID = id,
                Type = type,
                Directory = directory,
                Filename = filename,
                SourceLine = line,
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
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node Child(Graph graph, Node parent, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node child = NewNode(graph, id, type, directory, filename, line, length);
            parent.AddChild(child);
            return child;
        }
        //TODO: Only for testing.
        /// <summary>
        /// Creates a new graph with default basepath and graph name.
        /// </summary>
        /// <param name="viewName">the name of the graph</param>
        /// <param name="basePath">the basepath of the graph for looking up the source code files</param>
        /// <returns>new graph</returns>
        protected static Graph NewGraph(string viewName = "CodeFacts", string basePath = "DUMMYBASEPATH")
        {
            return new Graph(basePath, viewName);
        }

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
    }
}