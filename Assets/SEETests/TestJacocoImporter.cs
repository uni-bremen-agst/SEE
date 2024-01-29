using NUnit.Framework;
using SEE.DataModel.DG.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG
{
    // FIXME:
    // TODO (#672) Change path of test-data

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

        private Graph graph;

        [SetUp]
        public void SetUp()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/JLGExample/CodeFacts.gxl.xz";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/JLGExample/jacoco.xml";

            graph = LoadGraph(gxlPath);
            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;   
        }

        /// <summary>
        /// Test if metrics are set for the Project-Root. In JaCoCo-Test-Report its named "report".
        /// </summary>
        [Test]
        public void AddMetricToRootNode()
        {
            Node nodeToTest = graph.GetRoots()[0];

            Assert.AreEqual(1395f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(494f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(110f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(22f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(351f, nodeToTest.GetFloat(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(100f, nodeToTest.GetFloat(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(102f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(37f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(47f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(26f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(5f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(7f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for class-node. In JaCoCo-Test-Report its named "class".
        /// </summary>
        [Test]
        public void AddMetricToClassNode()
        {
            Node nodeToTest = graph.GetNode("BankAccount.Account");

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(44f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0.0f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(2f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(14f, nodeToTest.GetFloat(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(7f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(6f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for class-node, when there is a second class with the same name.
        /// </summary>
        [Test]
        public void AddMetricToClassNodeWhichNameIsUsedTwice()
        {
            Node nodeToTest = graph.GetNode("BankAccount.Main");

            Assert.AreEqual(3f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(77f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(2f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(17f, nodeToTest.GetFloat(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(2f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for package-node. In JaCoCo-Test-Report its named "package".
        /// </summary>
        [Test]
        public void AddMetricToPackageNode()
        {
            Node nodeToTest = graph.GetNode("BankAccount");

            Assert.AreEqual(3f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(153f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchMissed));
            Assert.AreEqual(4f, nodeToTest.GetFloat(AttributeNamesExtensions.BranchCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(43f, nodeToTest.GetFloat(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(15f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(13f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassMissed));
            Assert.AreEqual(4f, nodeToTest.GetFloat(AttributeNamesExtensions.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for method-node. In JaCoCo-Test-Report its named "method".
        /// </summary>
        [Test]
        public void AddMetricToMethodNode()
        {
            Node nodeToTest = graph.GetNode("BankAccount.Bank.getName()");

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionMissed));
            Assert.AreEqual(3f, nodeToTest.GetFloat(AttributeNamesExtensions.InstructionCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.LineMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.LineCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.ComplexityCovered));

            Assert.AreEqual(0f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodMissed));
            Assert.AreEqual(1f, nodeToTest.GetFloat(AttributeNamesExtensions.MethodCovered));
        }
    }
}