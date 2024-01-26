using Assets.SEE.DataModel.DG.IO;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.DataModel.DG
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
        /// Load Graph from gxl
        /// </summary>
        /// <param name="filename">gxl-file</param>
        /// <returns>Graph</returns>
        private static Graph LoadGraph(string filename)
        {
            GraphReader graphReader = new(filename, new HashSet<string> { hierarchicalEdgeType }, basePath: "");
            graphReader.Load();
            return graphReader.GetGraph();
        }

        /// <summary>
        /// Test if metrics are set for the Project-Root. In JaCoCo-Test-Report its named "report".
        /// </summary>
        [Test]
        public void AddMetricToRootNode()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/CodeFacts.gxl";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/jacocoTestReport(3).xml";
            Graph graph = LoadGraph(gxlPath);
            Node nodeToTest = graph.GetRoots()[0];

            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);

            float floatInstructionMissed = nodeToTest.GetFloat("Metric.INSTRUCTION_missed");
            float floatInstructionCovered = nodeToTest.GetFloat("Metric.INSTRUCTION_covered");

            float floatBranchMissed = nodeToTest.GetFloat("Metric.BRANCH_missed");
            float floatBranchCovered = nodeToTest.GetFloat("Metric.BRANCH_covered");

            float floatLineMissed = nodeToTest.GetFloat("Metric.LINE_missed");
            float floatLineCovered = nodeToTest.GetFloat("Metric.LINE_covered");

            float floatComplexityMissed = nodeToTest.GetFloat("Metric.COMPLEXITY_missed");
            float floatComplexityCovered = nodeToTest.GetFloat("Metric.COMPLEXITY_covered");

            float floatMethodMissed = nodeToTest.GetFloat("Metric.METHOD_missed");
            float floatMethodCovered = nodeToTest.GetFloat("Metric.METHOD_covered");

            float floatClassMissed = nodeToTest.GetFloat("Metric.CLASS_missed");
            float floatClassCovered = nodeToTest.GetFloat("Metric.CLASS_covered");

            Assert.AreEqual(1395f, floatInstructionMissed);
            Assert.AreEqual(494f, floatInstructionCovered);

            Assert.AreEqual(110f, floatBranchMissed);
            Assert.AreEqual(22f, floatBranchCovered);

            Assert.AreEqual(351f, floatLineMissed);
            Assert.AreEqual(100f, floatLineCovered);

            Assert.AreEqual(102f, floatComplexityMissed);
            Assert.AreEqual(37f, floatComplexityCovered);

            Assert.AreEqual(47f, floatMethodMissed);
            Assert.AreEqual(26f, floatMethodCovered);

            Assert.AreEqual(5f, floatClassMissed);
            Assert.AreEqual(7f, floatClassCovered);
        }

        /// <summary>
        /// Test if metrics are set for class-node. In JaCoCo-Test-Report its named "class".
        /// </summary>
        [Test]
        public void AddMetricToClassNode()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/CodeFacts.gxl";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/jacocoTestReport(3).xml";
            Graph graph = LoadGraph(gxlPath);
            Node nodeToTest = graph.GetNode("BankAccount.Account");

            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);

            float floatInstructionMissed = nodeToTest.GetFloat("Metric.INSTRUCTION_missed");
            float floatInstructionCovered = nodeToTest.GetFloat("Metric.INSTRUCTION_covered");

            float floatBranchMissed = nodeToTest.GetFloat("Metric.BRANCH_missed");
            float floatBranchCovered = nodeToTest.GetFloat("Metric.BRANCH_covered");

            float floatLineMissed = nodeToTest.GetFloat("Metric.LINE_missed");
            float floatLineCovered = nodeToTest.GetFloat("Metric.LINE_covered");

            float floatComplexityMissed = nodeToTest.GetFloat("Metric.COMPLEXITY_missed");
            float floatComplexityCovered = nodeToTest.GetFloat("Metric.COMPLEXITY_covered");

            float floatMethodMissed = nodeToTest.GetFloat("Metric.METHOD_missed");
            float floatMethodCovered = nodeToTest.GetFloat("Metric.METHOD_covered");

            float floatClassMissed = nodeToTest.GetFloat("Metric.CLASS_missed");
            float floatClassCovered = nodeToTest.GetFloat("Metric.CLASS_covered");

            Assert.AreEqual(0f, floatInstructionMissed);
            Assert.AreEqual(44f, floatInstructionCovered);

            Assert.AreEqual(0.0f, floatBranchMissed);
            Assert.AreEqual(2f, floatBranchCovered);

            Assert.AreEqual(0f, floatLineMissed);
            Assert.AreEqual(14f, floatLineCovered);

            Assert.AreEqual(0f, floatComplexityMissed);
            Assert.AreEqual(7f, floatComplexityCovered);

            Assert.AreEqual(0f, floatMethodMissed);
            Assert.AreEqual(6f, floatMethodCovered);

            Assert.AreEqual(0f, floatClassMissed);
            Assert.AreEqual(1f, floatClassCovered);
        }

        /// <summary>
        /// Test if metrics are set for class-node, when there is a second class with the same name.
        /// </summary>
        [Test]
        public void AddMetricToClassNodeWhichNameIsUsedTwice()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/CodeFacts.gxl";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/jacocoTestReport(3).xml";
            Graph graph = LoadGraph(gxlPath);
            Node nodeToTest = graph.GetNode("BankAccount.Main");

            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);

            float floatInstructionMissed = nodeToTest.GetFloat("Metric.INSTRUCTION_missed");
            float floatInstructionCovered = nodeToTest.GetFloat("Metric.INSTRUCTION_covered");

            float floatBranchMissed = nodeToTest.GetFloat("Metric.BRANCH_missed");
            float floatBranchCovered = nodeToTest.GetFloat("Metric.BRANCH_covered");

            float floatLineMissed = nodeToTest.GetFloat("Metric.LINE_missed");
            float floatLineCovered = nodeToTest.GetFloat("Metric.LINE_covered");

            float floatComplexityMissed = nodeToTest.GetFloat("Metric.COMPLEXITY_missed");
            float floatComplexityCovered = nodeToTest.GetFloat("Metric.COMPLEXITY_covered");

            float floatMethodMissed = nodeToTest.GetFloat("Metric.METHOD_missed");
            float floatMethodCovered = nodeToTest.GetFloat("Metric.METHOD_covered");

            float floatClassMissed = nodeToTest.GetFloat("Metric.CLASS_missed");
            float floatClassCovered = nodeToTest.GetFloat("Metric.CLASS_covered");

            Assert.AreEqual(3f, floatInstructionMissed);
            Assert.AreEqual(77f, floatInstructionCovered);

            Assert.AreEqual(0f, floatBranchMissed);
            Assert.AreEqual(2f, floatBranchCovered);

            Assert.AreEqual(1f, floatLineMissed);
            Assert.AreEqual(17f, floatLineCovered);

            Assert.AreEqual(1f, floatComplexityMissed);
            Assert.AreEqual(2f, floatComplexityCovered);

            Assert.AreEqual(1f, floatMethodMissed);
            Assert.AreEqual(1f, floatMethodCovered);

            Assert.AreEqual(0f, floatClassMissed);
            Assert.AreEqual(1f, floatClassCovered);
        }

        /// <summary>
        /// Test if metrics are set for package-node. In JaCoCo-Test-Report its named "package".
        /// </summary>
        [Test]
        public void AddMetricToPackageNode()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/CodeFacts.gxl";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/jacocoTestReport(3).xml";
            Graph graph = LoadGraph(gxlPath);
            Node nodeToTest = graph.GetNode("BankAccount");

            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);

            float floatInstructionMissed = nodeToTest.GetFloat("Metric.INSTRUCTION_missed");
            float floatInstructionCovered = nodeToTest.GetFloat("Metric.INSTRUCTION_covered");

            float floatBranchMissed = nodeToTest.GetFloat("Metric.BRANCH_missed");
            float floatBranchCovered = nodeToTest.GetFloat("Metric.BRANCH_covered");

            float floatLineMissed = nodeToTest.GetFloat("Metric.LINE_missed");
            float floatLineCovered = nodeToTest.GetFloat("Metric.LINE_covered");

            float floatComplexityMissed = nodeToTest.GetFloat("Metric.COMPLEXITY_missed");
            float floatComplexityCovered = nodeToTest.GetFloat("Metric.COMPLEXITY_covered");

            float floatMethodMissed = nodeToTest.GetFloat("Metric.METHOD_missed");
            float floatMethodCovered = nodeToTest.GetFloat("Metric.METHOD_covered");

            float floatClassMissed = nodeToTest.GetFloat("Metric.CLASS_missed");
            float floatClassCovered = nodeToTest.GetFloat("Metric.CLASS_covered");

            Assert.AreEqual(3f, floatInstructionMissed);
            Assert.AreEqual(153f, floatInstructionCovered);

            Assert.AreEqual(0f, floatBranchMissed);
            Assert.AreEqual(4f, floatBranchCovered);

            Assert.AreEqual(1f, floatLineMissed);
            Assert.AreEqual(43f, floatLineCovered);

            Assert.AreEqual(1f, floatComplexityMissed);
            Assert.AreEqual(15f, floatComplexityCovered);

            Assert.AreEqual(1f, floatMethodMissed);
            Assert.AreEqual(13f, floatMethodCovered);

            Assert.AreEqual(0f, floatClassMissed);
            Assert.AreEqual(4f, floatClassCovered);
        }

        /// <summary>
        /// Test if metrics are set for method-node. In JaCoCo-Test-Report its named "method".
        /// </summary>
        [Test]
        public void AddMetricToMethodNode()
        {
            string gxlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/CodeFacts.gxl";
            string xmlPath = Application.dataPath + "/../Assets/StreamingAssets/gradleProject/jacocoTestReport(3).xml";
            Graph graph = LoadGraph(gxlPath);
            Node nodeToTest = graph.GetNode("BankAccount.Bank.getName()");

            JaCoCoImporter.StartReadingTestXML(graph, xmlPath);

            float floatInstructionMissed = nodeToTest.GetFloat("Metric.INSTRUCTION_missed");
            float floatInstructionCovered = nodeToTest.GetFloat("Metric.INSTRUCTION_covered");

            float floatLineMissed = nodeToTest.GetFloat("Metric.LINE_missed");
            float floatLineCovered = nodeToTest.GetFloat("Metric.LINE_covered");

            float floatComplexityMissed = nodeToTest.GetFloat("Metric.COMPLEXITY_missed");
            float floatComplexityCovered = nodeToTest.GetFloat("Metric.COMPLEXITY_covered");

            float floatMethodMissed = nodeToTest.GetFloat("Metric.METHOD_missed");
            float floatMethodCovered = nodeToTest.GetFloat("Metric.METHOD_covered");

            Assert.AreEqual(0f, floatInstructionMissed);
            Assert.AreEqual(3f, floatInstructionCovered);

            Assert.AreEqual(0f, floatLineMissed);
            Assert.AreEqual(1f, floatLineCovered);

            Assert.AreEqual(0f, floatComplexityMissed);
            Assert.AreEqual(1f, floatComplexityCovered);

            Assert.AreEqual(0f, floatMethodMissed);
            Assert.AreEqual(1f, floatMethodCovered);
        }
    }
}