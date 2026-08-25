using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG.IO;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

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
        /// Load Graph from GXL file <paramref name="path"/>.
        /// </summary>
        /// <param name="path">data path of GXL file</param>
        /// <returns>loaded graph</returns>
        private static async UniTask<Graph> LoadGraphAsync(DataPath path)
        {
            return await GraphReader.LoadAsync(path, new HashSet<string> { hierarchicalEdgeType }, basePath: "");
        }

        /// <summary>
        /// Folder where the JLGExample data reside.
        /// </summary>
        private static readonly string JLGExampleFolder = DataPath.ProjectFolder() + "/Data/JLGExample";

        /// <summary>
        /// The graph that was loaded by <see cref="SetUpAsync"/> before each test case is executed.
        /// </summary>
        private Graph graph;

        [SetUp]
        public async Task SetUpAsync()
        {
            GraphIndex.FileRanges.ReportMissingSourceRange = false;
            DataPath gxlPath = new(JLGExampleFolder + "/CodeFacts.gxl.xz");
            DataPath xmlPath = new(JLGExampleFolder + "/jacoco.xml");

            graph = await LoadGraphAsync(gxlPath);
            await JaCoCoImporter.LoadAsync(graph, xmlPath);
        }

        [TearDown]
        public void TearDown()
        {
            GraphIndex.FileRanges.ReportMissingSourceRange = true;
            graph = null;
        }

        /// <summary>
        /// Test if metrics are set for the project root. In JaCoCo-Test-Report it is named "report".
        /// </summary>
        [Test]
        public void AddMetricToRootNode()
        {
            Node nodeToTest = graph.GetRoots()[0];
            Assert.That(nodeToTest, Is.Not.Null, "The graph must have a root node.");

            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionMissed), Is.EqualTo(1313));
            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionCovered), Is.EqualTo(441));

            Assert.That(nodeToTest.GetInt(JaCoCo.BranchMissed), Is.EqualTo(101));
            Assert.That(nodeToTest.GetInt(JaCoCo.BranchCovered), Is.EqualTo(27));

            Assert.That(nodeToTest.GetInt(JaCoCo.LineMissed), Is.EqualTo(330));
            Assert.That(nodeToTest.GetInt(JaCoCo.LineCovered), Is.EqualTo(83));

            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityMissed), Is.EqualTo(107));
            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityCovered), Is.EqualTo(20));

            Assert.That(nodeToTest.GetInt(JaCoCo.MethodMissed), Is.EqualTo(55));
            Assert.That(nodeToTest.GetInt(JaCoCo.MethodCovered), Is.EqualTo(8));

            Assert.That(nodeToTest.GetInt(JaCoCo.ClassMissed), Is.EqualTo(6));
            Assert.That(nodeToTest.GetInt(JaCoCo.ClassCovered), Is.EqualTo(4));
        }

        /// <summary>
        /// Test if metrics are set for a class node. In JaCoCo-Test-Report it is named "class".
        /// </summary>
        [Test]
        public void AddMetricToClassNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants");
            Assert.That(nodeToTest, Is.Not.Null, "There is no node counter.CountConsonants.");

            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionMissed), Is.EqualTo(7f));
            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionCovered), Is.EqualTo(130f));

            Assert.That(nodeToTest.GetInt(JaCoCo.BranchMissed), Is.EqualTo(0.0f));
            Assert.That(nodeToTest.GetInt(JaCoCo.BranchCovered), Is.EqualTo(6f));

            Assert.That(nodeToTest.GetInt(JaCoCo.LineMissed), Is.EqualTo(3f));
            Assert.That(nodeToTest.GetInt(JaCoCo.LineCovered), Is.EqualTo(11f));

            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityMissed), Is.EqualTo(2f));
            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityCovered), Is.EqualTo(5f));

            Assert.That(nodeToTest.GetInt(JaCoCo.MethodMissed), Is.EqualTo(2f));
            Assert.That(nodeToTest.GetInt(JaCoCo.MethodCovered), Is.EqualTo(2f));

            Assert.That(nodeToTest.GetInt(JaCoCo.ClassMissed), Is.EqualTo(0f));
            Assert.That(nodeToTest.GetInt(JaCoCo.ClassCovered), Is.EqualTo(1f));
        }

        /// <summary>
        /// Test if metrics are set for a package node. In JaCoCo-Test-Report it is named "package".
        /// </summary>
        [Test]
        public void AddMetricToPackageNode()
        {
            Node nodeToTest = graph.GetNode("counter");
            Assert.That(nodeToTest, Is.Not.Null, "There is no node counter.");

            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionMissed), Is.EqualTo(31));
            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionCovered), Is.EqualTo(313));

            Assert.That(nodeToTest.GetInt(JaCoCo.BranchMissed), Is.EqualTo(1));
            Assert.That(nodeToTest.GetInt(JaCoCo.BranchCovered), Is.EqualTo(17));

            Assert.That(nodeToTest.GetInt(JaCoCo.LineMissed), Is.EqualTo(13));
            Assert.That(nodeToTest.GetInt(JaCoCo.LineCovered), Is.EqualTo(45));

            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityMissed), Is.EqualTo(9));
            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityCovered), Is.EqualTo(14));

            Assert.That(nodeToTest.GetInt(JaCoCo.MethodMissed), Is.EqualTo(8));
            Assert.That(nodeToTest.GetInt(JaCoCo.MethodCovered), Is.EqualTo(6));

            Assert.That(nodeToTest.GetInt(JaCoCo.ClassMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.ClassCovered), Is.EqualTo(3));
        }

        /// <summary>
        /// Test if metrics are set for a method node. In JaCoCo-Test-Report it is named "method".
        /// </summary>
        [Test]
        public void AddMetricToMethodNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants.countConsonants(java.lang.String;)");

            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionCovered), Is.EqualTo(39));

            Assert.That(nodeToTest.GetInt(JaCoCo.BranchMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.BranchCovered), Is.EqualTo(6));

            Assert.That(nodeToTest.GetInt(JaCoCo.LineMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.LineCovered), Is.EqualTo(8));

            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityCovered), Is.EqualTo(4));

            Assert.That(nodeToTest.GetInt(JaCoCo.MethodMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.MethodCovered), Is.EqualTo(1));
        }

        /// <summary>
        /// Here we only test whether data can be read from a URL. The nodes in the
        /// referenced file are not actually in the graph. So we expect error
        /// messages. Yet, we will add one package node to the graph that we
        /// know is contained in the JaCoCo XML file. We will then check whether
        /// the metrics are set correctly. There are more nodes in the file, but
        /// we will ignore these.
        /// </summary>
        [Test]
        public async Task TestLoadAsyncMethodAsync()
        {
            // Note: LogAssert.Expect(LogType.Error, new Regex(".*No node found for.*"))
            // does not work as expected in combination with awaiting an asynchronous
            // message. So we have to ignore all error messages.
            LogAssert.ignoreFailingMessages = true;

            DataPath path = new()
            {
                Root = DataPath.RootKind.Url,
                Path = "https://raw.githubusercontent.com/vokal/jacoco-parse/master/test/assets/sample.xml"
            };

            // We know this package node exists in the JaCoCo XML file.
            Node nodeToTest = new()
            {
                // Note: In the graph, the separator for qualified names is a dot, whereas a / is used in the
                // JaCoCo XML file.
                ID = "com.wmbest.myapplicationtest",
                Type = "package"
            };
            graph.AddNode(nodeToTest);

            await JaCoCoImporter.LoadAsync(graph, path);

            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionMissed), Is.EqualTo(30));
            Assert.That(nodeToTest.GetInt(JaCoCo.InstructionCovered), Is.EqualTo(10));

            Assert.That(nodeToTest.GetInt(JaCoCo.BranchMissed), Is.EqualTo(3));
            Assert.That(nodeToTest.GetInt(JaCoCo.BranchCovered), Is.EqualTo(1));

            Assert.That(nodeToTest.GetInt(JaCoCo.LineMissed), Is.EqualTo(10));
            Assert.That(nodeToTest.GetInt(JaCoCo.LineCovered), Is.EqualTo(3));

            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityMissed), Is.EqualTo(6));
            Assert.That(nodeToTest.GetInt(JaCoCo.ComplexityCovered), Is.EqualTo(1));

            Assert.That(nodeToTest.GetInt(JaCoCo.MethodMissed), Is.EqualTo(4));
            Assert.That(nodeToTest.GetInt(JaCoCo.MethodCovered), Is.EqualTo(1));

            Assert.That(nodeToTest.GetInt(JaCoCo.ClassMissed), Is.EqualTo(0));
            Assert.That(nodeToTest.GetInt(JaCoCo.ClassCovered), Is.EqualTo(1));
        }
    }
}
