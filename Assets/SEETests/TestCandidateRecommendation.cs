using System;
using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Tools.ReflexionAnalysis;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.Tools.ReflexionAnalysis;
using System.Globalization;

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
        }

        private void SetupCountAttract(float phi = 1.0f)
        {
            MappingExperimentConfig config = new MappingExperimentConfig();
            CountAttractConfig attractConfig = new CountAttractConfig();
            attractConfig.Phi = phi;
            attractConfig.CandidateType = "Candidate";
            attractConfig.ClusterType = "Cluster";
            config.AttractFunctionConfig = attractConfig;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
        }

        private void SetupNBAttract(INodeReader nodeReader, bool useCda = false)
        {
            MappingExperimentConfig config = new MappingExperimentConfig();
            NBAttractConfig attractConfig = new NBAttractConfig();
            config.NodeReader = nodeReader;
            attractConfig.UseCDA = useCda;
            attractConfig.CandidateType = "Candidate";
            attractConfig.ClusterType = "Cluster";
            attractConfig.AlphaSmoothing = 1.0;
            config.AttractFunctionConfig = attractConfig;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
        }

        private void SetupADCAttract(INodeReader nodeReader, Document.DocumentMergingType mergingType) 
        {
            MappingExperimentConfig config = GetADCAttractConfig(mergingType);
            config.NodeReader = nodeReader;
            candidateRecommendation.UpdateConfiguration(graph, config);
            candidateRecommendation.UpdateRecommendations();
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

            this.nodeReader = new NodeReaderTest();
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

            this.AddArchitecture(numberNodes : 3, edgesFromTo);

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

            this.nodeReader = new NodeReaderTest();
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
             * Matches(A,i) = { e | (e.source = i or e.target = e) and i maps to A => e is allowed or e is implicitlyAllowed}
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
            this.AddArchitecture(numberNodes: 2, new (int,int)[]{});
            
            (int, int)[] edgesFromTo =
            {
                (2, 1), (3, 2), (5, 4), (6, 5), (7,1), (7,4)
            };

            this.AddImplementation(numberNodes: 7, edgesFromTo);

            this.nodeReader = new NodeReaderTest();
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