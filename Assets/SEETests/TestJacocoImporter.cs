using NUnit.Framework;
using SEE.DataModel.DG.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit-Tests for JaCoCoImporter
    /// </summary>
    internal class TestJacocoImporter
    {
        /// <summary>
        /// The name of the hierarchical edge type we use for emitting the parent-child
        /// relation among nodes.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";

        /// <summary>
        /// Load Graph from GXL file <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">GXL file</param>
        /// <returns>loaded graph</returns>
        private static Graph LoadGraph(string filename)
        {
            GraphReader graphReader = new(filename, new HashSet<string> { hierarchicalEdgeType }, basePath: "");
            graphReader.Load();
            return graphReader.GetGraph();
        }

        /// <summary>
        /// The graph that was loaded by <see cref="SetUp"/> before each test case is executed.
        /// </summary>
        private Graph graph;

        [SetUp]
        public void SetUp()
        {
            string gxlPath = Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz";
            string xmlPath = Application.streamingAssetsPath + "/JLGExample/jacoco.xml";

            graph = LoadGraph(gxlPath);
            JaCoCoImporter.Load(graph, xmlPath);
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;
        }

        /// <summary>
        /// Test if metrics are set for the project root. In JaCoCo-Test-Report it is named "report".
        /// </summary>
        [Test]
        public void AddMetricToRootNode()
        {
            Node nodeToTest = graph.GetRoots()[0];
            Assert.IsNotNull(nodeToTest);

            Assert.AreEqual(1313, nodeToTest.GetInt(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(441, nodeToTest.GetInt(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(101, nodeToTest.GetInt(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(27, nodeToTest.GetInt(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(330, nodeToTest.GetInt(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(83, nodeToTest.GetInt(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(107, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(20, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(55, nodeToTest.GetInt(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(8, nodeToTest.GetInt(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(6, nodeToTest.GetInt(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(4, nodeToTest.GetInt(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a class node. In JaCoCo-Test-Report it is named "class".
        /// </summary>
        [Test]
        public void AddMetricToClassNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants");
            Assert.IsNotNull(nodeToTest);

            Assert.AreEqual(7f, nodeToTest.GetInt(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(130f, nodeToTest.GetInt(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0.0f, nodeToTest.GetInt(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(6f, nodeToTest.GetInt(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(3f, nodeToTest.GetInt(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(11f, nodeToTest.GetInt(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(2f, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(5f, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(2f, nodeToTest.GetInt(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(2f, nodeToTest.GetInt(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(0f, nodeToTest.GetInt(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(1f, nodeToTest.GetInt(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a package node. In JaCoCo-Test-Report it is named "package".
        /// </summary>
        [Test]
        public void AddMetricToPackageNode()
        {
            Node nodeToTest = graph.GetNode("counter");
            Assert.IsNotNull(nodeToTest);

            Assert.AreEqual(31, nodeToTest.GetInt(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(313, nodeToTest.GetInt(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(1, nodeToTest.GetInt(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(17, nodeToTest.GetInt(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(13, nodeToTest.GetInt(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(45, nodeToTest.GetInt(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(9, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(14, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(8, nodeToTest.GetInt(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(6, nodeToTest.GetInt(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(3, nodeToTest.GetInt(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a method node. In JaCoCo-Test-Report it is named "method".
        /// </summary>
        [Test]
        public void AddMetricToMethodNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants.countConsonants(java.lang.String;)");

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(39, nodeToTest.GetInt(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(6, nodeToTest.GetInt(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(8, nodeToTest.GetInt(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(4, nodeToTest.GetInt(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(AttributeNamesExtensions.MethodCovered));
        }
    }
}