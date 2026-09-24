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
    /// Tests for <see cref="ChurnGraphGenerator"/>. A run emits an account of
    /// the graph derived and a table of the files changed in the period, and
    /// holds both against what the run before produced, so that a change to the
    /// code deriving the graph does not pass unnoticed.
    /// </summary>
    /// <remarks>
    /// Everything reported here is read back out of the graph, which is thus
    /// also a check that what was gathered did reach it.
    ///
    /// Mind that the report depends on the history of the repository as much as
    /// on the code deriving it; see <see cref="Baseline"/> on telling the two
    /// apart.
    /// </remarks>
    internal class TestGitFileChurn
    {
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
        /// test runner is <c>SEE.VCS.Filter</c> over and over.
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

            Filter filter = new
                (// A file is reported on if it matches at least one inclusive
                 // pattern and no exclusive one. Add a pattern with the value
                 // false to leave something out, say { "**/*.Designer.cs", false }.
                 globbing: new Globbing() { { "**/*.cs", true } },
                 // The directories, relative to the root of the repository, whose
                 // files are reported on. Nested directories are included.
                 repositoryPaths: new string[] { "Assets/SEE", "Assets/SEETests" },
                 // Regular expressions selecting the branches to be reported on,
                 // their commits taken together. Each is matched against
                 // the friendly name of a branch as a whole, so "master" is the
                 // local master alone and "origin/.*" every remote-tracking
                 // branch of origin. To catch both masters, write ".*master".
                 //
                 // An expression selecting no branch at all is reported as a
                 // warning. A branch selected by several of them is still
                 // reported on only once.
                 //
                 // The commits reachable from the branches are walked once
                 // over, so selecting many branches costs far less than as many
                 // walks; what they share is visited a single time.
                 branches: new string[] { "master", "origin/.*" });

            GitRepository repositoryConfiguration = new(new DataPath(DataPath.ProjectFolder()), filter);

            yield return new TestCaseData(startDate, repositoryConfiguration)
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
                                    Context(repositoryConfiguration, since, tips), report);
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
        /// The circumstances the graph was derived under, to be recorded beside
        /// the report and not compared with it.
        /// </summary>
        /// <remarks>
        /// The tip of every branch walked is named, so that a report differing
        /// from its baseline because the repository has gained commits can be
        /// told from one differing because the code deriving the graph has
        /// changed.
        /// </remarks>
        /// <param name="repositoryConfiguration">The repository the graph was derived
        /// from.</param>
        /// <param name="since">The beginning of the period reported on.</param>
        /// <param name="tips">The SHA of the tip of each branch walked, keyed by its
        /// name.</param>
        /// <returns>The circumstances.</returns>
        private static string Context(GitRepository repositoryConfiguration, DateTimeOffset since,
                                      IDictionary<string, string> tips)
        {
            Filter filter = repositoryConfiguration.VCSFilter;
            StringBuilder result = new();
            result.AppendLine($"repository: {repositoryConfiguration.RepositoryPath.Path}");
            result.AppendLine($"since:      {since:o}");
            result.AppendLine("paths:      "
                              + string.Join(", ", filter.RepositoryPaths
                                                  ?? Array.Empty<string>()));
            result.AppendLine("branches:   "
                              + string.Join(", ", filter.Branches ?? Enumerable.Empty<string>()));
            foreach (KeyValuePair<string, string> tip in tips)
            {
                result.AppendLine($"tip {tip.Key} {tip.Value}");
            }
            return result.ToString();
        }
    }
}
