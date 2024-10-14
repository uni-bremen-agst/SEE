using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG.IO;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        /// The graph that was loaded by <see cref="SetUpAsync"/> before each test case is executed.
        /// </summary>
        private Graph graph;

        [SetUp]
        public async Task SetUpAsync()
        {
            DataPath gxlPath = new(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz");
            DataPath xmlPath = new(Application.streamingAssetsPath + "/JLGExample/jacoco.xml");

            graph = await LoadGraphAsync(gxlPath);
            await JaCoCoImporter.LoadAsync(graph, xmlPath);
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

            Assert.AreEqual(1313, nodeToTest.GetInt(JaCoCo.InstructionMissed));
            Assert.AreEqual(441, nodeToTest.GetInt(JaCoCo.InstructionCovered));

            Assert.AreEqual(101, nodeToTest.GetInt(JaCoCo.BranchMissed));
            Assert.AreEqual(27, nodeToTest.GetInt(JaCoCo.BranchCovered));

            Assert.AreEqual(330, nodeToTest.GetInt(JaCoCo.LineMissed));
            Assert.AreEqual(83, nodeToTest.GetInt(JaCoCo.LineCovered));

            Assert.AreEqual(107, nodeToTest.GetInt(JaCoCo.ComplexityMissed));
            Assert.AreEqual(20, nodeToTest.GetInt(JaCoCo.ComplexityCovered));

            Assert.AreEqual(55, nodeToTest.GetInt(JaCoCo.MethodMissed));
            Assert.AreEqual(8, nodeToTest.GetInt(JaCoCo.MethodCovered));

            Assert.AreEqual(6, nodeToTest.GetInt(JaCoCo.ClassMissed));
            Assert.AreEqual(4, nodeToTest.GetInt(JaCoCo.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a class node. In JaCoCo-Test-Report it is named "class".
        /// </summary>
        [Test]
        public void AddMetricToClassNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants");
            Assert.IsNotNull(nodeToTest);

            Assert.AreEqual(7f, nodeToTest.GetInt(JaCoCo.InstructionMissed));
            Assert.AreEqual(130f, nodeToTest.GetInt(JaCoCo.InstructionCovered));

            Assert.AreEqual(0.0f, nodeToTest.GetInt(JaCoCo.BranchMissed));
            Assert.AreEqual(6f, nodeToTest.GetInt(JaCoCo.BranchCovered));

            Assert.AreEqual(3f, nodeToTest.GetInt(JaCoCo.LineMissed));
            Assert.AreEqual(11f, nodeToTest.GetInt(JaCoCo.LineCovered));

            Assert.AreEqual(2f, nodeToTest.GetInt(JaCoCo.ComplexityMissed));
            Assert.AreEqual(5f, nodeToTest.GetInt(JaCoCo.ComplexityCovered));

            Assert.AreEqual(2f, nodeToTest.GetInt(JaCoCo.MethodMissed));
            Assert.AreEqual(2f, nodeToTest.GetInt(JaCoCo.MethodCovered));

            Assert.AreEqual(0f, nodeToTest.GetInt(JaCoCo.ClassMissed));
            Assert.AreEqual(1f, nodeToTest.GetInt(JaCoCo.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a package node. In JaCoCo-Test-Report it is named "package".
        /// </summary>
        [Test]
        public void AddMetricToPackageNode()
        {
            Node nodeToTest = graph.GetNode("counter");
            Assert.IsNotNull(nodeToTest);

            Assert.AreEqual(31, nodeToTest.GetInt(JaCoCo.InstructionMissed));
            Assert.AreEqual(313, nodeToTest.GetInt(JaCoCo.InstructionCovered));

            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.BranchMissed));
            Assert.AreEqual(17, nodeToTest.GetInt(JaCoCo.BranchCovered));

            Assert.AreEqual(13, nodeToTest.GetInt(JaCoCo.LineMissed));
            Assert.AreEqual(45, nodeToTest.GetInt(JaCoCo.LineCovered));

            Assert.AreEqual(9, nodeToTest.GetInt(JaCoCo.ComplexityMissed));
            Assert.AreEqual(14, nodeToTest.GetInt(JaCoCo.ComplexityCovered));

            Assert.AreEqual(8, nodeToTest.GetInt(JaCoCo.MethodMissed));
            Assert.AreEqual(6, nodeToTest.GetInt(JaCoCo.MethodCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.ClassMissed));
            Assert.AreEqual(3, nodeToTest.GetInt(JaCoCo.ClassCovered));
        }

        /// <summary>
        /// Test if metrics are set for a method node. In JaCoCo-Test-Report it is named "method".
        /// </summary>
        [Test]
        public void AddMetricToMethodNode()
        {
            Node nodeToTest = graph.GetNode("counter.CountConsonants.countConsonants(java.lang.String;)");

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.InstructionMissed));
            Assert.AreEqual(39, nodeToTest.GetInt(JaCoCo.InstructionCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.BranchMissed));
            Assert.AreEqual(6, nodeToTest.GetInt(JaCoCo.BranchCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.LineMissed));
            Assert.AreEqual(8, nodeToTest.GetInt(JaCoCo.LineCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.ComplexityMissed));
            Assert.AreEqual(4, nodeToTest.GetInt(JaCoCo.ComplexityCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.MethodMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.MethodCovered));
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

            Assert.AreEqual(30, nodeToTest.GetInt(JaCoCo.InstructionMissed));
            Assert.AreEqual(10, nodeToTest.GetInt(JaCoCo.InstructionCovered));

            Assert.AreEqual(3, nodeToTest.GetInt(JaCoCo.BranchMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.BranchCovered));

            Assert.AreEqual(10, nodeToTest.GetInt(JaCoCo.LineMissed));
            Assert.AreEqual(3, nodeToTest.GetInt(JaCoCo.LineCovered));

            Assert.AreEqual(6, nodeToTest.GetInt(JaCoCo.ComplexityMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.ComplexityCovered));

            Assert.AreEqual(4, nodeToTest.GetInt(JaCoCo.MethodMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.MethodCovered));

            Assert.AreEqual(0, nodeToTest.GetInt(JaCoCo.ClassMissed));
            Assert.AreEqual(1, nodeToTest.GetInt(JaCoCo.ClassCovered));
        }
    }
}
