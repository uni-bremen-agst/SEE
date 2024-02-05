using System;
using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Tools.ReflexionAnalysis;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using static Assets.SEE.Tools.ReflexionAnalysis.CandidateRecommendation;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Tests for the candidate recommendation within a reflexion graph.
    ///
    ///
    ///
    /// </summary>
    internal class TestCandidateRecommendation : TestReflexionAnalysis
    {
        /// <summary>
        ///
        /// </summary>
        private Dictionary<int, Node> i;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<(int, int), Edge> ie;

        /// <summary>
        ///
        /// </summary>
        private Dictionary<int, Node> a;

        /// <summary>
        ///
        /// </summary>
        private Dictionary<(int, int), Edge> ae;

        /// <summary>
        /// 
        /// </summary>
        CandidateRecommendation candidateRecommendation;

        private readonly string[] alphabet = new string[]
        {
            "A", "B", "C", "D", "E", "F", "G", "H"
        };

        /// <summary>
        /// Sets up all three graphs (implementation, architecture,
        /// mapping) and registers itself at the reflexion analysis
        /// to obtain the results via the callback Update(ChangeEvent).
        /// Does not really run the analysis, however.
        /// </summary>
        [SetUp]
        protected override void Setup()
        {
            base.Setup();
            SetupReflexion();
            SetupCandidateRecommendation();
        }

        private void SetupReflexion()
        {
            graph.Subscribe(this);
            // An initial run is necessary to set up the necessary data structures.
            graph.RunAnalysis();
        }

        private void SetupCandidateRecommendation()
        {
            candidateRecommendation = new CandidateRecommendation();
            MappingExperimentConfig config = new MappingExperimentConfig();
            CountAttractConfig attractConfig = new CountAttractConfig();
            attractConfig.CandidateType = "Class";
            attractConfig.ClusterType = "Cluster";
            config.AttractFunctionConfig = attractConfig;
            candidateRecommendation.UpdateConfiguration(graph, config);
            graph.Subscribe(candidateRecommendation);
        }

        [TearDown]
        protected override void Teardown()
        {
            base.Teardown();
            i = null;
            a = null;
            candidateRecommendation = null;
        }

        /// <summary>
        /// 
        /// Creates implementation nodes for CountAttract Test1.
        /// 
        /// The dependencies are created as follows:
        /// 
        /// Dependency Edges:
        /// i4(D) => i1(A)
        /// i4(D) => i2(B)
        /// i4(D) => i2(C)
        /// i5(E) => i4(D)
        /// i6(F) => i5(D)
        /// i6(F) => i3(C)
        /// i7(G) => i6(F)
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void AddImplementationCountAttract(string type = "Class")
        {
            i = new Dictionary<int, Node>();

            i[0] = NewNode(false, "implementation", "Package");

            for (int j = 1; j < 8; j++)
            {
                string character = alphabet[j - 1];
                i[j] = NewNode(false, character, type);
                i[0].AddChild(i[j]);
            }

            (int, int)[] edgesFromTo =
            {
                (4, 1), (4, 2), (4, 3), (5, 4), (6, 5), (6, 3), (7, 6)
            };

            ie = CreateEdgesDictionary(edgesFromTo, i);
        }

        /// <summary>
        /// Creates an architecture as follows:
        ///
        /// a1
        /// a2
        /// 
        /// a1 calls a2
        /// 
        /// </summary>
        /// <returns></returns>
        private void AddArchitectureCountAttract()
        {
            a = new Dictionary<int, Node>();

            a[0] = NewNode(true, "architecture", "Architecture_Layer");

            for (int j = 1; j <= 3; j++)
            {
                a[j] = NewNode(true, "a" + j, "Cluster");
                a[0].AddChild(a[j]);
            }

            (int, int)[] edgesFromTo =
{
                (1, 2)
            };

            ae = CreateEdgesDictionary(edgesFromTo, a);
        }

        [Test]
        public void TestCountAttract()
        {
            this.AddArchitectureCountAttract();
            this.AddImplementationCountAttract();

            // Initial Mapping
            // maps i1(A) to a1(H1)
            graph.AddToMapping(i[1], a[1]);

            // maps i2(B) to a2(H2)
            graph.AddToMapping(i[2], a[2]);

            // check if i4(D) is in the Recommendations for both cluster
            Assert.That(DIsRecommendedForH1AndH2());

            // maps i3(C) to a1(H1)
            graph.AddToMapping(i[3], a[1]);

            // check if i4(D) is in the Recommendations for a1(H1) but not a2(H2)
            Assert.That(DIsRecommendedForH1ButNotH2());

            // remove i3(C) from mapping a1(H1)
            graph.RemoveFromMapping(i[3]);

            // check of i4(D) is in the Recommendations for both
            Assert.That(DIsRecommendedForH1AndH2());

            // maps i3(C) to a2(H2)
            graph.AddToMapping(i[3], a[2]);

            // check if i4(D) is in the Recommendations for a2(H2) but not a1(H1)
            Assert.That(DIsRecommendedForH2ButNotH1());

            /**
             * A mapsto H1
             * B mapsto H2
             * C mapsto H2
             * 
             * Overall(D) = 3 
             * toOthers(D, H1) = 2 * Phi
             * toOthers(D, H2) = 1
             *
             * CountAttract(D,H1) = 3 - 2 * phi
             * CountAttract(D,H2) = 3 - 1
             *
             * phi <  0.5 => CountAttract(D,{H1,H2}) = {H1} 
             * phi == 0.5 => CountAttract(D,{H1,H2}) = {H1,H2}
             * phi >  0.5 => CountAttract(D,{H1,H2}) = {H2}
            */

            // update phi value
            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.4f;

            // remaps i3(C) to a2(H2)
            graph.RemoveFromMapping(i[3]);
            graph.AddToMapping(i[3], a[2]);

            // check if i4(D) is in the Recommendations for a1(H1) but not a2(H2)
            Assert.That(DIsRecommendedForH1ButNotH2());

            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.5f;

            // remaps i3(C) to a2(H2)
            graph.RemoveFromMapping(i[3]);
            graph.AddToMapping(i[3], a[2]);

            Assert.That(DIsRecommendedForH1AndH2());

            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.6f;

            // remaps i3(C) to a2(H2)
            graph.RemoveFromMapping(i[3]);
            graph.AddToMapping(i[3], a[2]);

            Assert.That(DIsRecommendedForH2ButNotH1());

            //// remove all mappings
            for (int j = 1; j < i.Count; ++j)
            {
                graph.RemoveFromMapping(i[j], ignoreUnmapped: true);
            }

            Assert.That(candidateRecommendation.Recommendations.Keys.Count == 0 &&
                        candidateRecommendation.Recommendations.Values.Count == 0);

            #region local functions
            bool DIsRecommendedForH1AndH2()
            {
                return CheckRecommendations(candidateRecommendation.Recommendations,
                                    new Dictionary<Node, HashSet<MappingPair>>()
                                    {
                                        {a[1], new HashSet<MappingPair>() { new MappingPair(i[4], a[1], -1.0) } },
                                        {a[2], new HashSet<MappingPair>() { new MappingPair(i[4], a[2], -1.0) } }
                                    },
                                    null);
            }

            bool DIsRecommendedForH1ButNotH2()
            {
                return CheckRecommendations(candidateRecommendation.Recommendations,
                                    new Dictionary<Node, HashSet<MappingPair>>()
                                    {
                                        {a[1], new HashSet<MappingPair>() { new MappingPair(i[4], a[1], -1.0) } },
                                    },
                                    new Dictionary<Node, HashSet<MappingPair>>()
                                    {
                                        {a[2], new HashSet<MappingPair>() { new MappingPair(i[4], a[2], -1.0) } },
                                    });
            }

            bool DIsRecommendedForH2ButNotH1()
            {
                return CheckRecommendations(candidateRecommendation.Recommendations,
                    new Dictionary<Node, HashSet<MappingPair>>()
                    {
                                        {a[2], new HashSet<MappingPair>() { new MappingPair(i[4], a[2], -1.0) } },
                    },
                    new Dictionary<Node, HashSet<MappingPair>>()
                    {
                                        {a[1], new HashSet<MappingPair>() { new MappingPair(i[4], a[1], -1.0) } },
                    });
            } 
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recommendations">Actual recommendations which are compared to the expected recommendations.</param>
        /// <param name="mustBeRecommended">Required recommendations. If null this parameter is ignored.</param>
        /// <param name="mustNotBeRecommended">Forbidden recommendation. If null this parameter is ignored.
        /// If this parameter is not null it must not contain null values.</param>
        /// <returns></returns>
        public bool CheckRecommendations(Dictionary<Node, HashSet<MappingPair>> recommendations, 
                                         Dictionary<Node, HashSet<MappingPair>> mustBeRecommended,
                                         Dictionary<Node, HashSet<MappingPair>> mustNotBeRecommended)
        {
            // Check mustRecommended
            if (mustBeRecommended != null)
            {
                foreach (Node cluster in mustBeRecommended.Keys)
                {
                    HashSet<MappingPair> recommendationsForCluster;
                    if (!recommendations.TryGetValue(cluster, out recommendationsForCluster)) return false;

                    if (recommendationsForCluster == null && mustBeRecommended[cluster] == null) continue;
                    if (recommendationsForCluster == null || mustBeRecommended == null) return false;
                    // Cannot compare MappingPairs directly, because the attraction values might differ
                    if (!mustBeRecommended[cluster].Select(p => p.CandidateID).ToHashSet()
                        .IsSubsetOf(recommendationsForCluster.Select(p => p.CandidateID).ToHashSet())) return false;
                } 
            }

            // Check mustNotRecommended 
            if (mustNotBeRecommended != null)
            {
                foreach (Node cluster in mustNotBeRecommended.Keys)
                {
                    HashSet<MappingPair> recommendationsForCluster;
                    if (!recommendations.TryGetValue(cluster, out recommendationsForCluster)) continue;
                    if (mustNotBeRecommended[cluster] == null) throw new Exception("Error in while comparing Recommendations. Forbidden recommendations cannot be null.");
                    // Cannot compare MappingPairs directly, because the attraction values might differ
                    if (recommendationsForCluster.Select(p => p.CandidateID).ToHashSet()
                        .Intersect(mustNotBeRecommended[cluster].Select(p => p.CandidateID).ToHashSet()).Count() > 0) return false;
                }
            }

            return true;
        }
    }
}