using System;
using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Tools.ReflexionAnalysis;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using static Assets.SEE.Tools.ReflexionAnalysis.CandidateRecommendation;
using System.Security.Cryptography;
using System.Diagnostics;

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

        /// <summary>
        /// 
        /// </summary>
        NodeReaderTest nodeReader;

        private readonly string[] alphabet = new string[]
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", 
            "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", 
            "w", "x", "y", "z"
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
        }

        private void SetupReflexion()
        {
            graph.Subscribe(this);
            // An initial run is necessary to set up the necessary data structures.
            graph.RunAnalysis();
            SetupCandidateRecommendation();
        }

        private void SetupCandidateRecommendation()
        {
            candidateRecommendation = new CandidateRecommendation();
            graph.Subscribe(candidateRecommendation);
        }

        private void SetupCountAttract()
        {
            MappingExperimentConfig config = new MappingExperimentConfig();
            CountAttractConfig attractConfig = new CountAttractConfig();
            attractConfig.CandidateType = "Candidate";
            attractConfig.ClusterType = "Cluster";
            config.AttractFunctionConfig = attractConfig;
            candidateRecommendation.UpdateConfiguration(graph, config);          
        }

        private void SetupADCAttract() 
        {
            MappingExperimentConfig config = GetADCAttractConfig(Document.DocumentMergingType.Intersection);
            candidateRecommendation.UpdateConfiguration(graph, config);
            nodeReader = new NodeReaderTest();
            ((ADCAttract)candidateRecommendation.AttractFunction).SetNodeReader(nodeReader);
        }

        private MappingExperimentConfig GetADCAttractConfig(Document.DocumentMergingType mergingType)
        {
            MappingExperimentConfig config = new MappingExperimentConfig();
            ADCAttractConfig attractConfig = new ADCAttractConfig(mergingType);
            attractConfig.CandidateType = "Candidate";
            attractConfig.ClusterType = "Cluster";
            config.AttractFunctionConfig = attractConfig;
            return config;
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
        /// i4(d) => i1(a)
        /// i4(d) => i2(b)
        /// i4(d) => i2(c)
        /// i5(e) => i4(d)
        /// i6(f) => i5(d)
        /// i6(f) => i3(c)
        /// i7(g) => i6(f)
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void AddImplementationCountAttract(string type = "Candidate")
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
        /// 
        /// Creates implementation nodes for ADC Test1.
        /// 
        /// created nodes:
        /// i1(a1)
        /// i2(a2)
        /// i3(b1)
        /// i4(b2)
        /// i5(c2)
        /// i6(c3)
        /// i7(d)
        /// 
        /// The dependencies are created as follows:
        /// i2(a2) => i1(a1)
        /// i4(b2) => i3(b1)
        /// i6(c2) => i5(c1)
        ///
        /// i1(a1) => i3(b1)
        /// i3(b1) => i5(c1)
        /// 
        /// i7(d) => i3(b1)
        /// i7(d) => i5(c1)
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void AddImplementationADC(string type = "Candidate")
        {
            i = new Dictionary<int, Node>();

            i[0] = NewNode(false, "implementation", "Package");

            for (int j = 1; j < 12; j++)
            {
                string character = alphabet[j - 1];
                i[j] = NewNode(false, character, type);
                i[0].AddChild(i[j]);
            }

            i[7].Type = "NoCandidate";
            // reparent h to g
            i[8].Reparent(i[7]);
            // reparent i to h
            i[9].Reparent(i[8]);

            (int, int)[] edgesFromTo =
            {
                (2,1), 
                (4,3),
                (6,5),
                (1,3),
                (3,5),
                (8,3),
                (8,5),

                (1,9),
                (9,5),

                (10,9),
                (11,5),
            };

            ie = CreateEdgesDictionary(edgesFromTo, i);
        }

        /// <summary>
        /// Creates an architecture as follows:
        ///
        /// A1
        /// A2
        /// A3
        /// 
        /// A1 calls A2
        /// A2 calls A3
        /// 
        /// </summary>
        /// <returns></returns>
        private void AddArchitectureADC()
        {
            a = new Dictionary<int, Node>();

            a[0] = NewNode(true, "architecture", "Architecture_Layer");

            for (int j = 1; j <= 4; j++)
            {
                a[j] = NewNode(true, "A" + j, "Cluster");
                a[0].AddChild(a[j]);
            }

            (int, int)[] edgesFromTo =
            {
                (1, 2), (2,3)
            };

            ae = CreateEdgesDictionary(edgesFromTo, a);
        }

        /// <summary>
        /// Creates an architecture as follows:
        ///
        /// A1
        /// A2
        /// 
        /// A1 calls A2
        /// 
        /// </summary>
        /// <returns></returns>
        private void AddArchitectureCountAttract()
        {
            a = new Dictionary<int, Node>();

            a[0] = NewNode(true, "architecture", "Architecture_Layer");

            for (int j = 1; j <= 3; j++)
            {
                a[j] = NewNode(true, "A" + j, "Cluster");
                a[0].AddChild(a[j]);
            }

            (int, int)[] edgesFromTo =
{
                (1, 2)
            };

            ae = CreateEdgesDictionary(edgesFromTo, a);
        }

        private void AddToMapping(Node from, Node to)
        {
            graph.StartCaching();
            graph.AddToMapping(from, to);
            graph.ReleaseCaching();
            candidateRecommendation.UpdateRecommendations();
        }

        private void RemoveFromMapping(Node node)
        {
            graph.StartCaching();
            graph.RemoveFromMapping(node);
            graph.ReleaseCaching();
            candidateRecommendation.UpdateRecommendations();
        }

        private void ResetMapping()
        {
            graph.ResetMapping();
            candidateRecommendation.UpdateRecommendations();
        }

        [Test]
        public void TestADCAttract()
        {
            this.AddArchitectureADC();
            this.AddImplementationADC();
            SetupADCAttract();

            this.nodeReader.SetLookUp(i[9].ID,"word2");
            this.nodeReader.SetLookUp(i[10].ID,"word2");
            this.nodeReader.SetLookUp(i[11].ID,"word2");

            // Initial Mapping

            // maps a(i1) and b(i2) to A1(a1)
            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[2], a[1]);

            // maps c(i3) and d(i4) to A2(a2)
            this.AddToMapping(i[3], a[2]);
            this.AddToMapping(i[4], a[2]);

            // maps e(i3) and f(i4) to A3(a2)
            this.AddToMapping(i[5], a[3]);
            this.AddToMapping(i[6], a[3]);

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[1]), 0.00000001);
            Assert.AreEqual(2, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[2]), 0.00000001);
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[3]), 0.00000001);

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[9], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[9], a[2]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[9], a[3]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[2]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[3]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[11], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[11], a[2]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[11], a[3]));

            this.AddToMapping(i[7], a[2]);

            Assert.AreEqual(0.0, candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[1]), 0.001);
            Assert.AreEqual(0.0, candidateRecommendation.AttractFunction.GetAttractionValue(i[11], a[3]), 0.001);

            UnityEngine.Debug.Log("Before Update");
            UnityEngine.Debug.Log(candidateRecommendation.AttractFunction.DumpTrainingData());

            this.candidateRecommendation.UpdateConfiguration(graph, GetADCAttractConfig(Document.DocumentMergingType.Union));
            ((ADCAttract)candidateRecommendation.AttractFunction).SetNodeReader(nodeReader);
            ((ADCAttract)candidateRecommendation.AttractFunction).ClearDocumentCache();
            ((ADCAttract)candidateRecommendation.AttractFunction).Reset();
            this.graph.RunAnalysis();

            UnityEngine.Debug.Log("Before Calculating for j and k");
            UnityEngine.Debug.Log(candidateRecommendation.AttractFunction.DumpTrainingData());

            Assert.AreEqual(0.7071, candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[1]), 0.001);
            Assert.AreEqual(0.7071, candidateRecommendation.AttractFunction.GetAttractionValue(i[11], a[3]), 0.001);

            UnityEngine.Debug.Log("Before Resetting");
            UnityEngine.Debug.Log(candidateRecommendation.AttractFunction.DumpTrainingData());

            // second run of test 
            graph.ResetMapping();

            UnityEngine.Debug.Log("After Resetting");
            UnityEngine.Debug.Log(candidateRecommendation.AttractFunction.DumpTrainingData());

            // maps a(i1) and b(i2) to A1(a1)
            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[2], a[1]);

            // maps c(i3) and d(i4) to A2(a2)
            this.AddToMapping(i[3], a[2]);
            this.AddToMapping(i[4], a[2]);

            // maps e(i3) and f(i4) to A3(a2)
            this.AddToMapping(i[5], a[3]);
            this.AddToMapping(i[6], a[3]);

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[1]), 0.00000001);
            Assert.AreEqual(2, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[2]), 0.00000001);
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[3]), 0.00000001);

            graph.ResetMapping();

            UnityEngine.Debug.Log(candidateRecommendation.AttractFunction.DumpTrainingData());
        }

        [Test]
        public void TestCountAttract()
        {
            this.AddArchitectureCountAttract();
            this.AddImplementationCountAttract();
            SetupCountAttract();

            // Initial Mapping
            // maps i1(a) to a1(A1)
            this.AddToMapping(i[1], a[1]);

            // maps i2(b) to a2(A2)
            this.AddToMapping(i[2], a[2]);

            // check if i4(d) is in the Recommendations for both cluster
            Assert.That(DIsRecommendedForA1AndA2());

            // maps i3(c) to a1(A1)
            this.AddToMapping(i[3], a[1]);

            // check if i4(d) is in the Recommendations for a1(H1) but not a2(H2)
            Assert.That(DIsRecommendedForA1ButNotA2());

            // remove i3(c) from mapping a1(A1)
            this.RemoveFromMapping(i[3]);

            // check of i4(d) is in the Recommendations for both
            Assert.That(DIsRecommendedForA1AndA2());

            // maps i3(c) to a2(A2)
            this.AddToMapping(i[3], a[2]);

            // check if i4(d) is in the Recommendations for a2(A2) but not a1(A1)
            Assert.That(DIsRecommendedForA2ButNotA1());

            /**
             * a mapsto A1
             * b mapsto A2
             * c mapsto A2
             * 
             * Overall(d) = 3 
             * toOthers(d, A1) = 2 * Phi
             * toOthers(d, A2) = 1
             *
             * CountAttract(d,A1) = 3 - 2 * phi
             * CountAttract(d,A2) = 3 - 1
             *
             * phi <  0.5 => CountAttract(d,{A1,A2}) = {A1} 
             * phi == 0.5 => CountAttract(d,{A1,A2}) = {A1,A2}
             * phi >  0.5 => CountAttract(d,{A1,A2}) = {A2}
            */

            // update phi value
            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.4f;

            // remaps i3(c) to a2(A2)
            this.RemoveFromMapping(i[3]);
            this.AddToMapping(i[3], a[2]);

            // check if i4(d) is in the Recommendations for a1(A1) but not a2(A2)
            Assert.That(DIsRecommendedForA1ButNotA2());

            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.5f;

            // remaps i3(c) to a2(A2)
            this.RemoveFromMapping(i[3]);
            this.AddToMapping(i[3], a[2]);

            Assert.That(DIsRecommendedForA1AndA2());

            ((CountAttract)candidateRecommendation.AttractFunction).Phi = 0.6f;

            // remaps i3(c) to a2(A2)
            this.RemoveFromMapping(i[3]);
            this.AddToMapping(i[3], a[2]);

            Assert.That(DIsRecommendedForA2ButNotA1());

            this.ResetMapping();

            Assert.That(candidateRecommendation.Recommendations.Keys.Count == 0 &&
                        candidateRecommendation.Recommendations.Values.Count == 0);

            Assert.That(candidateRecommendation.AttractFunction.EmptyTrainingData());

            #region local functions
            bool DIsRecommendedForA1AndA2()
            {
                return CheckRecommendations(candidateRecommendation.Recommendations,
                                    new Dictionary<Node, HashSet<MappingPair>>()
                                    {
                                        {a[1], new HashSet<MappingPair>() { new MappingPair(i[4], a[1], -1.0) } },
                                        {a[2], new HashSet<MappingPair>() { new MappingPair(i[4], a[2], -1.0) } }
                                    },
                                    null);
            }

            bool DIsRecommendedForA1ButNotA2()
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

            bool DIsRecommendedForA2ButNotA1()
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