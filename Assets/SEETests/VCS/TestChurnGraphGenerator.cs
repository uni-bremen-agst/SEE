using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="ChurnGraphGenerator"/>, one per way of asking it
    /// which commits to count: <see cref="TestChurn"/> for a period over a set
    /// of branches, <see cref="TestCommitRange"/> for a range between two
    /// commits. A run emits an account of the graph derived and a table of the
    /// files changed, and holds both against what the run before produced, so
    /// that a change to the code deriving the graph does not pass unnoticed.
    /// </summary>
    /// <remarks>
    /// Everything reported is read back out of the graph by
    /// <see cref="ChurnReport"/>, which is thus also a check that what was
    /// gathered did reach it, and which is what lets the two accounts be held
    /// against one another.
    ///
    /// The two differ in how much a difference from the baseline is worth.
    /// <see cref="TestCommitRange"/> names its commits by their SHA and they
    /// cannot move, so the graph is a function of the code alone and any
    /// difference at all is the code having changed its answer.
    /// <see cref="TestChurn"/> reports on branches, whose tips advance, so a
    /// difference there may be one of history; see <see cref="Baseline"/> on
    /// telling the two apart.
    /// </remarks>
    internal class TestChurnGraphGenerator
    {
        /// <summary>
        /// The commit <see cref="TestCommitRange"/> takes its files from and
        /// counts the changes up to. The tip of master when that test was
        /// written.
        /// </summary>
        private const string commitID = "c57e48f86ec3729dbe8f1c106cfe437e079e30c0";

        /// <summary>
        /// The commit <see cref="TestCommitRange"/> counts the changes from,
        /// excluded itself. An ancestor of <see cref="commitID"/> a few hundred
        /// commits back: far enough to take in a fair spread of the history,
        /// near enough that the run stays short.
        /// </summary>
        private const string baselineCommitID = "e5fb7dfc727ed0d92fac7b52ddb11cb67cd1ef59";

        /// <summary>
        /// The configurations <see cref="TestChurn"/> is run on, one
        /// test case each.
        /// </summary>
        /// <remarks>
        /// A source rather than a <see cref="TestCaseAttribute"/>, because the
        /// argument of an attribute must be a constant expression, and
        /// <c>new DateTime(...)</c> is not one: an object creation never is. The
        /// arrays would have been allowed, an array of constants being a
        /// constant expression, but the date alone rules the attribute out.
        ///
        /// Being a method, this is moreover evaluated when the test is
        /// discovered rather than when this class is initialized, so it may
        /// read the fields above whatever order those are declared in.
        ///
        /// Name every case: with none, all that tells two of them apart in the
        /// test runner is <c>SEE.VCS.Filter</c> over and over. The name is also
        /// what the baseline is filed under, so changing it starts afresh.
        /// </remarks>
        /// <returns>One test case per configuration.</returns>
        private static IEnumerable<TestCaseData> Configurations()
        {
            // The beginning of the period to be reported on; commits authored at
            // the instant this day begins at in UTC are still taken into account,
            // that being the instant a date denotes elsewhere in SEE.
            //
            // The further back this reaches, the longer the test runs.
            DateTime startDate = new(2025, 1, 1);

            // Regular expressions selecting the branches to be reported on,
            // their commits taken together. Each is matched against the friendly
            // name of a branch as a whole, so "master" is the local master alone
            // and "origin/.*" every remote-tracking branch of origin. To catch
            // both masters, write ".*master".
            //
            // An expression selecting no branch at all is reported as a warning.
            // A branch selected by several of them is still reported on only
            // once.
            //
            // The commits reachable from the branches are walked once over, so
            // selecting many branches costs far less than as many walks; what
            // they share is visited a single time.
            //
            // Mind that this takes in the branch this work is done on, so every
            // push moves the baseline. That is history rather than a defect, and
            // Baseline says which it is.
            yield return new TestCaseData(startDate, Configuration("master", "origin/.*"))
                .SetName(nameof(TestChurn));
        }

        /// <summary>
        /// Derives the graph of the repository named by
        /// <paramref name="repositoryConfiguration"/>, emits an account of it
        /// and a table of the files changed since <paramref name="startDate"/>
        /// to the console, and holds both against the baseline of the previous
        /// run. Also reports the time the deriving took, so that this may be
        /// run under a profiler.
        /// </summary>
        /// <remarks>
        /// The <see cref="DeepProfiler"/> scope below yields a dotTrace snapshot
        /// covering only the generation, with every method transitively called in
        /// it. How to set dotTrace up for this is documented at
        /// <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>; the
        /// essential part is that "Control profiling" must be set to "Using API",
        /// because otherwise the calls made here are inert.
        ///
        /// The profiling type has to be Timeline. dotTrace offers no other type for
        /// a Unity target: "Only the Timeline type is supported for profiling a
        /// Mono/Unity application." A Timeline snapshot can only be read by
        /// dotTrace's Timeline viewer; it is not a .dtp snapshot and the
        /// command-line Reporter cannot turn it into a report.
        ///
        /// Unlike <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>,
        /// the work here runs synchronously on the thread of this test method, so
        /// its call tree is the one to look at in the viewer.
        /// </remarks>
        /// <param name="startDate">The beginning of the period to be reported on.</param>
        /// <param name="repositoryConfiguration">The repository configuration based on which the
        /// graph is derived.</param>
        [TestCaseSource(nameof(Configurations))]
        public void TestChurn(DateTime startDate, GitRepository repositoryConfiguration)
        {
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);
            Graph graph = new(repositoryPath, repositoryName);

            Performance p = Performance.Begin($"Adding nodes for {repositoryName} commits since "
                                              + $"{startDate:yyyy-MM-dd}");
            // Enable the following line to get a dotTrace snapshot of the generation, with every
            // method transitively called in it.
            //using (DeepProfiler.Capture(nameof(TestChurn)))
            {
                ChurnGraphGenerator.AddNodesAfterDate
                    (graph: graph,
                     simplifyGraph: true,
                     repositoryConfiguration: repositoryConfiguration,
                     repositoryName: repositoryName,
                     startDate: startDate,
                     computeCoFileChanges: true,
                     changePercentage: null,
                     token: default);
            }
            p.End(true);

            ChurnReport.Check(graph);
            // The instant the date denotes, as ChurnGraphGenerator reads it.
            DateTimeOffset since = new(startDate.Date, TimeSpan.Zero);
            IDictionary<string, string> tips = Tips(repositoryConfiguration);
            // The separators of the date are quoted, because an unquoted '-'
            // and ':' stand for the date and the time separator of the current
            // culture.
            string report
                = ChurnReport.Of(graph, $"{tips.Count} branches, author date >= "
                                        + since.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz"));
            Debug.Log(report);
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name,
                                    Context(repositoryConfiguration, Period(since, tips)), report);
        }

        /// <summary>
        /// Derives the graph of the commits between <see cref="baselineCommitID"/>
        /// and <see cref="commitID"/>, emits an account of it and a table of the
        /// files changed to the console, and holds both against the baseline of
        /// the previous run.
        /// </summary>
        [Test]
        public void TestCommitRange()
        {
            // No branch is named: which commits are walked is settled by the two
            // SHAs. The globbing and the directories are the ones TestChurn uses,
            // so that the two accounts may be held against one another.
            GitRepository repositoryConfiguration = Configuration();
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);
            Graph graph = new(repositoryPath, repositoryName);

            Performance p = Performance.Begin($"Adding nodes for {repositoryName} commits "
                                              + $"{Short(baselineCommitID)}..{Short(commitID)}");
            ChurnGraphGenerator.AddNodesForCommit
                (graph: graph,
                 simplifyGraph: true,
                 repositoryConfiguration: repositoryConfiguration,
                 repositoryName: repositoryName,
                 commitID: commitID,
                 baselineCommitID: baselineCommitID,
                 computeCoFileChanges: true,
                 changePercentage: null,
                 token: default);
            p.End(true);

            ChurnReport.Check(graph);
            string report = ChurnReport.Of(graph, $"commits {Short(baselineCommitID)}.."
                                                  + Short(commitID));
            Debug.Log(report);
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name,
                                    Context(repositoryConfiguration, Range()), report);
        }

        /// <summary>
        /// The repository the graph is derived from, along with the filter
        /// stating which of its files, and where named which of its branches,
        /// are taken into account.
        /// </summary>
        /// <param name="branches">Regular expressions selecting the branches to be walked.
        /// None where the commits are settled otherwise.</param>
        /// <returns>The repository configuration.</returns>
        private static GitRepository Configuration(params string[] branches)
        {
            Filter filter = new
                (// A file is reported on if it matches at least one inclusive
                 // pattern and no exclusive one. Add a pattern with the value
                 // false to leave something out, say { "**/*.Designer.cs", false }.
                 globbing: new Globbing() { { "**/*.cs", true } },
                 // The directories, relative to the root of the repository, whose
                 // files are reported on. Nested directories are included.
                 repositoryPaths: new string[] { "Assets/SEE", "Assets/SEETests" },
                 // Null rather than an empty set, which would select no branch
                 // at all where the filter is consulted for one.
                 branches: branches.Length == 0 ? null : branches);
            return new GitRepository(new DataPath(DataPath.ProjectFolder()), filter);
        }

        /// <summary>
        /// The commit at the tip of each branch <see cref="ChurnGraphGenerator"/>
        /// walks, which is every one the filter of
        /// <paramref name="repositoryConfiguration"/> holds relevant but the
        /// symbolic references among them, keyed by the name of the branch and
        /// ordered by it.
        /// </summary>
        /// <remarks>
        /// The SHAs are read here, while the session is open, and only they are
        /// handed out. A <see cref="Branch"/> is a handle into the native
        /// repository the session holds; one outliving its session refers to
        /// memory that has been freed, and asking such a one for its
        /// <see cref="Branch.Tip"/> brings the editor down rather than raising
        /// anything that could be caught.
        /// </remarks>
        /// <param name="repositoryConfiguration">The repository whose branches are asked
        /// for.</param>
        /// <returns>The SHA of the tip of each branch walked, keyed by its name.</returns>
        private static IDictionary<string, string> Tips(GitRepository repositoryConfiguration)
        {
            SortedDictionary<string, string> result = new(StringComparer.Ordinal);
            using GitRepositorySession session = repositoryConfiguration.OpenGitSession();
            foreach (Branch branch in session.RelevantBranches()
                                             .Where(branch => !branch.FriendlyName
                                                     .EndsWith("/HEAD", StringComparison.Ordinal)))
            {
                result[branch.FriendlyName] = branch.Tip.Sha;
            }
            return result;
        }

        /// <summary>
        /// The first seven characters of <paramref name="sha"/>, as git abbreviates
        /// one.
        /// </summary>
        /// <param name="sha">The SHA to be abbreviated.</param>
        /// <returns>The abbreviation.</returns>
        private static string Short(string sha)
        {
            return sha.Substring(0, 7);
        }

        /// <summary>
        /// What is recorded of the commits a period took in: from when, and the
        /// tip of every branch walked.
        /// </summary>
        /// <remarks>
        /// The tips are named so that a report differing from its baseline
        /// because the repository has gained commits can be told from one
        /// differing because the code deriving the graph has changed.
        /// </remarks>
        /// <param name="since">The beginning of the period reported on.</param>
        /// <param name="tips">The SHA of the tip of each branch walked, keyed by its
        /// name.</param>
        /// <returns>The lines to record.</returns>
        private static IEnumerable<string> Period(DateTimeOffset since,
                                                  IDictionary<string, string> tips)
        {
            yield return $"since:      {since:o}";
            foreach (KeyValuePair<string, string> tip in tips)
            {
                yield return $"tip {tip.Key} {tip.Value}";
            }
        }

        /// <summary>
        /// What is recorded of the commits a range took in, which is the two
        /// naming it.
        /// </summary>
        /// <remarks>
        /// No tip is named, unlike for a period: the commits reported on are
        /// named outright and cannot move, so a difference from the baseline is
        /// never one of history.
        /// </remarks>
        /// <returns>The lines to record.</returns>
        private static IEnumerable<string> Range()
        {
            yield return $"commit:     {commitID}";
            yield return $"baseline:   {baselineCommitID}";
        }

        /// <summary>
        /// The circumstances the graph was derived under, to be recorded beside
        /// the report and not compared with it.
        /// </summary>
        /// <param name="repositoryConfiguration">The repository the graph was derived
        /// from.</param>
        /// <param name="commits">What is recorded of the commits taken in, which differs
        /// between a period and a range.</param>
        /// <returns>The circumstances.</returns>
        private static string Context(GitRepository repositoryConfiguration,
                                      IEnumerable<string> commits)
        {
            Filter filter = repositoryConfiguration.VCSFilter;
            StringBuilder result = new();
            result.AppendLine($"repository: {repositoryConfiguration.RepositoryPath.Path}");
            foreach (string line in commits)
            {
                result.AppendLine(line);
            }
            result.AppendLine("paths:      "
                              + string.Join(", ", filter.RepositoryPaths
                                                  ?? Array.Empty<string>()));
            if (filter.Branches != null)
            {
                result.AppendLine("branches:   " + string.Join(", ", filter.Branches));
            }
            return result.ToString();
        }
    }
}
