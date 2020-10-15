using NUnit.Framework;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Test cases for SEE.Layout.IO.Reader and SEE.Layout.IO.Writer.
    /// </summary>
    public class TestLayoutIO
    {
        private static Graph LoadGraph(string filename)
        {
            GraphReader graphReader = new GraphReader(filename, new HashSet<string>() { hierarchicalEdgeType, "Belongs_To" });
            graphReader.Load();
            return graphReader.GetGraph();
        }

        /// <summary>
        /// The name of the hierarchical edge type we use for emitting the parent-child
        /// relation among nodes.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";
        private const float floatTolerance = 0.1f;

        [Test]
        public void TestWriteRead()
        {
            string graphName = "architecture";
            string filename = Application.dataPath + "/../Temp/layout.gvl";

            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

            // Layout the nodes.
            RectanglePackingNodeLayout packer = new RectanglePackingNodeLayout(0.0f, 1.0f);
            Dictionary<ILayoutNode, NodeTransform> savedLayout = packer.Layout(gameObjects);

            // Apply the layout.
            Dictionary<string, NodeTransform> layoutMap = new Dictionary<string, NodeTransform>(savedLayout.Count);
            foreach (var entry in savedLayout)
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
            // Save the layout.
            SEE.Layout.IO.Writer.Save(filename, graphName, gameObjects);
            Dump(gameObjects, 10);

            // Clear the scale.x, scale.z and position of all gameObjects so that we can be sure
            // that they are actually read. Note that the GVL does not contain scale.y,
            // that is why we need to maintain it.
            foreach (ILayoutNode layoutNode in gameObjects)
            {
                layoutNode.LocalScale = new Vector3(0.0f, layoutNode.LocalScale.y, 0.0f);
                layoutNode.CenterPosition = Vector3.zero;
            }

            // Read the saved layout. 
            Dictionary<ILayoutNode, NodeTransform> readLayout = new LoadedNodeLayout(0, filename).Layout(gameObjects);

            Dump(readLayout, 10);

            // Now savedLayout and readLayout should be the same except for  
            // scale.y and, thus, position.y (none of those are stored in GVL).
            Assert.AreEqual(savedLayout.Count, readLayout.Count); // no gameObject added or removed
            foreach (var entry in readLayout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform readTransform = entry.Value;

                Debug.LogFormat("Comparing {0}\n", node.ID);
                NodeTransform savedTransform = layoutMap[node.ID];
                Assert.That(readTransform.scale.x, Is.EqualTo(savedTransform.scale.x).Within(floatTolerance));
                Assert.That(readTransform.scale.z, Is.EqualTo(savedTransform.scale.z).Within(floatTolerance));
                Assert.That(readTransform.position.x, Is.EqualTo(savedTransform.position.x).Within(floatTolerance));
                Assert.That(readTransform.position.z, Is.EqualTo(savedTransform.position.z).Within(floatTolerance));
                Assert.AreEqual(savedTransform.rotation, readTransform.rotation);
            }
        }

        private void Dump(Dictionary<ILayoutNode, NodeTransform> readLayout, int howMany = int.MaxValue)
        {
            foreach (var entry in readLayout)
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
            SEE.Layout.IO.Writer.Save(filename, graphName, gameObjects);

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
            // The path to the GXL and GVL files.
            string path = Application.dataPath + "/../Data/GXL/SEE/";

            // Loading the underlying graph.
            Graph graph = LoadGraph(path + "Architecture.gxl");
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
            seeCityComponent.gvlPath = path + "Architecture.gvl";

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
            SEE.Layout.IO.Writer.Save(path + "Architecture-saved.gvl", "architecture", layoutNodes);
        }

        private ICollection<GameObject> GetGameObjects(GameObject go)
        {
            List<GameObject> result = new List<GameObject>();
            if (go.tag == Tags.Node)
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
