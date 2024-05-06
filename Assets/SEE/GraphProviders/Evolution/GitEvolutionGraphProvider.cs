using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    public class GitEvolutionGraphProvider : GitRepositoryProvider<List<Graph>>
    {
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

            using (var repo = new Repository(RepositoryPath.Path))
            {
              
                
                List<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    .Where(commit => commit.Parents.Count() == 1)
                    .Reverse()
                    .ToList();
                foreach (var currentCommit in commitList)
                {
                    Graph g = new Graph(RepositoryPath.Path);
                    g.AddNode(new Node(){SourceName = "AAAAAA", ID = "AAA"});
                    g.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyyy"));
                    // All commits between the first commit in commitList and the current commit
                    List<Commit> commitsInBetween =
                        commitList.GetRange(0, commitList.FindIndex(x => x.Sha == currentCommit.Sha) + 1);
                    graph.Add(g);
                    Debug.Log("aaaaa"); 
                }
            }

            return graph;
        }

        public override GraphProviderKind GetKind()
        {
            throw new System.NotImplementedException();
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