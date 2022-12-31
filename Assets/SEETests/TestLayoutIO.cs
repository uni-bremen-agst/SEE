using NUnit.Framework;
using SEE.DataModel;
using SEE.Layout.NodeLayouts;
using System.Collections.Generic;
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
            string filename = Application.dataPath + "/../Temp/layout.gvl";

            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

            CalculateLayout(gameObjects,
                            out Dictionary<ILayoutNode, NodeTransform> savedLayout,
                            out Dictionary<string, NodeTransform> layoutMap);

            // Save the layout.
            IO.GVLWriter.Save(filename, "architecture", gameObjects);
            Dump(gameObjects, 10);

            ClearLayout(gameObjects, yIsStored);

            // Read the saved layout.
            Dictionary<ILayoutNode, NodeTransform> readLayout = new LoadedNodeLayout(0, filename).Layout(gameObjects);
            Dump(readLayout, 10);

            Assert.AreEqual(savedLayout.Count, readLayout.Count); // no gameObject added or removed
            // Now layoutMap and readLayout should be the same except for
            // scale.y and, thus, position.y (none of those are stored in GVL).
            LayoutsAreEqual(readLayout, layoutMap, yIsStored);
        }

        /// <summary>
        /// Test for reading and writing a node layout in SLD format.
        /// </summary>
        //[Test]
        //public void TestSLDWriteRead()
        //{
        //    // SLD contains the height (y co-ordinate).
        //    bool yIsStored = true;
        //    string filename = Application.dataPath + "/../Temp/layout.sld";

        //    ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

        //    CalculateLayout(gameObjects,
        //                    out Dictionary<ILayoutNode, NodeTransform> savedLayout,
        //                    out Dictionary<string, NodeTransform> layoutMap);

        //    // Save the layout.
        //    SEE.Layout.IO.SLDWriter.Save(filename, gameObjects);
        //    Dump(gameObjects, 10);

        //    ClearLayout(gameObjects, yIsStored);

        //    // Read the saved layout.
        //    Dictionary<ILayoutNode, NodeTransform> readLayout = new LoadedNodeLayout(0, filename).Layout(gameObjects);
        //    Dump(readLayout, 10);

        //    Assert.AreEqual(savedLayout.Count, readLayout.Count); // no gameObject added or removed
        //    // Now layoutMap and readLayout should be the same except for
        //    // scale.y and, thus, position.y (none of those are stored in GVL).
        //    LayoutsAreEqual(readLayout, layoutMap, yIsStored);
        //}

        /// <summary>
        /// Clears the scale and position of the position of all <paramref name="gameObjects"/>
        /// so that we can be sure that they are actually read. If <paramref name="yIsStored"/>
        /// scale and position are reset completely; otherwise only the x and z components
        /// of those two vectors are reset.
        /// Note that the GVL does not contain scale.y and position.y, that is why we need
        /// to maintain it.
        /// </summary>
        /// <param name="gameObjects">game objects whose layout is to be reset</param>
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
        /// Checks whether the two layouts <paramref name="layoutMap"/> and <paramref name="readLayout"/>
        /// are the same. If <paramref name="compareY"/> is false, the y scale and y position are ignored
        /// in the comparison.
        /// </summary>
        /// <param name="readLayout">the layout read from disk</param>
        /// <param name="layoutMap">the layout calculated originally</param>
        /// <param name="compareY">whether the y scale and y position should be compared</param>
        private static void LayoutsAreEqual(Dictionary<ILayoutNode, NodeTransform> readLayout,
                                            Dictionary<string, NodeTransform> layoutMap,
                                            bool compareY)
        {
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in readLayout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform readTransform = entry.Value;

                Debug.LogFormat("Comparing {0}\n", node.ID);
                NodeTransform savedTransform = layoutMap[node.ID];
                Assert.That(readTransform.scale.x, Is.EqualTo(savedTransform.scale.x).Within(floatTolerance));
                if (compareY)
                {
                    Assert.That(readTransform.scale.y, Is.EqualTo(savedTransform.scale.y).Within(floatTolerance));
                }
                Assert.That(readTransform.scale.z, Is.EqualTo(savedTransform.scale.z).Within(floatTolerance));
                Assert.That(readTransform.position.x, Is.EqualTo(savedTransform.position.x).Within(floatTolerance));
                if (compareY)
                {
                    Assert.That(readTransform.position.y, Is.EqualTo(savedTransform.position.y).Within(floatTolerance));
                }
                Assert.That(readTransform.position.z, Is.EqualTo(savedTransform.position.z).Within(floatTolerance));
                Assert.AreEqual(savedTransform.rotation, readTransform.rotation);
            }
        }

        private static void CalculateLayout
            (ICollection<ILayoutNode> gameObjects,
            out Dictionary<ILayoutNode, NodeTransform> savedLayout,
            out Dictionary<string, NodeTransform> layoutMap)
        {
            // Layout the nodes.
            RectanglePackingNodeLayout packer = new RectanglePackingNodeLayout(0.0f, 1.0f);
            savedLayout = packer.Layout(gameObjects);

            // Apply the layout.
            layoutMap = new Dictionary<string, NodeTransform>(savedLayout.Count);
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in savedLayout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;
                node.LocalScale = transform.scale;
                Vector3 position = transform.position;
                // from ground to center position along the y axis
                position.y += transform.scale.y / 2.0f;
                node.CenterPosition = position;
                layoutMap[node.ID] = new NodeTransform(node.CenterPosition, node.LocalScale, node.Rotation);
            }
        }

        private void Dump(Dictionary<ILayoutNode, NodeTransform> readLayout, int howMany = int.MaxValue)
        {
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in readLayout)
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;

                Debug.LogFormat("{0} => {1}\n", node.ID, transform);
            }
        }

        private void Dump(ICollection<ILayoutNode> gameObjects, int howMany = int.MaxValue)
        {
            foreach (ILayoutNode layoutNode in gameObjects)
            {
                howMany--;
                if (howMany <= 0)
                {
                    break;
                }
                Debug.LogFormat("{0} => position={1} worldScale={2}\n", layoutNode.ID, layoutNode.CenterPosition, layoutNode.AbsoluteScale);
            }
        }

        [Test]
        public void TestWriteReadEmpty()
        {
            string graphName = "architecture";
            string filename = Application.dataPath + "/../Temp/emptylayout.gvl";

            ICollection<ILayoutNode> gameObjects = new List<ILayoutNode>();

            // Save the layout.
            IO.GVLWriter.Save(filename, graphName, gameObjects);

            // Read the saved layout.
            LoadedNodeLayout loadedNodeLayout = new LoadedNodeLayout(0, filename);
            Dictionary<ILayoutNode, NodeTransform> readLayout = loadedNodeLayout.Layout(gameObjects);
            Assert.AreEqual(0, readLayout.Count);
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
