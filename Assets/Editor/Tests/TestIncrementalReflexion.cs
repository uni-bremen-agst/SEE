using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel 
{
    /// <summary>
    /// Tests for the incremental reflexion analysis.
    /// 
    /// These test cases follows the scenarios described in the paper
    /// "Incremental Reflexion Analysis", Rainer Koschke, Journal on Software Maintenance
    /// and Evolution, 2011, DOI 10.1002 / smr.542 in Figure 8.
    /// </summary>
    class TestIncrementalReflexion : TestReflexionAnalysis
    {
        /// <summary>
        /// The implementation nodes in the implementation graph: i[j] where 1 <= j <= 17.
        /// 
        /// Note: i[0] does not exist.
        /// </summary>
        Dictionary<int, Node> i;

        /// <summary>
        /// The architecture nodes in the architecture graph: a[j] where 1 <= j <= 8.
        /// 
        /// Note: a[0] does not exist.
        /// </summary>
        Dictionary<int, Node> a;

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
            impl = NewImplementation();
            arch = NewArchitecture();
            mapping = new Graph();
            reflexion = new Reflexion(impl, arch, mapping);
            reflexion.Register(this);
            // An initial run is necessary to set up the necessary data structures.
            reflexion.Run();
        }

        [TearDown]
        protected override void Teardown()
        {
            base.Teardown();
            i = null;
            a = null;
        }

        private static void AddToGraph(Graph graph, Edge edge, Node from, Node to)
        {
            edge.Source = from;
            edge.Target = to;
            graph.AddEdge(edge);
        }

        private Graph NewArchitecture()
        {
            Graph arch = new Graph();
            a = new Dictionary<int, Node>();
            for (int j = 1; j <= 8; j++)
            {
                a[j] = NewNode(arch, "a" + j, "Component");
            }
            a[7].AddChild(a[6]);
            a[7].AddChild(a[5]);

            a[8].AddChild(a[1]);
            a[8].AddChild(a[2]);

            Dictionary<int, Edge> s = new Dictionary<int, Edge>();
            for (int j = 1; j <= 4; j++)
            {
                s[j] = new Edge();
            }
            s[1].Type = "Source_Dependency";
            s[2].Type = call;
            s[3].Type = call;
            s[4].Type = "Source_Dependency";

            AddToGraph(arch, s[1], a[3], a[7]);
            AddToGraph(arch, s[2], a[1], a[3]);
            AddToGraph(arch, s[3], a[8], a[8]);
            AddToGraph(arch, s[4], a[2], a[4]);

            return arch;
        }

        private Graph NewImplementation()
        {
            Graph impl = new Graph();
            i = new Dictionary<int, Node>();
            for (int j = 1; j <= 17; j++)
            {
                i[j] = NewNode(impl, "i" + j);
            }
            i[1].AddChild(i[2]);
            i[1].AddChild(i[11]);

            i[2].AddChild(i[3]);
            i[2].AddChild(i[7]);

            i[3].AddChild(i[4]);
            i[3].AddChild(i[5]);
            i[3].AddChild(i[6]);

            i[7].AddChild(i[8]);
            i[7].AddChild(i[9]);
            i[7].AddChild(i[10]);

            i[11].AddChild(i[12]);
            i[11].AddChild(i[13]);

            Dictionary<int, Edge> e = new Dictionary<int, Edge>();
            for (int j = 1; j <= 9; j++)
            {
                Edge edge = new Edge();
                edge.Type = call;
                e[j] = edge;
            }
            e[1].Type = "import";
            e[2].Type = "set";
            e[3].Type = "use";

            AddToGraph(impl, e[1], i[ 3], i[15]);
            AddToGraph(impl, e[2], i[ 4], i[16]);
            AddToGraph(impl, e[3], i[ 5], i[17]);
            AddToGraph(impl, e[4], i[ 8], i[ 6]);
            AddToGraph(impl, e[5], i[ 9], i[ 8]);
            AddToGraph(impl, e[6], i[ 9], i[10]);
            AddToGraph(impl, e[7], i[12], i[10]);
            AddToGraph(impl, e[8], i[12], i[ 9]);
            AddToGraph(impl, e[9], i[14], i[13]);

            return impl;
        }

        //--------------------
        // Incremental mapping 
        //--------------------

        [Test]
        public void TestIncrementalMapping()
        {
            reflexion.Add_To_Mapping(i[17], a[6]);
            DumpEvents();
        }

    }
}
