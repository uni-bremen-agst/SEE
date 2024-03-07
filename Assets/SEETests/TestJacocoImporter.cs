using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG.IO;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        /// messages. But these are indication that the download and processing as
        /// such work.
        /// </summary>
        [Test]
        public void TestLoadAsyncMethod()
        {
            DataPath path = new()
            {
                Root = DataPath.RootKind.Url,
                Path = "https://raw.githubusercontent.com/vokal/jacoco-parse/master/test/assets/sample.xml"
            };
            UniTask.ToCoroutine(async () =>
            {
                await JaCoCoImporter.LoadAsync(graph, path);
                LogAssert.Expect(LogType.Error, new Regex(@".*No node found for.*"));
            });
        }
    }
}
