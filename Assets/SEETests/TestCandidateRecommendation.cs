using System;
using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Tools.ReflexionAnalysis;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.Tools.ReflexionAnalysis;

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
        Recommendations candidateRecommendation;

        /// <summary>
        /// 
        /// </summary>
        TestNodeReader nodeReader;

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
            candidateRecommendation = new Recommendations();
        }

        private void SetupCountAttract(float phi = 1.0f, Dictionary<string, double> edgeWeights = null)
        {
            RecommendationSettings config = new RecommendationSettings();
            CountAttractConfig attractConfig = new CountAttractConfig();
            attractConfig.Phi = phi;
            config.CandidateType = "Candidate";
            config.ClusterType = "Cluster";
            attractConfig.EdgeWeights = edgeWeights != null ? edgeWeights : attractConfig.EdgeWeights;
            config.CountAttractConfig = attractConfig;
            config.AttractFunctionType = AttractFunction.AttractFunctionType.CountAttract;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
        }

        private void SetupNBAttract(INodeReader nodeReader, bool useCda = false)
        {
            RecommendationSettings config = new RecommendationSettings();
            NBAttractConfig attractConfig = new NBAttractConfig();
            config.NodeReader = nodeReader;
            config.CandidateType = "Candidate";
            config.ClusterType = "Cluster";
            attractConfig.AlphaSmoothing = 1.0;
            config.NBAttractConfig = attractConfig;
            config.AttractFunctionType = AttractFunction.AttractFunctionType.NBAttract;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
        }

        private void SetupADCAttract(INodeReader nodeReader, Document.DocumentMergingType mergingType)
        {
            RecommendationSettings config = GetADCAttractConfig(mergingType);
            config.NodeReader = nodeReader;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
        }

        private RecommendationSettings GetADCAttractConfig(Document.DocumentMergingType mergingType)
        {
            RecommendationSettings config = new RecommendationSettings();
            ADCAttractConfig attractConfig = new ADCAttractConfig(mergingType);
            config.CandidateType = "Candidate";
            config.ClusterType = "Cluster";
            config.ADCAttractConfig = attractConfig;
            config.AttractFunctionType = AttractFunction.AttractFunctionType.ADCAttract;
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

        public void AddImplementation(int numberNodes, (int, int)[] edgesFromTo, string type = "Candidate")
        {
            i = new Dictionary<int, Node>();

            i[0] = NewNode(false, "implementation", "Package");

            for (int j = 1; j <= numberNodes; j++)
            {
                i[j] = NewNode(false, j.ToString(), type);
                i[0].AddChild(i[j]);
            }

            ie = CreateEdgesDictionary(edgesFromTo, i);
        }

        public void AddArchitecture(int numberNodes, (int, int)[] edgesFromTo)
        {
            a = new Dictionary<int, Node>();

            a[0] = NewNode(true, "architecture", "Architecture_Layer");

            for (int j = 1; j <= numberNodes; j++)
            {
                a[j] = NewNode(true, "A" + j, "Cluster");
                a[0].AddChild(a[j]);
            }

            ae = CreateEdgesDictionary(edgesFromTo, a);
        }

        public void AddHierarchy((int, int)[] edgesFromTo, ReflexionSubgraphs subgraphType)
        {
            if (subgraphType == ReflexionSubgraphs.None
                || subgraphType == ReflexionSubgraphs.Mapping
                || subgraphType == ReflexionSubgraphs.FullReflexion)
            {
                throw new Exception("Unexpected reflexion subgraph.");
            }

            Dictionary<int, Node> subgraph = subgraphType == ReflexionSubgraphs.Implementation ? i : a;

            foreach ((int, int) edge in edgesFromTo)
            {
                subgraph[edge.Item1].Reparent(subgraph[edge.Item2]);
            }
        }

        public void OverrideDefaultEdgeType((int, int)[] edgesFromTo, ReflexionSubgraphs subgraphType, string edgeType)
        {
            if (subgraphType == ReflexionSubgraphs.None
                || subgraphType == ReflexionSubgraphs.Mapping
                || subgraphType == ReflexionSubgraphs.FullReflexion)
            {
                throw new Exception("Unexpected reflexion subgraph.");
            }

            Dictionary<(int,int), Edge> edges = subgraphType == ReflexionSubgraphs.Implementation ? ie : ae;

            foreach ((int,int) edge in edgesFromTo)
            {
                edges[edge].Type = edgeType;
            }
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

        private void RemoveEdge(Edge edge)
        {
            graph.StartCaching();
            graph.RemoveEdge(edge);
            graph.ReleaseCaching();
            candidateRecommendation.UpdateRecommendations();
        }

        private void AddEdge(Edge edge)
        {
            graph.StartCaching();
            graph.AddEdge(edge);
            graph.ReleaseCaching();
            candidateRecommendation.UpdateRecommendations();
        }

        private void ResetMapping()
        {
            graph.ResetMapping();
            candidateRecommendation.UpdateRecommendations();
        }

        [Test]
        public void TestNBAtrract()
        {
            (int, int)[] edgesFromTo = { };

            this.AddArchitecture(numberNodes: 3, edgesFromTo);
            this.AddImplementation(numberNodes: 5, edgesFromTo);

            this.nodeReader = new TestNodeReader();
            this.nodeReader.SetLookUp(i[1].ID, "word1");
            this.nodeReader.SetLookUp(i[2].ID, "word1");
            this.nodeReader.SetLookUp(i[3].ID, "word1");
            this.nodeReader.SetLookUp(i[4].ID, "word1 word2");
            this.nodeReader.SetLookUp(i[5].ID, "word2");

            SetupNBAttract(nodeReader);

            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[2], a[2]);
            this.AddToMapping(i[3], a[3]);

            double attract4To1 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));
            double attract4To2 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[2]));
            double attract4To3 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[3]));

            double attract5To2 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            double attract5To3 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));
            double attract5To1 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));

            Assert.AreEqual(attract4To1, attract4To2, 0.000001);
            Assert.AreEqual(attract4To1, attract4To3, 0.000001);
            Assert.AreEqual(attract4To2, attract4To3, 0.000001);

            Assert.IsTrue(attract4To1 > 0);
            Assert.IsTrue(attract4To2 > 0);
            Assert.IsTrue(attract4To3 > 0);

            /**
             * 
             * The calculations assume alpha smoothing with alpha = 1 within the data set
             * One case for all classes named A, because all classes are currently containing the same data.
             * 
             * Histogram of class A
             * w1 = 1 + a = 2
             * w2 = 0 + a = 1
             * implementation = 0 + a = 1
             * 
             * P(a1) = 1/3
             * P(a2) = 1/3
             * P(a3) = 1/3
             * 
             * doc(i4) = {w1, w2, implementation} (the word 'implementation' is add, because of the hierarchical ascendants of the node)
             * doc(i5) = {w2, implementation}
             * 
             * P(A | w1 w2) proportional to P(A) * P(w1 | A) * P(w2 | A) * P(implementation | A) =
             *                              1/3 * 1/4 * 2/4 * 1/4 = 0.0104166   
             * 
             * P(A | w1) proportional to P(A) * P(w2 | A) * P(implementation | A) =
             *                              1/3 * 1/4 * 1/4 = 0.0208333
             * 
             * */

            Assert.AreEqual(attract5To1, 0.0208333, 0.00001);
            Assert.AreEqual(attract5To2, 0.0208333, 0.00001);
            Assert.AreEqual(attract5To3, 0.0208333, 0.00001);

            Assert.AreEqual(attract4To1, 0.0104166, 0.00001);
            Assert.AreEqual(attract4To2, 0.0104166, 0.00001);
            Assert.AreEqual(attract4To3, 0.0104166, 0.00001);

            this.AddToMapping(i[4], a[1]);

            attract5To2 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            attract5To3 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));
            attract5To1 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));

            Assert.IsTrue(attract5To1 > attract5To2);
            Assert.IsTrue(attract5To1 > attract5To3);
            Assert.AreEqual(attract5To3, attract5To2, 0.0001);

            this.RemoveFromMapping(i[4]);
            this.AddToMapping(i[4], a[2]);

            attract5To2 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            attract5To3 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));
            attract5To1 = NaiveBayesIncremental.ConvertFromLogarithmicScale(candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));

            Assert.IsTrue(attract5To2 > attract5To1);
            Assert.IsTrue(attract5To2 > attract5To3);
            Assert.AreEqual(attract5To3, attract5To1, 0.0001);
        }


        [Test]
        public void TestADCAttractIntersectAndUnion()
        {
            (int, int)[] edgesFromTo =
            {
                (1, 2), (3,2)
            };

            this.AddArchitecture(numberNodes: 3, edgesFromTo);

            edgesFromTo = new (int, int)[]
            {
                (1,2),
                (3,2),
                (5,4),
            };

            this.AddImplementation(numberNodes: 5, edgesFromTo);

            /**
             * Architecture: 
             * 
             * A1 -> A2
             * A3 -> A2
             * 
             * Implementation:
             * 
             * i1 -> i2
             * i3 -> i3 
             * i5 -> i4
             * 
             * */

            this.nodeReader = new TestNodeReader();
            this.nodeReader.SetLookUp(i[3].ID, "word2");
            this.nodeReader.SetLookUp(i[5].ID, "word1 word2");
            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Intersection);

            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[2], a[2]);
            this.AddToMapping(i[4], a[2]);
            this.AddToMapping(i[3], a[3]);

            /**
             * 
             * E is edge in Architecture
             * e is edge in Implementation
             * 
             * doc(E) = { Union of all doc(e) | for all e where AllowedBy(e) == E }
             *
             * doc(e) = if mergingtype is union : doc(e.source) union with doc(e.target)
             *          if mergingtype is intersect : doc(e.source) intersect with doc(e.target)
             *          
             * dis() is overlap(X,Y) = |(X Intersect Y)| / Min(|X|, |Y|)
             * 
             * Matches(A,i) = { e | (e.source = i or e.target = i) and i maps to A => e is allowed or e is implicitlyAllowed}
             * 
             * AllowedBy(e) = E | E.Source in MapsTo(e.Source).Outgoings and E.Target in MapsTo(e.Source).Incomings
             * 
             * ADC(A, i) = Sum of dis(doc(AllowedBy(e)), doc(e)) | e in Matches(A,i)
             * 
             * i1 mapsto A1
             * i2 mapsto A2
             * i3 mapsto A3
             * 
             * i4 mapsto A2
             * 
             * 
             * doc(A1#A2) = { Union of all doc(e) | e is allowed through A1#A2 }
             *            = doc(i1#i2)
             *            = doc(i1) intersect doc(i2)
             *            = {w1} intersect {w1}
             *            = {w1}
             * 
             * doc(A3#A2) = { Union of all doc(e) | e is allowed through A3#A2 }
             *            = doc(i3#i2)
             *            = doc(i3) intersect doc(i2)
             *            = {w2} intersect {w1}
             *            = {}
             * */

            string trainingData = candidateRecommendation.AttractFunction.DumpTrainingData();
            UnityEngine.Debug.Log(trainingData);

            /**
             *  
             *  
             *  First case Intersect:
             *  
             *  doc(A1#A1) = {}
             *  doc(A1#A2) = {w1}
             *  doc(A2#A2) = {} 
             *  doc(A3#A2) = {}
             *  doc(A3#A3) = {}
             *
             *  ADC(A1, i5) = dis(doc(A1#A2), (i5#i4))
             *              = dis({w1}, {w1})
             *              = 1 / 1 
             *              = 1
             *  *  
             *  ADC(A2, i5) = dis(doc(A2#A2), doc(i5#i4))
             *              = dis({}, {w1})
             *              = 0
             *              
             *  ADC(A3, i5) = dis(doc(A2#A3), doc(i5#i4))
             *              = dis({}, {w1,w2})
             *              = 0
             *  
             * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));

            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Union);

            /**
              *  
              * dis() is overlap(X,Y) =  |(X Intersect Y)| / Min(|X|, |Y|)
              *  
              * doc(A1#A2) = { Union of all doc(e) | e is allowed through A1#A2 }
              *            = doc(i1#i2)
              *            = doc(i1) union with doc(i2)
              *            = {w1} union with {w1}
              *            = {w1}
              * 
              * doc(A3#A2) = { Union of all doc(e) | e is allowed through A3#A2 }
              *            = doc(i3#i2)
              *            = doc(i3) union doc(i2)
              *            = {w2} union {w1}
              *            = {w1 w2}  
              *  
              *  doc(A1#A1) = {}
              *  doc(A1#A2) = {w1}
              *  doc(A2#A2) = {} 
              *  doc(A3#A2) = {w1 w2}
              *  doc(A3#A3) = {}
              *
              *  ADC(A1, i5) = dis(doc(A1#A2), (i5#i4))
              *              = dis({w1}, {w1 w2})
              *              = 1 / 1 
              *              = 1
              *  *  
              *  ADC(A2, i5) = dis(doc(A2#A2), doc(i5#i4))
              *              = dis({}, {w1 w2})
              *              = 0
              *              
              *  ADC(A3, i5) = dis(doc(A2#A3), doc(i5#i4))
              *              = dis({w1 w2}, {w1 w2})
              *              = 2 / 2
              *              = 1
              *  
              * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));

            IEnumerable<Edge> archEdges = this.graph.Edges().Where(e => e.IsInArchitecture() && ReflexionGraph.IsSpecified(e));

            foreach (Edge archEdge in archEdges)
            {
                this.RemoveEdge(archEdge);
            }

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));

            Assert.That(this.candidateRecommendation.AttractFunction.EmptyTrainingData());

            foreach (Edge archEdge in archEdges)
            {
                this.AddEdge(archEdge);
            }

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[2]));
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[5], a[3]));

            graph.ResetMapping();

            Assert.That(this.candidateRecommendation.AttractFunction.EmptyTrainingData());
        }

        [Test]
        public void TestADCAttractImplicitlyAllowed()
        {
            // no architecture dependencies
            this.AddArchitecture(numberNodes: 2, new (int, int)[] { });

            (int, int)[] edgesFromTo =
            {
                (2, 1), (3, 2), (5, 4), (6, 5), (7,1), (7,4)
            };

            this.AddImplementation(numberNodes: 7, edgesFromTo);

            this.nodeReader = new TestNodeReader();
            this.nodeReader.SetLookUp(i[5].ID, "word2");
            this.nodeReader.SetLookUp(i[7].ID, "word1 word2");
            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Intersection);

            this.AddToMapping(i[2], a[1]);
            this.AddToMapping(i[5], a[2]);

            /**
             * For more details of calculation see the test case ADCAttractIntersectAndUnion
             * 
             * No implicit dependencies should contain any words, so all possible 
             * attraction values should be 0.
             * 
             * */

            Assert.That(this.candidateRecommendation.AttractFunction.EmptyTrainingData());

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[3], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[3], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[6], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[6], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[2]));

            this.AddToMapping(i[3], a[1]);
            this.AddToMapping(i[6], a[2]);

            /**
             *  doc(A1#A1) = doc(i3#i2)
             *             = doc(i3) intersect doc(i2)
             *             = {w1} intersect {w1}
             *             = {w1}
             *  doc(A2#A2) = doc(i6#i5)
             *             = doc(i6) intersect doc(i5)
             *             = {w2} intersect {w1}
             *             = {}
             * 
             * ADC(i1, A1) = dis(doc(A1#A1), doc(i2#i1))
             *             = dis({w1}, doc(i2) intersect doc(i1))
             *             = dis({w1}, {w1} intersect {w1})
             *             = dis({w1}, {w1})
             *             = 1
             *             
             * ADC(i1, A2) = 0 // no matching implementation edges
             * 
             * 
             * ADC(i4, A1) = 0 // no matching implementation edges
             * 
             * ADC(i4, A2) = dis(doc(A2#A2), doc(i5#i4))
             *             = dis({}, doc(i5#i4))
             *             = 0 
             * 
             * ADC(i7, A1) = 0 // no matching implementation edges
             * ADC(i7, A2) = 0 // no matching implementation edges
             * 
             * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[2]));

            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Union);

            /**
             *  doc(A1#A1) = doc(i3#i2)
             *             = doc(i3) union doc(i2)
             *             = {w1} union {w1}
             *             = {w1}
             *  doc(A2#A2) = doc(i6#i5)
             *             = doc(i6) union doc(i5)
             *             = {w2} union {w1}
             *             = {w1 w2}
             * 
             * ADC(i1, A1) = dis(doc(A1#A1), doc(i2#i1))
             *             = dis({w1}, doc(i2) union doc(i1))
             *             = dis({w1}, {w1} union {w1})
             *             = dis({w1}, {w1})
             *             = 1
             *             
             * ADC(i1, A2) = 0 // no matching implementation edges
             * 
             * 
             * ADC(i4, A1) = 0 // no matching implementation edges
             * 
             * ADC(i4, A2) = dis(doc(A2#A2), doc(i5#i4))
             *             = dis({w1 w2}, doc(i5) union doc(i4))
             *             = dis({w1 w2}, {w2} union {w1})
             *             = dis({w1 w2}, {w2, w1})
             *             = 1
             * 
             * ADC(i7, A1) = 0 // no matching implementation edges
             * ADC(i7, A2) = 0 // no matching implementation edges
             * 
             * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[1], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[2]));

            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[2]));

            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Intersection);

            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[4], a[2]);

            /**
             * 
             *  doc(A1#A1) = doc(i3#i2) union doc(i2#i1)
             *             = ({w1} intersect {w1}) union ({w1} intersect {w1})
             *             = {w1} union {w1}
             *             = {w1}
             *             
             *  doc(A2#A2) = doc(i6#i5) union doc(i5#i4)
             *             = (doc(i6) intersect doc(i5)) union (doc(i5) intersect doc(i4))
             *             = ({w2} intersect {w1}) union ({w1} intersect {w2})
             *             = {} union {}
             *             = {}
             *             
             * ADC(i7, A1) = dis(doc(A1#A1), doc(i7#i1))
             *             = dis({w1}, doc(i7) intersect doc(i1))
             *             = dis({w1}, ({w1 w2} intersect {w1})
             *             = dis({w1}, {w1})
             *             = 1
             *             
             * ADC(i7, A2) = dis(doc(A2#A2), doc(i7#i4))
             *             = dis({}, doc(i7) intersect doc(i4))
             *             = 0
             *
             * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[1]));
            Assert.AreEqual(0, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[2]));

            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Union);

            /**
             * 
             *  doc(A1#A1) = doc(i3#i2) union doc(i2#i1)
             *             = ({w1} union {w1}) union ({w1} union {w1})
             *             = {w1} union {w1}
             *             = {w1}
             *             
             *  doc(A2#A2) = doc(i6#i5) union doc(i5#i4)
             *             = (doc(i6) intersect doc(i5)) union (doc(i5) intersect doc(i4))
             *             = ({w2} union {w1}) union ({w1} inion {w2})
             *             = {w1 w2} union {w1 w2}
             *             = {w1 w2}
             *             
             * ADC(i7, A1) = dis(doc(A1#A1), doc(i7#i1))
             *             = dis({w1}, doc(i7) union doc(i1))
             *             = dis({w1}, ({w1 w2} union {w1})
             *             = dis({w1}, {w1 w2})
             *             = 1
             *             
             * ADC(i7, A2) = dis(doc(A2#A2), doc(i7#i4))
             *             = dis({w1 w2}, doc(i7) intersect doc(i4))
             *             = dis({w1 w2}, {w1 w2} intersect {w1})
             *             = dis({w1 w2}, {w1})
             *             = 1
             *
             * */

            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[1]));
            Assert.AreEqual(1, candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[2]));

            graph.ResetMapping();

            Assert.That(this.candidateRecommendation.AttractFunction.EmptyTrainingData());
        }

        [Test]
        public void TestCountAttract()
        {
            (int, int)[] edgesFromTo =
            {
                (1, 2)
            };

            this.AddArchitecture(numberNodes: 2, edgesFromTo: edgesFromTo);

            /// i4(d) => i1(a)
            /// i4(d) => i2(b)
            /// i4(d) => i3(c)
            edgesFromTo = new (int, int)[]
            {
                (4, 1), (4, 2), (4, 3)
            };

            this.AddImplementation(numberNodes: 4, edgesFromTo);

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
             * 1 mapsto A1
             * 2 mapsto A2
             * 3 mapsto A2
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

            SetupCountAttract(0.6f);

            Assert.That(DIsRecommendedForA2ButNotA1());

            SetupCountAttract(0.5f);

            Assert.That(DIsRecommendedForA1AndA2());

            // update phi value
            SetupCountAttract(0.4f);

            // check if i4(d) is in the Recommendations for a1(A1) but not a2(A2)
            Assert.That(DIsRecommendedForA1ButNotA2());

            // remove arch edge to ignore phi value
            Edge archEdge = this.graph.Edges().Where(e => e.IsInArchitecture()).SingleOrDefault();
            this.graph.RemoveEdge(archEdge);
            this.candidateRecommendation.UpdateRecommendations();

            Assert.That(DIsRecommendedForA2ButNotA1());

            // add arch edge again to ensure the phi value still affects the recommendations
            this.graph.AddEdge(archEdge);
            this.candidateRecommendation.UpdateRecommendations();

            Assert.That(DIsRecommendedForA1ButNotA2());

            this.graph.RemoveNode(a[1]);
            this.candidateRecommendation.UpdateRecommendations();

            Assert.That(DIsRecommendedForA2ButNotA1());

            this.ResetMapping();

            Assert.That(candidateRecommendation.GetAutomaticMappings().Count() == 0);

            Assert.That(candidateRecommendation.AttractFunction.EmptyTrainingData());

            #region local functions
            bool DIsRecommendedForA1AndA2()
            {
                IList<MappingPair> recommendationsOfD = candidateRecommendation.GetRecommendations(i[4]).ToList();
                IEnumerable<MappingPair> recommendationsOfA1 = candidateRecommendation.GetRecommendations(a[1]);
                IEnumerable<MappingPair> recommendationsOfA2 = candidateRecommendation.GetRecommendations(a[2]);

                MappingPair recommendation0 = recommendationsOfD[0];
                MappingPair recommendation1 = recommendationsOfD[1];

                bool validForD = recommendation0.CandidateID.Equals(i[4].ID)
                          && recommendation0.CandidateID.Equals(i[4].ID)
                          && (recommendation0.ClusterID.Equals(a[1].ID) && recommendation1.ClusterID.Equals(a[2].ID)
                             || recommendation0.ClusterID.Equals(a[2].ID) && recommendation1.ClusterID.Equals(a[1].ID));

                bool validForA1 = recommendationsOfA1.FirstOrDefault().CandidateID.Equals(i[4].ID)
                               && recommendationsOfA1.FirstOrDefault().ClusterID.Equals(a[1].ID);

                bool validForA2 = recommendationsOfA2.FirstOrDefault().CandidateID.Equals(i[4].ID)
                               && recommendationsOfA2.FirstOrDefault().ClusterID.Equals(a[2].ID);

                return validForD && validForA1 && validForA2;
            }

            bool DIsRecommendedForA1ButNotA2()
            {
                IList<MappingPair> recommendationsOfD = candidateRecommendation.GetRecommendations(i[4]).ToList();
                IEnumerable<MappingPair> recommendationsOfA1 = candidateRecommendation.GetRecommendations(a[1]);
                IEnumerable<MappingPair> recommendationsOfA2 = candidateRecommendation.GetRecommendations(a[2]);

                MappingPair recommendation = recommendationsOfD[0];
                bool validForD = recommendationsOfD.Count() == 1
                              && recommendation.CandidateID.Equals(i[4].ID)
                              && recommendation.ClusterID.Equals(a[1].ID);

                recommendation = recommendationsOfA1.FirstOrDefault();

                bool validForA1 = recommendation.CandidateID.Equals(i[4].ID)
                               && recommendation.ClusterID.Equals(a[1].ID);

                bool validForA2 = recommendationsOfA2.Count() == 0;

                return validForD && validForA1 && validForA2;
            }

            bool DIsRecommendedForA2ButNotA1()
            {
                IList<MappingPair> recommendationsOfD = candidateRecommendation.GetRecommendations(i[4]).ToList();
                IEnumerable<MappingPair> recommendationsOfA1 = candidateRecommendation.GetRecommendations(a[1]);
                IEnumerable<MappingPair> recommendationsOfA2 = candidateRecommendation.GetRecommendations(a[2]);

                MappingPair recommendation = recommendationsOfD[0];
                bool validForD = recommendationsOfD.Count() == 1
                              && recommendation.CandidateID.Equals(i[4].ID)
                              && recommendation.ClusterID.Equals(a[2].ID);

                recommendation = recommendationsOfA2.FirstOrDefault();

                bool validForA2 = recommendation.CandidateID.Equals(i[4].ID)
                               && recommendation.ClusterID.Equals(a[2].ID);

                bool validForA1 = recommendationsOfA1.Count() == 0;

                return validForD && validForA1 && validForA1;
            }
            #endregion
        }

        [Test]
        public void TestADCAttractHierarchical()
        {
            (int, int)[] edgesFromTo = {(1,2),(2,1)}; // allow birectional dependencies

            this.AddArchitecture(numberNodes: 2, edgesFromTo: edgesFromTo);

            edgesFromTo = new (int, int)[]
            {
                (1,2), (1,11), (11,1), // training edges  
                (3,8), (5,10), // incoming edges of candidate tree
                (7,3), (8,4), (9,6), // outgoing edges of candidate tree 
            };

            this.AddImplementation(numberNodes: 11, edgesFromTo);

            edgesFromTo = new (int, int)[]
            {
                (4,3),(5,3),(6,4), // mapped tree
                (8,7),(9,7),(10,9) // candidate tree
            };

            this.AddHierarchy(edgesFromTo, ReflexionSubgraphs.Implementation);

            this.nodeReader = new TestNodeReader();
            SetupADCAttract(this.nodeReader, Document.DocumentMergingType.Intersection);

            // setup initial mapping to train the abstract dependencies:
            // doc(A1 -> A1) = {w1}
            // doc(A1 -> A2) = {w2}
            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[2], a[1]);
            this.AddToMapping(i[11], a[2]);

            this.AddToMapping(i[3], a[1]);

            // every edge in subtree should be overlapping by 1
            void runAsserts(int archIndex)
            {
                Assert.AreEqual(5, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[7], a[archIndex]));
                Assert.AreEqual(2, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[8], a[archIndex]));
                Assert.AreEqual(2, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[9], a[archIndex]));
                Assert.AreEqual(1, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[10], a[archIndex])); 
            }
            
            runAsserts(1);
            runAsserts(2);

            this.AddToMapping(i[7], a[1]);
            this.RemoveFromMapping(i[7]);

            runAsserts(1);
            runAsserts(2);

            this.AddToMapping(i[7], a[2]);
            this.RemoveFromMapping(i[7]);

            runAsserts(1);
            runAsserts(2);
        }

        /**
         * TODO: cite source of this Testcase
         * */
        [Test]
        public void TestCountAttractHierarchical()
        {
            (int, int)[] edgesFromTo =
            {
                (2, 1),(2,3)
            };

            this.AddArchitecture(numberNodes: 3, edgesFromTo: edgesFromTo);

            edgesFromTo = new (int, int)[]
            {
               (5,1),(6,3),(6,2),(6,5),(6,8)
            };

            this.AddImplementation(numberNodes: 8, edgesFromTo);

            edgesFromTo = new (int, int)[]
            {
                (2,1),(3,1),(5,4),(6,4),(8,7)
            };

            this.AddHierarchy(edgesFromTo, ReflexionSubgraphs.Implementation);

            edgesFromTo = new (int, int)[]
            {
                (6,2), (6,3), (6,8)
            };

            this.OverrideDefaultEdgeType(edgesFromTo, ReflexionSubgraphs.Implementation, "Use");

            Dictionary<string, double> edgeWeights = new Dictionary<string, double>()
            {
                { "Use", 2.0 }
            };

            SetupCountAttract(phi: 1.0f, edgeWeights);

            // Initial Mapping
            this.AddToMapping(i[1], a[1]);
            this.AddToMapping(i[7], a[3]);

            Assert.AreEqual(5, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));

            this.RemoveFromMapping(i[1]);

            Assert.AreEqual(0, this.candidateRecommendation.AttractFunction.GetAttractionValue(i[4], a[1]));
        }
    }
}