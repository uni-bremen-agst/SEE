using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    [Serializable]
    public class FullVCSGraphProvider : GraphProvider
    {
        [OdinSerialize]
        [ShowInInspector, Tooltip("Path to the git repository."), HideReferenceObjectPicker,
         RuntimeTab(GraphProviderFoldoutGroup)]
        public DirectoryPath RepositoryPath = new();

        /// <summary>
        /// The date limit until commits should be analysed
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD-MM-YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
         Tooltip("Paths and their inclusion/exclusion status."), RuntimeTab(GraphProviderFoldoutGroup),
         HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "", false }
        };


        [OdinSerialize] [ShowInInspector] public int AuthorThreshhold { get; set; } = 1;

        [OdinSerialize] [ShowInInspector] public int CommitThreshhold { get; set; } = 1;

        [OdinSerialize] [ShowInInspector] public bool SimplifyGraph { get; set; }

        [OdinSerialize] [ShowInInspector] public bool AutoFetch { get; set; }

        public override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            UniTask<Graph> graphTask = UniTask.FromResult(GetGraph(graph));

            return graphTask;
        }

        private Graph GetGraph(Graph graph)
        {
            graph.BasePath = RepositoryPath.Path;
            string[] pathSegments = RepositoryPath.Path.Split(Path.DirectorySeparatorChar);

            VCSGraphProvider.NewNode(graph, pathSegments[^1], "Repository", pathSegments[^1]);

            //var mainNode = graph.GetNode(pathSegments[^1]);
            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            IEnumerable<string> includedFiles = PathGlobbing
                .Where(path => path.Value == true)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = PathGlobbing
                .Where(path => path.Value == false)
                .Select(path => path.Key);

            using (var repo = new Repository(RepositoryPath.Path))
            {
                //string[] pathSegments = RepositoryPath.Path.Split(Path.DirectorySeparatorChar);

                List<Commit> commitList = new();

                // Iterate over each commit in each branch until time limit is reached
                // Assuming, that commits are sorted chronological.
                foreach (var branch in repo.Branches)
                {
                    foreach (var commit in branch.Commits)
                    {
                        // Assuming that git log is in a 
                        if (DateTime.Compare(commit.Author.When.Date, timeLimit) < 0)
                        {
                            break;
                        }

                        commitList.Add(commit);
                    }
                }


                // remove duplicates
                commitList = commitList
                    .GroupBy(x => x.Sha)
                    .Select(x => x.First())
                    //.Where(x => x.Parents.Count() <= 1)
                    .ToList();


                Dictionary<string, GitFileMetricsCollector> fileMetrics = new();

                foreach (var commit in commitList)
                {
                    var changedFilesPath = repo.Diff.Compare<TreeChanges>(commit.Tree, commit.Parents.First().Tree)
                        .Select(y => y.Path);

                    foreach (var changedFile in changedFilesPath)
                    {
                        if (!includedFiles.Contains(Path.GetExtension(changedFile)) ||
                            excludedFiles.Contains(Path.GetExtension(changedFile)))
                        {
                            continue;
                        }

                        if (!fileMetrics.ContainsKey(changedFile))
                        {
                            fileMetrics.Add(changedFile,
                                new GitFileMetricsCollector(1, new HashSet<string> { commit.Author.Email }));
                        }
                        else
                        {
                            fileMetrics[changedFile].NumberOfCommits += 1;
                            fileMetrics[changedFile].Authors.Add(commit.Author.Email);
                        }
                    }
                }

                foreach (var file in fileMetrics)
                {
                    //analoge tp VCSGraphÜProvider
                    string[] filePathSegments = file.Key.Split(Path.AltDirectorySeparatorChar);
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1])
                            .AddChild(VCSGraphProvider.NewNode(graph, file.Key, "file", file.Key));
                    }
                    else
                    {
                        Node n = VCSGraphProvider.BuildGraphFromPath(file.Key, null, null, graph,
                            graph.GetNode(pathSegments[^1]));
                        n.SetInt("Metric.Authors.Number", file.Value.Authors.Count);
                        n.SetInt("Metric.File.Commits", file.Value.NumberOfCommits);
                    }
                }

                // foreach (var file in fileMetrics)
                // {
                //     string[] pathSplit = file.Key.Split(Path.AltDirectorySeparatorChar);
                //     string nodePath = string.Join(Path.DirectorySeparatorChar.ToString(), pathSplit, 0,
                //         pathSplit.Length);
                //
                //     GraphSearch search = new GraphSearch(graph);
                //     var n = graph.GetNode(nodePath);
                //     n.SetInt("Metric.Authors.Number", file.Value.Authors.Count);
                //     n.SetInt("Metric.File.Commits", file.Value.NumberOfCommits);
                // }

                if (SimplifyGraph)
                {
                    foreach (var child in graph.GetRoots().First().Children().ToList())
                    {
                        DoSimplyfiGraph(child, graph);
                    }
                }

                //  SimplyfiGraph(graph.GetRoots().First());
                Debug.Log($"{fileMetrics.Count}");
            }

            return graph;
        }

        private void DoSimplyfiGraph(Node root, Graph g)
        {
            if (root.Children().ToList().All(x => x.Type != "file"))
            {
                foreach (var child in root.Children().ToList())
                {
                    child.Reparent(root.Parent);
                    // child.ID = root.ID + Path.AltDirectorySeparatorChar + child.ID;
                    DoSimplyfiGraph(child, g);

                    //root.Children().Remove(root);
                }

                if (g.ContainsNode(root))
                {
                    g.RemoveNode(root);
                }
            }
            else
            {
                foreach (var node in root.Children().Where(x => x.Type == "directory").ToList())
                {
                    DoSimplyfiGraph(node, g);
                }
            }
        }

        // private Node GetNode(string path, Graph graph, Node current = null)
        // {
        //     string[] pathSplit = path.Split(Path.AltDirectorySeparatorChar);
        //     if (pathSplit.Length == 1)
        //     {
        //         return current.Children().First(x => x.ID == pathSplit.First());
        //     }
        // }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.VCS;
        }

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(PathGlobbing.ToString()) ? null : PathGlobbing;
            writer.Save(pathGlobbing, pathGlobbingLabel);
        }

        private const string pathGlobbingLabel = "PathGlobbing";

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GitFileMetricsCollector
    {
        public int NumberOfCommits { get; set; }

        public HashSet<string> Authors { get; set; }

        public GitFileMetricsCollector(int numberOfCommits, HashSet<string> authors)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
        }
    }
}