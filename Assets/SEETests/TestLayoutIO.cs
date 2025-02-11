using NUnit.Framework;
using SEE.Game;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Test cases for SEE.Layout.IO.Reader and SEE.Layout.IO.Writer.
    /// </summary>
    public class TestLayoutIO
    {
        /// <summary>
        /// By how much two floats may differ to be considered still equal.
        /// </summary>
        private const float floatTolerance = 0.1f;

        /// <summary>
        /// Test for reading and writing a node layout in GVL.
        /// </summary>
        [Test]
        public void TestGVLWriteRead()
        {
            // GVL does not contain the height (y co-ordinate).
            bool yIsStored = false;
            string filename = Path.GetTempFileName() + Filenames.GVLExtension;

            try
            {
                ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

                CalculateLayout(gameObjects,
                                out Dictionary<ILayoutNode, NodeTransform> savedLayout,
                                out Dictionary<string, NodeTransform> layoutMap);

                // Save the layout.
                IO.GVLWriter.Save(filename, "architecture", gameObjects);

                ClearLayout(gameObjects, yIsStored);

                // Read the saved layout.
                Dictionary<ILayoutNode, NodeTransform> readLayout = new LoadedNodeLayout(filename).Create(gameObjects, Vector3.zero, Vector2.one);
                //Dump(readLayout, 10);

                Assert.AreEqual(savedLayout.Count, readLayout.Count); // no gameObject added or removed
                                                                      // Now layoutMap and readLayout should be the same except for
                                                                      // scale.y and, thus, position.y (none of those are stored in GVL).
                LayoutsAreEqual(readLayout, layoutMap, yIsStored);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for reading and writing a node layout in SLD format.
        /// </summary>
        [Test]
        public void TestSLDWriteRead()
        {
            // SLD contains the height (y co-ordinate).
            bool yIsStored = true;
            string filename = Path.GetTempFileName() + Filenames.SLDExtension;

            try
            {
                ICollection<ILayoutNode> layoutNodes = NodeCreator.CreateNodes(howManyRootNodes: 9, howDeeplyNested: 0);

                CalculateLayout(layoutNodes,
                                out _,
                                out Dictionary<string, NodeTransform> layoutMap);

                //Dump(layoutMap, 10, "Created layout (y relates to the ground)");

                // Save the layout including the y co-ordinate (in SLD relating to the center).
                {
                    ICollection<GameObject> gameObjects = ToGameNodes(layoutMap);
                    IO.SLDWriter.Save(filename, gameObjects);
                    //Dump(gameObjects, 10, "Saved layout");
                }

                ClearLayout(layoutNodes, yIsStored);

                // Read the saved layout.
                Dictionary<ILayoutNode, NodeTransform> readLayout = new LoadedNodeLayout(filename).Create(layoutNodes, Vector3.zero, Vector2.one);
                //Dump(readLayout, 10, "Read layout");

                Assert.AreEqual(layoutMap.Count, readLayout.Count); // no gameObject added or removed
                                                                    // Now layoutMap and readLayout should be the same including
                                                                    // scale.y and, thus, position.y (all of those are stored in GVL).
                LayoutsAreEqual(readLayout, layoutMap, yIsStored);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }

            // Returns a list of new game objects whose position and scale are retrieved from the given layoutNodes.
            static ICollection<GameObject> ToGameNodes(Dictionary<string, NodeTransform> layoutNodes)
            {
                ICollection<GameObject> result = new List<GameObject>(layoutNodes.Count);
                foreach (var layoutNode in layoutNodes)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = layoutNode.Key;
                    go.transform.localScale = layoutNode.Value.Scale;
                    go.transform.position = layoutNode.Value.CenterPosition;
                    result.Add(go);
                }
                return result;
            }
        }

        /// <summary>
        /// Clears the scale and position of the position of all <paramref name="gameObjects"/>
        /// so that we can be sure that they are actually read. If <paramref name="yIsStored"/>
        /// scale and position are reset completely; otherwise only the x and z components
        /// of those two vectors are reset.
        /// Note that the GVL does not contain scale.y and position.y, that is why we need
        /// to maintain it. This can be achieved by <paramref name="yIsStored"/> setting
        /// to false.
        /// </summary>
        /// <param name="gameObjects">game objects whose layout is to be reset</param>
        /// <param name="yIsStored">if true, the height and y position are zeroed, too;
        /// otherwise only the width, depth and x/z co-ordinates</param>
        private static void ClearLayout(ICollection<ILayoutNode> gameObjects, bool yIsStored)
        {
            foreach (ILayoutNode layoutNode in gameObjects)
            {
                if (yIsStored)
                {
                    layoutNode.LocalScale = Vector3.zero;
                    layoutNode.CenterPosition = Vector3.zero;
                }
                else
                {
                    layoutNode.LocalScale = new Vector3(0.0f, layoutNode.LocalScale.y, 0.0f);
                    layoutNode.CenterPosition = new Vector3(0.0f, layoutNode.LocalScale.y, 0.0f);
                }
            }
        }

        /// <summary>
        /// Checks whether the two layouts <paramref name="originalLayout"/> and <paramref name="readLayout"/>
        /// are the same. If <paramref name="compareY"/> is false, the y scale and y position are ignored
        /// in the comparison.
        /// </summary>
        /// <param name="readLayout">the layout read from disk</param>
        /// <param name="originalLayout">the layout calculated originally</param>
        /// <param name="compareY">whether the y scale and y position should be compared</param>
        private static void LayoutsAreEqual(Dictionary<ILayoutNode, NodeTransform> readLayout,
                                            Dictionary<string, NodeTransform> originalLayout,
                                            bool compareY)
        {
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in readLayout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform readTransform = entry.Value;

                //Debug.Log($"Comparing node with ID {node.ID}\n");
                NodeTransform savedTransform = originalLayout[node.ID];
                Assert.That(readTransform.Scale.x, Is.EqualTo(savedTransform.Scale.x).Within(floatTolerance));
                if (compareY)
                {
                    Assert.That(readTransform.Scale.y, Is.EqualTo(savedTransform.Scale.y).Within(floatTolerance));
                }
                Assert.That(readTransform.Scale.z, Is.EqualTo(savedTransform.Scale.z).Within(floatTolerance));
                Assert.That(readTransform.X, Is.EqualTo(savedTransform.X).Within(floatTolerance));
                if (compareY)
                {
                    Assert.That(readTransform.CenterPosition.y, Is.EqualTo(savedTransform.CenterPosition.y).Within(floatTolerance));
                }
                Assert.That(readTransform.Z, Is.EqualTo(savedTransform.Z).Within(floatTolerance));
                Assert.AreEqual(savedTransform.Rotation, readTransform.Rotation);
            }
        }

        /// <summary>
        /// Layouts the <paramref name="gameObjects"/> (which layout is implementation
        /// defined, currently <see cref="RectanglePackingNodeLayout"/> is used).
        /// </summary>
        /// <param name="gameObjects">the nodes to be laid out</param>
        /// <param name="savedLayout">the resulting node layout</param>
        /// <param name="layoutMap">the applied layout as a mapping of a node's ID onto the
        /// <see cref="NodeTransform"/> resulting from the layout</param>
        private static void CalculateLayout
            (ICollection<ILayoutNode> gameObjects,
            out Dictionary<ILayoutNode, NodeTransform> savedLayout,
            out Dictionary<string, NodeTransform> layoutMap)
        {
            // Layout the nodes.
            savedLayout = new RectanglePackingNodeLayout().Create(gameObjects, Vector3.zero, Vector2.one);

            // Apply the layout.
            layoutMap = new Dictionary<string, NodeTransform>(savedLayout.Count);
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in savedLayout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;
                node.LocalScale = transform.Scale;
                node.CenterPosition = transform.CenterPosition;
                layoutMap[node.ID] = new NodeTransform(node.CenterPosition, node.LocalScale, node.Rotation);
            }
        }

        private void Dump(ICollection<GameObject> gameObjects, int howMany = int.MaxValue, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{message}\n");
            }
            foreach (GameObject gameObject in gameObjects.OrderBy(go => go.name))
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                Debug.Log($"{gameObject.name} => position={gameObject.transform.position} worldScale={gameObject.transform.lossyScale}\n");
            }
        }

        private void Dump(Dictionary<string, NodeTransform> layoutMap, int howMany = int.MaxValue, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{message}\n");
            }
            foreach (KeyValuePair<string, NodeTransform> entry in layoutMap.OrderBy(key => key.Key))
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                NodeTransform transform = entry.Value;

                Debug.Log($"{entry.Key} => {transform}\n");
            }
        }

        private void Dump(Dictionary<ILayoutNode, NodeTransform> readLayout, int howMany = int.MaxValue, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{message}\n");
            }
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in readLayout)
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;

                Debug.Log($"{node.ID} => {transform}\n");
            }
        }

        private void Dump(ICollection<ILayoutNode> gameObjects, int howMany = int.MaxValue, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{message}\n");
            }
            foreach (ILayoutNode layoutNode in gameObjects)
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                Debug.Log($"{layoutNode.ID} => position={layoutNode.CenterPosition} worldScale={layoutNode.AbsoluteScale}\n");
            }
        }

        [Test]
        public void TestWriteReadEmptyGVL()
        {
            TestWriteReadLayout(Filenames.GVLExtension);
        }

        [Test]
        public void TestWriteReadEmptySLD()
        {
            TestWriteReadLayout(Filenames.SLDExtension);
        }

        /// <summary>
        /// Writes a layout in the format depending upon <paramref name="fileExtension"/> and
        /// reads it again.
        /// </summary>
        /// <param name="fileExtension">the file extension for a layout format file</param>
        private static void TestWriteReadLayout(string fileExtension)
        {
            string graphName = "architecture";
            string filename = Path.GetTempFileName() + fileExtension;

            try
            {
                // Save the layout.
                if (fileExtension == Filenames.GVLExtension)
                {
                    IO.GVLWriter.Save(filename, graphName, new List<ILayoutNode>());
                }
                else if (fileExtension == Filenames.SLDExtension)
                {
                    IO.SLDWriter.Save(filename, new List<GameObject>());
                }
                else
                {
                    Assert.Fail("Untested layout format");
                }
                // Read the saved layout.
                LoadedNodeLayout loadedNodeLayout = new(filename);
                Dictionary<ILayoutNode, NodeTransform> readLayout = loadedNodeLayout.Create(new List<ILayoutNode>(), Vector3.zero, Vector2.one);
                Assert.AreEqual(0, readLayout.Count);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Tests whether a file can be read that was generated by Gravis.
        /// </summary>
        [Test]
        public void TestRead()
        {
            // FIXME reintroduce tests
#if false
            // The relative path to the GXL and GVL files.

            // Loading the underlying graph.
            Graph graph = LoadGraph(Filenames.OnCurrentPlatform("Data/GXL/SEE/") + "Architecture.gxl");
            //graph.DumpTree();

            // Setting up the node layout so that it is read from the GVL file.
            GameObject seeCity = new GameObject();
            seeCity.name = "SEECity";
            seeCity.transform.position = Vector3.zero; // new Vector3(-1012.38f, 0.0f, 581.414f);
            seeCity.transform.localScale = Vector3.one * 100 * 18.91f;
            SEECity seeCityComponent = seeCity.AddComponent<SEECity>();
            seeCityComponent.NodeLayout = NodeLayoutKind.FromFile;
            seeCityComponent.EdgeLayout = EdgeLayoutKind.None;
            seeCityComponent.LeafObjects = AbstractSEECity.LeafNodeKinds.Blocks;
            seeCityComponent.InnerNodeObjects = AbstractSEECity.InnerNodeKinds.Blocks;
            seeCityComponent.LayoutPath.Root = DataPath.RootKind.ProjectFolder;
            seeCityComponent.LayoutPath.RelativePath = "/Data/GXL/SEE/Architecture.gvl";

            // Render the city. This will create all game objects as well as their
            // layout. As stated before, the layout does not interest us.
            GraphRenderer graphRenderer = new GraphRenderer(seeCityComponent, graph);
            graphRenderer.Draw(seeCity);

            // Now we have the game objects whose layout information was read
            // from the GVL file.

            // The game-object hierarchy for the nodes in graph are children of seeCity.
            ICollection<GameNode> gameNodes = graphRenderer.ToLayoutNodes(GetGameObjects(seeCity));
            // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
            // (GameNode implements ILayoutNode).
            ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();

            //SEE.Layout.IO.Reader reader = new SEE.Layout.IO.Reader(path + "Architecture.gvl",
            //                                                       gameObjects.Cast<IGameNode>().ToList(),
            //                                                       0.0f);
            DumpTree(layoutNodes);
            // Save the layout.
            SEE.Layout.IO.GVLWriter.Save(Filenames.OnCurrentPlatform("Data/GXL/SEE/") + "Architecture-saved.gvl", "architecture", layoutNodes);
#endif
        }

        private ICollection<GameObject> GetGameObjects(GameObject go)
        {
            List<GameObject> result = new List<GameObject>();
            if (go.CompareTag(Tags.Node))
            {
                result.Add(go);
            }
            foreach (Transform child in go.transform)
            {
                ICollection<GameObject> ascendants = GetGameObjects(child.gameObject);
                result.AddRange(ascendants);
            }
            return result;
        }

        //private ILayoutNode ToTestGameNode(GameObject go)
        //{
        //    NodeRef nodeRef = go.GetComponent<NodeRef>();
        //    return new LayoutGameObject(nodeRef.node.ID);
        //}

        public void DumpTree(ICollection<ILayoutNode> gameObjects)
        {
            foreach (ILayoutNode root in ILayoutNodeHierarchy.Roots(gameObjects))
            {
                DumpTree(root);
            }
        }

        /// <summary>
        /// Dumps the hierarchy for given root. Used for debugging.
        /// </summary>
        internal void DumpTree(ILayoutNode root)
        {
            DumpTree(root, 0);
        }

        /// <summary>
        /// Dumps the hierarchy for given root by adding level many -
        /// as indentation followed by the layout information. Used for debugging.
        /// </summary>
        private void DumpTree(ILayoutNode root, int level)
        {
            string indentation = "";
            for (int i = 0; i < level; i++)
            {
                indentation += "-";
            }
            Debug.LogFormat("{0}{1}: position={2} worldscale={3} rotation={4}.\n", indentation, root.ID, root.CenterPosition, root.AbsoluteScale, root.Rotation);
            foreach (ILayoutNode child in root.Children())
            {
                DumpTree(child, level + 1);
            }
        }
    }
}
