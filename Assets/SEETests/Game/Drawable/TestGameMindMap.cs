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
        /// The root object containing the Mind Map hierarchy used by the tests.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// The container holding the Mind Map nodes used by the tests.
        /// </summary>
        private GameObject attachedObjects;

        /// <summary>
        /// Creates the Mind Map hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("MindMap");

            attachedObjects = new GameObject("AttachedObjects");
            attachedObjects.tag = Tags.AttachedObjects;
            attachedObjects.transform.SetParent(root.transform);
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
        /// Verifies that a theme cannot become a subtheme if no other theme can act
        /// as its new parent.
        /// </summary>
        [Test]
        public void TestThemeCannotBecomeSubthemeWithoutAnotherTheme()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Theme);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Subtheme, GameMindMap.NodeKind.Theme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a theme can become a subtheme if another theme can act as
        /// its new parent.
        /// </summary>
        [Test]
        public void TestThemeCanBecomeSubthemeWithAnotherTheme()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Theme);
            CreateNode("OtherTheme", GameMindMap.NodeKind.Theme);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Subtheme, GameMindMap.NodeKind.Theme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a theme without children can become a leaf if another theme
        /// can act as its new parent.
        /// </summary>
        [Test]
        public void TestThemeWithoutChildrenCanBecomeLeafWithAnotherTheme()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Theme);
            CreateNode("OtherTheme", GameMindMap.NodeKind.Theme);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Leaf, GameMindMap.NodeKind.Theme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a theme with children cannot become a leaf even if another
        /// theme is available as a parent.
        /// </summary>
        [Test]
        public void TestThemeWithChildrenCannotBecomeLeaf()
        {
            GameObject node = CreateNode("Node", GameMindMap.NodeKind.Theme);
            GameObject child = CreateNode("Child", GameMindMap.NodeKind.Subtheme);
            GameObject branchLine = CreateBranchLine("Branch");
            CreateNode("OtherTheme", GameMindMap.NodeKind.Theme);

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            bool result = GameMindMap.CheckValidNodeKindChange(
                node, GameMindMap.NodeKind.Leaf, GameMindMap.NodeKind.Theme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Creates and initializes a Mind Map node with the given name and node kind.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="nodeKind">The kind of the node.</param>
        /// <returns>The created Mind Map node.</returns>
        private GameObject CreateNode(string name, GameMindMap.NodeKind nodeKind)
        {
            GameObject node = new GameObject(name);
            node.tag = Tags.MindMapNode;
            node.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder valueHolder = node.AddComponent<MMNodeValueHolder>();
            InitializeValueHolder(valueHolder);
            valueHolder.NodeKind = nodeKind;

            return node;
        }

        /// <summary>
        /// Creates a branch line owned by the test Mind Map hierarchy.
        /// </summary>
        /// <param name="name">The name of the branch line.</param>
        /// <returns>The created branch line.</returns>
        private GameObject CreateBranchLine(string name)
        {
            GameObject branchLine = new GameObject(name);
            branchLine.transform.SetParent(attachedObjects.transform);
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
