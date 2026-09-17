using NUnit.Framework;
using SEE.Game.Drawable.ValueHolders;
using System.Reflection;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests the hierarchy and validation behavior of <see cref="GameMindMap"/>.
    /// </summary>
    [TestFixture]
    public class TestGameMindMap
    {
        /// <summary>
        /// The root containing all test nodes.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// Creates the hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("MindMap");
        }

        /// <summary>
        /// Destroys all objects created by the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Verifies that a node cannot become its own parent.
        /// </summary>
        [Test]
        public void TestParentChangeRejectsNodeItself()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);

            Assert.That(GameMindMap.ParentChangeIsValid(node, node), Is.False);
        }

        /// <summary>
        /// Verifies that an unrelated node can become the new parent.
        /// </summary>
        [Test]
        public void TestParentChangeAcceptsUnrelatedNode()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);
            GameObject parent = CreateNode("Parent", GameMindMap.NodeKind.Theme);

            Assert.That(GameMindMap.ParentChangeIsValid(node, parent), Is.True);
        }

        /// <summary>
        /// Verifies that a descendant cannot become the parent of one of its ancestors.
        /// </summary>
        [Test]
        public void TestParentChangeRejectsDescendant()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);
            GameObject child = CreateNode("Child", GameMindMap.NodeKind.Subtheme);
            GameObject branchLine = CreateBranchLine("Branch");

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            Assert.That(GameMindMap.ParentChangeIsValid(node, child), Is.False);
        }

        /// <summary>
        /// Verifies that a subtheme without children can be transformed into a leaf.
        /// </summary>
        [Test]
        public void TestSubthemeWithoutChildrenCanBecomeLeaf()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Leaf, GameMindMap.NodeKind.Subtheme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a subtheme with children cannot be transformed into a leaf.
        /// </summary>
        [Test]
        public void TestSubthemeWithChildrenCannotBecomeLeaf()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);
            GameObject child = CreateNode("Child", GameMindMap.NodeKind.Leaf);
            GameObject branchLine = CreateBranchLine("Branch");

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Leaf, GameMindMap.NodeKind.Subtheme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a subtheme can always be transformed into a theme.
        /// </summary>
        [Test]
        public void TestSubthemeCanBecomeTheme()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Subtheme);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Theme, GameMindMap.NodeKind.Subtheme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a leaf can be transformed into another node kind.
        /// </summary>
        [Test]
        public void TestLeafCanBecomeTheme()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Leaf);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Theme, GameMindMap.NodeKind.Leaf);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Creates and initializes a Mind Map node.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="nodeKind">The kind of the node.</param>
        /// <returns>The created node.</returns>
        private GameObject CreateNode(string name, GameMindMap.NodeKind nodeKind)
        {
            GameObject node = new GameObject(name);
            node.tag = Tags.MindMapNode;
            node.transform.SetParent(root.transform);

            MMNodeValueHolder valueHolder = node.AddComponent<MMNodeValueHolder>();
            InitializeValueHolder(valueHolder);
            valueHolder.NodeKind = nodeKind;

            return node;
        }

        /// <summary>
        /// Creates a branch line owned by the test hierarchy.
        /// </summary>
        /// <param name="name">The name of the branch line.</param>
        /// <returns>The created branch line.</returns>
        private GameObject CreateBranchLine(string name)
        {
            GameObject branchLine = new GameObject(name);
            branchLine.transform.SetParent(root.transform);
            return branchLine;
        }

        /// <summary>
        /// Invokes the Unity initialization of the given Mind Map node value holder.
        /// </summary>
        /// <param name="valueHolder">The value holder that should be initialized.</param>
        private static void InitializeValueHolder(MMNodeValueHolder valueHolder)
        {
            MethodInfo awake = typeof(MMNodeValueHolder).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(awake, Is.Not.Null, "Could not find MMNodeValueHolder.Awake().");
            awake.Invoke(valueHolder, null);
        }
    }
}
