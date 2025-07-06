using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Scanner;
using SEE.Scanner.Antlr;
using SEE.Utils;
using SEE.VCS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Processes and collects all metrics from all files of a git repository.
    /// </example>
    public static class GitGraphGenerator
    {
        /// <summary>
        /// A mapping of filenames (relative paths in a repository) onto their <see cref="GitFileMetrics"/>.
        /// </summary>
        private class FileToMetrics : Dictionary<string, GitFileMetrics> { }

        /// <summary>
        /// Returns a new <see cref="FileToMetrics"/> mapping containing empty <see cref="GitFileMetrics"/>
        /// for each and only the filenames contained in <paramref name="repositoryFiles"/>.
        /// </summary>
        /// <param name="repositoryFiles">A set of a files whose metrics should be calculated</param>
        private static FileToMetrics InitialFileToMetrics(HashSet<string> repositoryFiles)
        {
            FileToMetrics fileToMetrics = new();
            foreach (string file in repositoryFiles)
            {
                fileToMetrics.Add(file, new GitFileMetrics(0, new HashSet<FileAuthor>(), 0, 0));
            }
            return fileToMetrics;
        }

        #region Truck Factor
        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for
        /// </summary>
        private const float truckFactorCoreDevRatio = 0.8f;

        /// <summary>
        /// Calculates and adds the truck factor of all files in <paramref name="fileToMetrics"/>.
        /// </summary>
        /// <param name="fileToMetrics">Where to add the truck factor</param>
        private static void CalculateTruckFactor(FileToMetrics fileToMetrics)
        {
            foreach (KeyValuePair<string, GitFileMetrics> file in fileToMetrics)
            {
                file.Value.TruckFactor = CalculateTruckFactor(file.Value.AuthorsChurn);
            }
        }

        /// <summary>
        /// Calculates the truck factor based on a LOC-based heuristic by Yamashita et al. (2015)
        /// for estimating the coreDev set. Cited by Ferreira et al.
        ///
        /// Source/Math: https://doi.org/10.1145/2804360.2804366, https://doi.org/10.1007/s11219-019-09457-2
        /// </summary>
        /// <param name="developersChurn">The churn of each developer</param>
        /// <returns>The calculated truck factor</returns>
        private static int CalculateTruckFactor(IDictionary<FileAuthor, int> developersChurn)
        {
            if (developersChurn.Count == 0)
            {
                return 0;
            }

            int totalChurn = developersChurn.Select(x => x.Value).Sum();

            HashSet<FileAuthor> coreDevs = new();

            float cumulativeRatio = 0;

            // Sorting devs by their number of changed files
            List<FileAuthor> sortedDevs =
                developersChurn
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();

            // Selecting the coreDevs which are responsible for at least 80% of the total churn of a file
            while (cumulativeRatio <= truckFactorCoreDevRatio)
            {
                FileAuthor dev = sortedDevs.First();
                cumulativeRatio += (float)((float)developersChurn[dev] / totalChurn);
                coreDevs.Add(dev);
                sortedDevs.Remove(dev);
            }

            return coreDevs.Count;
        }

        #endregion Truck Factor

        #region Author Aliasing
        /// <summary>
        /// If <paramref name="consultAliasMap"/> is false, the original <paramref name="author"/>
        /// will be returned.
        /// Otherwise: Returns the alias of the specified <paramref name="author"/> if it exists in the alias mapping;
        /// or else returns the original <paramref name="author"/>.
        ///
        /// For two <see cref="FileAuthor"/>s to match, they must have same name and email address,
        /// where the string comparison for both facets is case-insensitive.
        /// </summary>
        /// <param name="author">The author whose alias is to be retrieved.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false.</param>
        /// <returns>A <see cref="FileAuthor"/> instance representing the alias of the author if found,
        /// or the original author if no alias exists or if <paramref name="consultAliasMap"/> is false.</returns>
        private static FileAuthor GetAuthorAliasIfExists(FileAuthor author, bool consultAliasMap, AuthorMapping authorAliasMap)
        {
            // FIXME: Move this code to GitRepository.
            // If the author is not in the alias map or combining of author aliases is disabled, use the original author
            return ResolveAuthorAliasIfEnabled(author, consultAliasMap, authorAliasMap) ?? author;

            static FileAuthor ResolveAuthorAliasIfEnabled(FileAuthor author, bool combineSimilarAuthors, AuthorMapping authorAliasMap)
            {
                if (!combineSimilarAuthors)
                {
                    return null;
                }

                return authorAliasMap
                    .FirstOrDefault(alias => alias.Value.Any(x => String.Equals(x.Email, author.Email, StringComparison.OrdinalIgnoreCase)
                                                               && String.Equals(x.Name,  author.Name,  StringComparison.OrdinalIgnoreCase))).Key;
            }
        }
        #endregion Author Aliasing

        /// <summary>
        /// Adds nodes of type <see cref="DataModel.DG.VCS.FileType"/> and <see cref="DataModel.DG.VCS.DirectoryType"/>
        /// to the <paramref name="graph"/> for the relevant files in the given <paramref name="repository"/>
        /// present at the given <paramref name="commitID"/>. Metrics are added, too. The <see cref="GitFileMetrics"/>
        /// are calulated relative to <paramref name="baselineCommitID"/>.
        ///
        /// </summary>
        /// <param name="graph">Where to add the nodes.</param>
        /// <param name="simplifyGraph">If true, single chains of directory nodes in the node hierarchy
        /// will be collapsed into the inner most directory node</param>
        /// <param name="repository">The repository from which the nodes and metrics are derived.</param>
        /// <param name="commitID">The commit id at which the files must exist.</param>
        /// <param name="baselineCommitID">The commit id of the baseline against which to gather
        /// the VCS metrics</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an author alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>the input <paramref name="graph"/> with the added nodes</returns>
        internal static Graph AddNodesForCommit
            (Graph graph,
             bool simplifyGraph,
             GitRepository repository,
             string commitID,
             string baselineCommitID,
             bool consultAliasMap,
             AuthorMapping authorAliasMap,
             Action<float> changePercentage = null,
             CancellationToken token = default)
        {
            string repositoryPath = repository.RepositoryPath.Path;
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);

            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                graph.BasePath = repositoryPath;
            }
            graph.Name = repositoryName;
            graph.CommitID(commitID);
            graph.RepositoryPath(repositoryPath);

            float percentage = 0.05f;
            changePercentage?.Invoke(percentage);

            // Get all files using "git ls-tree -r <CommitID> --name-only".
            HashSet<string> files = repository.AllFiles(commitID, token);
            percentage = 0.2f;
            changePercentage?.Invoke(percentage);

            FileToMetrics fileToMetrics = Prepare(graph, files);

            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            // Includes all commits between the baseline commit and the commitID
            // including the commitID itself but excluding the baseline commit.
            IEnumerable<Commit> commitList = repository.CommitsBetween(baselineCommitID, commitID);
            percentage = 0.3f;
            changePercentage?.Invoke(percentage);

            int commitCount = commitList.Count();
            int currentCommitIndex = 0;

            foreach (Commit commit in commitList)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                UpdateMetricsForCommit(fileToMetrics, repository, commit, consultAliasMap, authorAliasMap);
                currentCommitIndex++;
                changePercentage?.Invoke(Mathf.Clamp((float)currentCommitIndex / commitCount, percentage, 0.98f));
            }

            Finalize(graph, simplifyGraph, repository, repositoryName, fileToMetrics);

            changePercentage?.Invoke(1f);
            return graph;
        }

        /// <summary>
        /// Calculates <see cref="GitFileMetrics"/> for all <paramref name="files"/> and adds
        /// these to their corresponding nodes in the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the file metrics.</param>
        /// <param name="simplifyGraph">If true, single chains of directory nodes in the node hierarchy
        /// will be collapsed into the inner most directory node</param>
        /// <param name="repository">The repository from which the nodes and metrics are derived.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="files">The files for which to calculate the metrics.</param>
        /// <param name="commitsInBetween">The metrics will be gathered for only the commits in this list.</param>
        /// <param name="commitChanges">The changes associated with each commit in <paramref name="commitsInBetween"/>;
        /// for each element in <paramref name="commitsInBetween"/> there must be a corresponding entry in
        /// <paramref name="commitChanges"/>.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        internal static void AddNodesForCommits
            (Graph graph,
             bool simplifyGraph,
             GitRepository repository,
             string repositoryName,
             HashSet<string> files,
             IList<Commit> commitsInBetween,
             IDictionary<Commit, Patch> commitChanges,
             bool consultAliasMap,
             AuthorMapping authorAliasMap)
        {
            FileToMetrics fileToMetrics = Prepare(graph, files);

            foreach (Commit commitInBetween in commitsInBetween)
            {
                UpdateMetricsForPatch(fileToMetrics, commitInBetween, commitChanges[commitInBetween], consultAliasMap, authorAliasMap);
            }

            Finalize(graph, simplifyGraph, repository, repositoryName, fileToMetrics);
        }

        /// <summary>
        /// Adds nodes of type <see cref="DataModel.DG.VCS.FileType"/> and <see cref="DataModel.DG.VCS.DirectoryType"/>
        /// for the relevant files in the given <paramref name="repository"/> present after the given
        /// <paramref name="startDate"/> to the <paramref name="graph"/>. For each added node, the
        /// <see cref="GitFileMetrics"/> are calculated, too.
        /// </summary>
        /// <param name="graph">Where to add the file metrics.</param>
        /// <param name="simplifyGraph">If true, single chains of directory nodes in the node hierarchy
        /// will be collapsed into the inner most directory node</param>
        /// <param name="repository"> The repository from which the nodes and metrics are derived.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="startDate">The date after which commits in the history should be considered.
        /// Older commits will be ignored.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        /// <param name="changePercentage">To report the progress.</param>
        internal static void AddNodesAfterDate
            (Graph graph,
             bool simplifyGraph,
             GitRepository repository,
             string repositoryName,
             DateTime startDate,
             bool consultAliasMap,
             AuthorMapping authorAliasMap,
             Action<float> changePercentage,
             CancellationToken token)
        {
            IList<Commit> commitList = repository.CommitsAfter(startDate);
            HashSet<string> files = repository.AllFiles(token);
            FileToMetrics fileToMetrics = Prepare(graph, files);

            int counter = 0;
            int commitLength = commitList.Count();
            foreach (Commit commit in commitList)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }
                UpdateMetricsForCommit(fileToMetrics, repository, commit, consultAliasMap, authorAliasMap);
                changePercentage?.Invoke(Mathf.Clamp((float)counter / commitLength, 0, 0.98f));
                counter++;
            }
            Finalize(graph, simplifyGraph, repository, repositoryName, fileToMetrics);
            changePercentage?.Invoke(1f);
        }

        /// <summary>
        /// Updates the metrics of <paramref name="fileToMetrics"/> according to the <paramref name="commit"/>
        /// for all files changed by <paramref name="patch"/>.
        /// </summary>
        /// <param name="fileToMetrics">metrics will be calculated for the files therein and added to this map</param>
        /// <param name="commit">The commit that should be processed. Only its <see cref="Commit.Author"/>
        /// will be used.</param>
        /// <param name="patch">The changes the <paramref name="commit"/> has made. This will be most likely the
        /// changes between this commit and its parent. Can be null.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        private static void UpdateMetricsForPatch
            (FileToMetrics fileToMetrics,
            Commit commit,
            Patch patch,
            bool consultAliasMap,
            AuthorMapping authorAliasMap)
        {
            if (patch == null || commit == null)
            {
                return;
            }

            HashSet<string> files = new(fileToMetrics.Keys);

            FileAuthor committer = GetAuthorAliasIfExists(new FileAuthor(commit.Author.Name, commit.Author.Email),
                                                          consultAliasMap, authorAliasMap);

            foreach (PatchEntryChanges changedFile in patch)
            {
                string filePath = changedFile.Path;

                if (!files.Contains(filePath))
                {
                    continue;
                }

                int churn = changedFile.LinesAdded + changedFile.LinesDeleted;

                if (!fileToMetrics.ContainsKey(filePath))
                {
                    // If the file has not been added to the metrics yet, add it.
                    fileToMetrics.Add(filePath,
                        new GitFileMetrics(1,
                            new HashSet<FileAuthor> { committer },
                            changedFile.LinesAdded,
                            changedFile.LinesDeleted));

                    fileToMetrics[filePath].AuthorsChurn.Add(committer, churn);
                }
                else
                {
                    GitFileMetrics changedFileMetrics = fileToMetrics[filePath];
                    changedFileMetrics.NumberOfCommits += 1;
                    changedFileMetrics.LinesAdded += changedFile.LinesAdded;
                    changedFileMetrics.LinesRemoved += changedFile.LinesDeleted;
                    changedFileMetrics.Authors.Add(committer);
                    changedFileMetrics.AuthorsChurn.GetOrAdd(committer, () => 0);
                    changedFileMetrics.AuthorsChurn[committer] += churn;

                    foreach (string otherFilePath in patch
                                 .Where(e => !e.Equals(changedFile))
                                 .Select(x => x.Path))
                    {
                        // Processing the files which were changed together with the current file
                        changedFileMetrics.FilesChangesTogether.GetOrAdd(otherFilePath, () => 0);
                        changedFileMetrics.FilesChangesTogether[otherFilePath]++;
                    }

                    fileToMetrics[filePath].AuthorsChurn[committer] += churn;
                }
            }
        }

        /// <summary>
        /// Initial preparation for gathering the data. Adds a repository node to the graph
        /// and returns an <see cref="FileToMetrics"/> mapping containing all files in <paramref name="files"/>
        /// with <see cref="GitFileMetrics"/> initialized to zero.
        /// </summary>
        /// <param name="graph">Where to add the repository node.</param>
        /// <param name="files">The set of files for which to return the initial <see cref="GitFileMetrics"/></param>
        /// <returns>Mapping of all filenames in <paramref name="files"/> onto zero <see cref="GitFileMetrics"/></returns>
        private static FileToMetrics Prepare(Graph graph, HashSet<string> files)
        {
            return InitialFileToMetrics(files);
        }

        /// <summary>
        /// Finalizes the graph by adding all nodes and metrics from <paramref name="fileToMetrics"/>
        /// to <paramref name="graph"/> and finalizing the node hierarchy.
        /// </summary>
        /// <param name="graph">Where to add the nodes and their metrics.</param>
        /// <param name="simplifyGraph">Whether the resulting graph should be simplified.</param>
        /// <param name="repository">The repository from which the data is to be gathered.</param>
        /// <param name="repositoryName"> The name of the repository to be used as Id to retrieve the root node.
        /// All nodes will be placed under this root.</param>
        /// /// <param name="fileToMetrics">The metric data from which to generate the nodes./param>
        private static void Finalize
            (Graph graph,
            bool simplifyGraph,
            GitRepository repository,
            string repositoryName,
            FileToMetrics fileToMetrics)
        {
            AddNodesAndMetrics(fileToMetrics, graph, simplifyGraph, repositoryName, repository);
            graph.FinalizeNodeHierarchy();
        }

        /// <summary>
        /// If <paramref name="commit"/> is null, nothing happens.
        ///
        /// Otherwise, the metrics will be calculated between <paramref name="commit"/> and
        /// all its parents. If a commit has no parent, <paramref name="commit"/> is the
        /// very first commit in the version history, which is perfectly okay.
        /// </summary>
        /// <param name="fileToMetrics">Metrics will be calculated for the files therein and added to this map</param>
        /// <param name="gitRepository">The diff will be retrieved from this repository.</param>
        /// <param name="commit">The commit that should be processed assumed to belong to <paramref name="gitRepository"/></param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        private static void UpdateMetricsForCommit
            (FileToMetrics fileToMetrics,
             GitRepository gitRepository,
             Commit commit,
             bool consultAliasMap,
             AuthorMapping authorAliasMap)
        {
            if (commit == null)
            {
                return;
            }
            if (commit.Parents.Any())
            {
                foreach (Commit parent in commit.Parents)
                {
                    UpdateMetricsForPatch(fileToMetrics, commit, gitRepository.Diff(parent, commit), consultAliasMap, authorAliasMap);
                }
            }
            else
            {
                UpdateMetricsForPatch(fileToMetrics, commit, gitRepository.Diff(null, commit), consultAliasMap, authorAliasMap);
            }
        }

        /// <summary>
        /// Retrieves the token stream for given file content from its repository and commit ID.
        /// </summary>
        /// <param name="repositoryFilePath">The file path from the node. This must be a relative path
        /// in the syntax of the repository regarding the directory separator</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="language">The language the given text is written in.</param>
        /// <returns>The token stream for the specified file and commit.</returns>
        private static ICollection<AntlrToken> RetrieveTokens
            (string repositoryFilePath,
             GitRepository repository,
             AntlrLanguage language)
        {
            try
            {
                return AntlrToken.FromString(repository.GetFileContent(repositoryFilePath), language);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error retrieving file content for {repositoryFilePath}: {e.Message}");
                return new List<AntlrToken>();
            }
        }

        /// <summary>
        /// Adds Halstead, McCabe, number of tokens and lines of code metrics and comments
        /// to the corresponding node for the supported TokenLanguages in <paramref name="graph"/>.
        /// Otherwise, metrics are not available.
        ///
        /// Note: A file may exist in multiple branches. We will pick the first one we find.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        private static void AddCodeMetrics(Graph graph, GitRepository repository)
        {
            foreach (Node node in graph.Nodes())
            {
                if (node.Type == DataModel.DG.VCS.FileType)
                {
                    string repositoryFilePath = node.ID;
                    AntlrLanguage language = AntlrLanguage.FromFileExtension(Path.GetExtension(repositoryFilePath).TrimStart('.'));
                    if (language != AntlrLanguage.Plain)
                    {
                        ICollection<AntlrToken> tokens = RetrieveTokens(repositoryFilePath, repository, language);
                        TokenMetrics.Gather(tokens,
                                            out TokenMetrics.LineMetrics lineMetrics, out int numberOfTokens,
                                            out int mccabeComplexity, out TokenMetrics.HalsteadMetrics halsteadMetrics);


                        node.SetInt(Metrics.LOC, lineMetrics.LOC);
                        node.SetInt(Metrics.LOC, lineMetrics.Comments);
                        node.SetInt(Metrics.LOC, numberOfTokens);

                        node.SetInt(Metrics.McCabe, mccabeComplexity);

                        node.SetInt(Halstead.DistinctOperators, halsteadMetrics.DistinctOperators);
                        node.SetInt(Halstead.DistinctOperands, halsteadMetrics.DistinctOperands);
                        node.SetInt(Halstead.TotalOperators, halsteadMetrics.TotalOperators);
                        node.SetInt(Halstead.TotalOperands, halsteadMetrics.TotalOperands);
                        node.SetInt(Halstead.ProgramVocabulary, halsteadMetrics.ProgramVocabulary);
                        node.SetInt(Halstead.ProgramLength, halsteadMetrics.ProgramLength);
                        node.SetFloat(Halstead.EstimatedProgramLength, halsteadMetrics.EstimatedProgramLength);
                        node.SetFloat(Halstead.Volume, halsteadMetrics.Volume);
                        node.SetFloat(Halstead.Difficulty, halsteadMetrics.Difficulty);
                        node.SetFloat(Halstead.Effort, halsteadMetrics.Effort);
                        node.SetFloat(Halstead.TimeRequiredToProgram, halsteadMetrics.TimeRequiredToProgram);
                        node.SetFloat(Halstead.NumberOfDeliveredBugs, halsteadMetrics.NumberOfDeliveredBugs);
                    }
                }
            }
        }

        /// <summary>
        /// Fills and adds all files and their metrics from <paramref name="fileToMetrics"/>
        /// to the passed graph <paramref name="graph"/>.
        /// </summary>
        /// <param name="fileToMetrics">The metrics to add.</param>
        /// <param name="graph">The initial graph where the files and metrics should be generated.</param>
        /// <param name="simplifyGraph">If the final graph should be simplified.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="repository"> The repository from which the nodes and metrics are derived.</param>
        private static void AddNodesAndMetrics
            (FileToMetrics fileToMetrics,
             Graph graph,
             bool simplifyGraph,
             string repositoryName,
             GitRepository repository)
        {
            CalculateTruckFactor(fileToMetrics);

            foreach (KeyValuePair<string, GitFileMetrics> file in fileToMetrics)
            {
                Node n = GraphUtils.GetOrAddFileNode(graph, file.Key);
                n.SetInt(DataModel.DG.VCS.NumberOfDevelopers, file.Value.Authors.Count);
                n.SetInt(DataModel.DG.VCS.NumberOfCommits, file.Value.NumberOfCommits);
                n.SetInt(DataModel.DG.VCS.LinesAdded, file.Value.LinesAdded);
                n.SetInt(DataModel.DG.VCS.LinesRemoved, file.Value.LinesRemoved);
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

            AddCodeMetrics(graph, repository);
            graph.AddSingleRoot(out Node _, repositoryName, DataModel.DG.VCS.RepositoryType);
            Simplify(graph, simplifyGraph);
        }

        /// <summary>
        /// If <paramref name="simplifyGraph"/> is true, the graph will be simplified by
        /// compressing single chains of directory nodes into the inner most directory node.
        /// </summary>
        /// <param name="graph">graph to be simplified</param>
        /// <param name="simplifyGraph">whether the graph should be simplified</param>
        private static void Simplify(Graph graph, bool simplifyGraph)
        {
            if (simplifyGraph)
            {
                foreach (Node child in graph.GetRoots()[0].Children().ToList())
                {
                    SimplifyGraph(child);
                }
            }
        }

        /// <summary>
        /// Simplifies a given graph by combining common directories (nodes of type
        /// <see cref="DataModel.DG.VCS.DirectoryType"/>).
        ///
        /// If a directory has only other directories as children, their paths will be combined.
        /// For instance the file structure:
        /// <code>
        /// root/
        ///?? dir1/
        ///?  ?? dir2/
        ///?  ?  ?? dir6/
        ///?  ?? dir3/
        ///?  ?  ?? file1.md
        ///?  ?  ?? dir5/
        ///  ?? dir4/
        /// </code>
        /// would become:
        /// <code>
        ///root/
        /// ?? dir1/dir2/dir6/
        /// ?? dir1/dir4/
        /// ?? dir1/dir3/
        /// ?  ?? file1.md
        /// ?  ?? dir5/
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
