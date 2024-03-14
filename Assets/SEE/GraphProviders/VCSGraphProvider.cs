using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE.DataModel.DG;
using LibGit2Sharp;
using System.IO;
using System.Linq;
using SEE.Game.City;
using Cysharp.Threading.Tasks;
using SEE.Utils.Config;
using System;
using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
using SEE.Utils.Paths;
using SEE;

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
            string commit1Sha = ""; // give the 2 commits you wanna compare
            string commit2Sha = "";

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
                    (includedFiles.Any() &&  includedFiles.Contains(Path.GetExtension(path)))) ||
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
            //compare 2 commits and save the changes
            using (var repo = new Repository(repositoryPath))
            {
                var commit1 = repo.Lookup<Commit>(commit1Sha);
                var commit2 = repo.Lookup<Commit>(commit2Sha);

                var changes = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree);

                foreach (var change in changes)
                {
                    Debug.Log($"{change.Path}: {change.LinesAdded} lines added, {change.LinesDeleted} lines deleted");
                }
            }
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
    }
}
