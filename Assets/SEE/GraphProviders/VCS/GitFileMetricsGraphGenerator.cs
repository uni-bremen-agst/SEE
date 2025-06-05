using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Generates a <see cref="Graph"/> from the metrics of a <see cref="GitFileMetricProcessor"/>
    /// instance.
    /// </summary>
    public static class GitFileMetricsGraphGenerator
    {
        /// <summary>
        /// Fills and adds all files and their metrics from <paramref name="fileToMetrics"/>
        /// to the passed graph <paramref name="initialGraph"/>.
        /// </summary>
        /// <param name="fileToMetrics">The metrics to add.</param>
        /// <param name="initialGraph">The initial graph where the files and metrics should be generated.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="simplifyGraph">If the final graph should be simplified.</param>
        public static void FillGraphWithGitMetrics(IDictionary<string, GitFileMetrics> fileToMetrics, Graph initialGraph,
            string repositoryName, bool simplifyGraph)
        {
            FillGraphWithGitMetrics(fileToMetrics, initialGraph, repositoryName, simplifyGraph, "");
        }

        /// <summary>
        /// Fills and adds all files and their metrics from <paramref name="fileToMetrics"/>
        /// to the passed graph <paramref name="initialGraph"/>.
        /// </summary>
        /// <param name="fileToMetrics">The metrics to add.</param>
        /// <param name="initialGraph">The initial graph where the files and metrics should be generated.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="simplifyGraph">If the final graph should be simplified.</param>
        /// <param name="idSuffix">A suffix to add to all nodes. This can be used when the same repository is
        /// loaded in two code cities at the same time.</param>
        public static void FillGraphWithGitMetrics
            (IDictionary<string, GitFileMetrics> fileToMetrics,
            Graph initialGraph,
            string repositoryName,
            bool simplifyGraph,
            string idSuffix)
        {
            if (initialGraph == null || fileToMetrics == null)
            {
                return;
            }

            foreach (KeyValuePair<string, GitFileMetrics> file in fileToMetrics)
            {
                Node n
                    = GraphUtils.GetOrAddNode(file.Key, initialGraph.GetNode(repositoryName + idSuffix),
                                              initialGraph, idSuffix: idSuffix);
                n.SetInt(DataModel.DG.VCS.NumberOfDevelopers, file.Value.Authors.Count);
                n.SetInt(DataModel.DG.VCS.CommitFrequency, file.Value.NumberOfCommits);
                n.SetInt(DataModel.DG.VCS.Churn, file.Value.Churn);
                n.SetInt(DataModel.DG.VCS.TruckNumber, file.Value.TruckFactor);
                if (file.Value.Authors.Any())
                {
                    n.SetString(DataModel.DG.VCS.AuthorAttributeName, String.Join(',', file.Value.Authors));
                }

                foreach (KeyValuePair<FileAuthor, int> authorChurn in file.Value.AuthorsChurn)
                {
                    n.SetInt(DataModel.DG.VCS.Churn + ":" + authorChurn.Key, authorChurn.Value);
                }
            }

            if (simplifyGraph)
            {
                foreach (Node child in initialGraph.GetRoots()[0].Children().ToList())
                {
                    SimplifyGraph(child);
                }
            }
        }

        /// <summary>
        /// Simplifies a given graph by combining common directories (nodes of type
        /// <see cref="DataModel.DG.VCS.DirectoryType"/>.
        ///
        /// If a directory has only other directories as children, their paths will be combined.
        /// For instance the file structure:
        /// <code>
        /// root/
        ///├─ dir1/
        ///│  ├─ dir2/
        ///│  │  ├─ dir6/
        ///│  ├─ dir3/
        ///│  │  ├─ file1.md
        ///│  │  ├─ dir5/
        ///  ├─ dir4/
        /// </code>
        /// would become:
        /// <code>
        ///root/
        /// ├─ dir1/dir2/dir6/
        /// ├─ dir1/dir4/
        /// ├─ dir1/dir3/
        /// │  ├─ file1.md
        /// │  ├─ dir5/
        /// </code>
        ///
        /// </summary>
        /// <param name="root">The root element of the graph to analyse from.</param>
        private static void SimplifyGraph(Node root)
        {
            Graph graph = root.ItsGraph;
            IList<Node> children = root.Children();
            if (children.ToList().TrueForAll(x => x.Type != DataModel.DG.VCS.FileType) && children.Any())
            {
                foreach (Node child in children.ToList())
                {
                    child.Reparent(root.Parent);
                    SimplifyGraph(child);
                }

                if (graph.ContainsNode(root))
                {
                    graph.RemoveNode(root);
                }
            }
            else
            {
                foreach (Node node in children.Where(x => x.Type == DataModel.DG.VCS.DirectoryType).ToList())
                {
                    SimplifyGraph(node);
                }
            }
        }
    }
}
