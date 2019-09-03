using SEE.DataModel;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SEE.Layout
{
    internal class LCAFinderTest
    {
        private static int failure = 0;
        private static int success = 0;

        public static void Run()
        {
            failure = 0;
            success = 0;
            TestSingle();
            TestSingle();
            TestSimple();
            TestMultiLevel();
            Debug.Log("Test failure rate: " + failure + "/" + (failure + success));
        }

        private static void AssertEquals(Node expected, Node actual,
                                        [CallerFilePath] string path = "",
                                        [CallerLineNumber] int lineNumber = 0,
                                        [CallerMemberName] string caller = null)
        {
            if (!expected.Equals(actual))
            {
                Debug.LogError(caller + " assertion failed (at "
                               + path + ":" + lineNumber + ")\n"
                               + "Expected: " + expected + "\n Actual: " + actual + "\n");
                failure++;
            } 
            else
            {
                success++;
            }
        }

        void TestEmpty()
        {
            Graph empty = new Graph();
            try
            {
                new LCAFinder(empty, (Node)null);
            }
            catch (System.Exception)
            {
                Debug.LogError("Expected NullPointerException() to throw, but it didn't\n");
            }
        }

        private static int nodeID = 0;
        
        private static Node NewVertex(Graph graph, string name = "")
        {
            GameObject gameObject = new GameObject();
            Node node = gameObject.AddComponent<Node>();
            if (string.IsNullOrEmpty(name))
            {
                node.LinkName = nodeID.ToString();
                nodeID++;
            }
            else
            {
                node.LinkName = name;
            }
            graph.AddNode(node);
            return node;
        }

        private static GameObject NewGraph(out Graph graph)
        {
            nodeID = 0;
            GameObject gameObject = new GameObject();
            graph = gameObject.AddComponent<Graph>();
            graph.name = "LCA Graph";
            return gameObject;
        }

        private static void TestSingle()
        {
            GameObject gameObject = NewGraph(out Graph graph);
            Node root = NewVertex(graph);      
            try
            {
                LCAFinder lca = new LCAFinder(graph, root);
                AssertEquals(root, lca.LCA(root, root));
            }
            catch (Exception e)
            {
                Debug.LogError("Unexpected exception: " + e);
                throw e;
            }
            finally
            {
                Destroyer.DestroyGameObject(gameObject);
            }
        }

        private static void TestSimple()
        {
            GameObject gameObject = NewGraph(out Graph graph);
            Node root = NewVertex(graph);
            Node a = NewVertex(graph);
            Node b = NewVertex(graph);
            root.AddChild(a);
            root.AddChild(b);

            try
            {
                LCAFinder lca = new LCAFinder(graph, root);

                AssertEquals(root, lca.LCA(a, b));
                AssertEquals(root, lca.LCA(root, b));
                AssertEquals(root, lca.LCA(b, root));
                AssertEquals(root, lca.LCA(a, root));
                AssertEquals(root, lca.LCA(root, a));
            }
            catch (Exception e)
            {
                Debug.LogError("Unexpected exception: " + e);
                throw e;
            }
            finally
            {
                Destroyer.DestroyGameObject(gameObject);
            }
        }

        private static void TestMultiLevel()
        {
            //        root
            //       / |   \
            //      a  b    c
            //     /\  |    /\
            //   a1 a2 b1 c1 c2
            //            /\ 
            //          c11 c12
            GameObject gameObject = NewGraph(out Graph graph);
            Node root = NewVertex(graph, "root");
            Node a = NewVertex(graph, "a");
            Node b = NewVertex(graph, "b");
            Node c = NewVertex(graph, "c");
            Node a1 = NewVertex(graph, "a1");
            Node a2 = NewVertex(graph, "a2");
            Node b1 = NewVertex(graph, "b1");
            Node c1 = NewVertex(graph, "c1");
            Node c2 = NewVertex(graph, "c2");
            Node c11 = NewVertex(graph, "c11");
            Node c12 = NewVertex(graph, "c12");

            root.AddChild(a);
            root.AddChild(b);
            root.AddChild(c);

            a.AddChild(a1);
            a.AddChild(a2);

            b.AddChild(b1);

            c.AddChild(c1);
            c.AddChild(c2);
            c1.AddChild(c11);
            c1.AddChild(c12);

            graph.DumpTree();

            try
            {
                LCAFinder lca = new LCAFinder(graph, root);

                AssertEquals(a, lca.LCA(a1, a2));
                AssertEquals(root, lca.LCA(a2, b1));
                AssertEquals(root, lca.LCA(b1, c12));
                AssertEquals(c1, lca.LCA(c11, c12));
                AssertEquals(c, lca.LCA(c2, c12));
                AssertEquals(c, lca.LCA(c1, c));
            }
            catch (Exception e)
            {
                Debug.LogError("Unexpected exception: " + e);
                throw e;
            }
            finally
            {
                Destroyer.DestroyGameObject(gameObject);
            }
        }
    }
}

