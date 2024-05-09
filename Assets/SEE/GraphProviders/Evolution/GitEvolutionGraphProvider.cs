using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    [Serializable]
    public class GitEvolutionGraphProvider : GitRepositoryProvider<List<Graph>>
    {
        private const string NumberOfAuthorsMetricName = "Metric.Authors.Number";

        private const string NumberOfCommitsMetricName = "Metric.File.Commits";


        /// <summary>
        /// The date limit until commits should be analysed
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD-MM-YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        public override UniTask<List<Graph>> ProvideAsync(List<Graph> graph, AbstractSEECity city) =>
            UniTask.FromResult(GetGraph(graph));


        private List<Graph> GetGraph(List<Graph> graph)
        {
            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);


            IEnumerable<string> includedFiles = PathGlobbing
                .Where(path => path.Value == true)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = PathGlobbing
                .Where(path => path.Value == false)
                .Select(path => path.Key);
            string[] pathSegments = RepositoryPath.Path.Split(Path.DirectorySeparatorChar);

            using (var repo = new Repository(RepositoryPath.Path))
            {
                List<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    .Where(commit => commit.Parents.Count() == 1)
                    .Reverse()
                    .ToList();
                int counter = 1;

                Dictionary<Commit, List<PatchEntryChanges>> commitChanges = new();
                foreach (var commit in commitList)
                {
                    commitChanges.Add(commit, GetFileChanges(commit, repo));
                }

                foreach (var currentCommit in commitList)
                {
                    Graph g = new Graph(RepositoryPath.Path);
                    g.BasePath = RepositoryPath.Path;
                    GraphUtils.NewNode(g, pathSegments[^1], "Repository", pathSegments[^1]);

                    g.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyyy"));
                    g.StringAttributes.Add("CommitId", currentCommit.Sha);
                    // All commits between the first commit in commitList and the current commit
                    List<Commit> commitsInBetween =
                        commitList.GetRange(0, commitList.FindIndex(x => x.Sha == currentCommit.Sha) + 1);
                    Dictionary<string, GitFileMetricsCollector> fileMetrics = new();
                    foreach (var commit in commitsInBetween)
                    {
                        //var changedFilesPath = repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);

                        foreach (var changedFile in commitChanges[commit])
                        {
                            string filePath = changedFile.Path;
                            if (!includedFiles.Contains(Path.GetExtension(filePath)) ||
                                excludedFiles.Contains(Path.GetExtension(filePath)))
                            {
                                continue;
                            }

                            if (!fileMetrics.ContainsKey(filePath))
                            {
                                fileMetrics.Add(filePath,
                                    new GitFileMetricsCollector(1, new HashSet<string> { commit.Author.Email },
                                        changedFile.LinesAdded + changedFile.LinesDeleted));
                            }
                            else
                            {
                                fileMetrics[filePath].NumberOfCommits += 1;
                                fileMetrics[filePath].Authors.Add(commit.Author.Email);
                                fileMetrics[filePath].Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                            }
                        }
                    }

                    foreach (var file in fileMetrics)
                    {
                        //analoge tp VCSProvider
                        string[] filePathSegments = file.Key.Split(Path.AltDirectorySeparatorChar);
                        // Files in the main directory.
                        if (filePathSegments.Length == 1)
                        {
                            Node n = GraphUtils.NewNode(g, file.Key, "file", file.Key);
                            g.GetNode(pathSegments[^1])
                                .AddChild(n);
                            n.SetInt(NumberOfAuthorsMetricName, file.Value.Authors.Count);
                            n.SetInt(NumberOfCommitsMetricName, file.Value.NumberOfCommits);
                            n.SetInt("Metric.File.Churn", file.Value.Churn);
                        }
                        else
                        {
                            Node n = GraphUtils.BuildGraphFromPath(file.Key, null, null, g,
                                g.GetNode(pathSegments[^1]));
                            n.SetInt(NumberOfAuthorsMetricName, file.Value.Authors.Count);
                            n.SetInt(NumberOfCommitsMetricName, file.Value.NumberOfCommits);
                            n.SetInt("Metric.File.Churn", file.Value.Churn);
                        }
                    }

                    graph.Add(g);
                }
            }

            return graph;
        }

        
        
        
        private List<PatchEntryChanges> GetFileChanges(Commit commit, Repository repo)
        {
            return repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree).Select(x => x).ToList();
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.GitHistory;
        }

        protected override void SaveAttributes(ConfigWriter writer)
        {
            throw new System.NotImplementedException();
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            throw new System.NotImplementedException();
        }
    }
}