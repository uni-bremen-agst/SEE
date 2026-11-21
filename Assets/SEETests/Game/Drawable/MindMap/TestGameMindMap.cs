using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
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
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);

            Assert.That(GameMindMapHierarchy.ParentChangeIsValid(node, node), Is.False);
        }

        /// <summary>
        /// Verifies that an unrelated node can become the new parent.
        /// </summary>
        [Test]
        public void TestParentChangeAcceptsUnrelatedNode()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);
            GameObject parent = CreateNode("Parent", MindMapNodeKind.Theme);

            Assert.That(GameMindMapHierarchy.ParentChangeIsValid(node, parent), Is.True);
        }

        /// <summary>
        /// Verifies that a descendant cannot become the parent of one of its ancestors.
        /// </summary>
        [Test]
        public void TestParentChangeRejectsDescendant()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);
            GameObject child = CreateNode("Child", MindMapNodeKind.Subtheme);
            GameObject branchLine = CreateBranchLine("Branch");

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            Assert.That(GameMindMapHierarchy.ParentChangeIsValid(node, child), Is.False);
        }

        /// <summary>
        /// Verifies that a subtheme without children can be transformed into a leaf.
        /// </summary>
        [Test]
        public void TestSubthemeWithoutChildrenCanBecomeLeaf()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Leaf, MindMapNodeKind.Subtheme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a subtheme with children cannot be transformed into a leaf.
        /// </summary>
        [Test]
        public void TestSubthemeWithChildrenCannotBecomeLeaf()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);
            GameObject child = CreateNode("Child", MindMapNodeKind.Leaf);
            GameObject branchLine = CreateBranchLine("Branch");

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Leaf, MindMapNodeKind.Subtheme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a subtheme can always be transformed into a theme.
        /// </summary>
        [Test]
        public void TestSubthemeCanBecomeTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Subtheme);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Theme, MindMapNodeKind.Subtheme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a leaf can be transformed into another node kind.
        /// </summary>
        [Test]
        public void TestLeafCanBecomeTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Leaf);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Theme, MindMapNodeKind.Leaf);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a theme cannot become a subtheme if no other theme can act
        /// as its new parent.
        /// </summary>
        [Test]
        public void TestThemeCannotBecomeSubthemeWithoutAnotherTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Theme);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Subtheme, MindMapNodeKind.Theme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a theme can become a subtheme if another theme can act as
        /// its new parent.
        /// </summary>
        [Test]
        public void TestThemeCanBecomeSubthemeWithAnotherTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Theme);
            CreateNode("OtherTheme", MindMapNodeKind.Theme);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Subtheme, MindMapNodeKind.Theme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that an inactive theme cannot make a Theme-to-Subtheme
        /// transition valid because it cannot be selected as the required parent.
        /// </summary>
        [Test]
        public void TestThemeCannotBecomeSubthemeWithOnlyInactiveTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Theme);

            GameObject otherTheme = CreateNode("OtherTheme", MindMapNodeKind.Theme);

            otherTheme.SetActive(false);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node,
                MindMapNodeKind.Subtheme,
                MindMapNodeKind.Theme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a theme without children can become a leaf if another theme
        /// can act as its new parent.
        /// </summary>
        [Test]
        public void TestThemeWithoutChildrenCanBecomeLeafWithAnotherTheme()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Theme);
            CreateNode("OtherTheme", MindMapNodeKind.Theme);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Leaf, MindMapNodeKind.Theme);

            Assert.That(result, Is.True);
        }

        /// <summary>
        /// Verifies that a theme with children cannot become a leaf even if another
        /// theme is available as a parent.
        /// </summary>
        [Test]
        public void TestThemeWithChildrenCannotBecomeLeaf()
        {
            GameObject node = CreateNode("Node", MindMapNodeKind.Theme);
            GameObject child = CreateNode("Child", MindMapNodeKind.Subtheme);
            GameObject branchLine = CreateBranchLine("Branch");
            CreateNode("OtherTheme", MindMapNodeKind.Theme);

            node.GetComponent<MMNodeValueHolder>().AddChild(child, branchLine);

            bool result = GameMindMapHierarchy.CheckValidNodeKindChange(
                node, MindMapNodeKind.Leaf, MindMapNodeKind.Theme);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that renaming only an imported child preserves
        /// its parent reference and updates all associated branch lines
        /// and stored child references.
        /// </summary>
        [Test]
        public void TestRenameMindMapWithOnlyChildCollision()
        {
            string parentName = "Theme-ParentA";
            string childName = "Subtheme-ChildB";

            CreateNode(childName, MindMapNodeKind.Subtheme);

            MindMapNodeConf parent =
                CreateImportedNode(parentName, MindMapNodeKind.Theme, "");

            MindMapNodeConf child =
                CreateImportedNode(
                    childName,
                    MindMapNodeKind.Subtheme,
                    parentName);

            string oldBranchName = child.BranchLineToParent;

            GetStoredChildNames(parent).Add(childName, oldBranchName);

            DrawableConfig config = new();
            config.MindMapNodeConfigs.Add(parent);
            config.MindMapNodeConfigs.Add(child);
            config.LineConfigs.Add(new LineConf { ID = oldBranchName });

            Assert.DoesNotThrow(
                () => GameMindMap.RenameMindMap(config, attachedObjects));

            Assert.That(parent.ID, Is.EqualTo(parentName));
            Assert.That(child.ID, Is.Not.EqualTo(childName));
            Assert.That(child.ID, Does.StartWith("Subtheme-"));
            Assert.That(child.ParentNode, Is.EqualTo(parentName));

            string expectedBranchName =
                ValueHolder.MindMapBranchLine + "-"
                + GameMindMap.GetIDofName(parent.ID) + "-"
                + GameMindMap.GetIDofName(child.ID);

            Assert.That(child.BranchLineToParent, Is.EqualTo(expectedBranchName));
            Assert.That(child.BranchLineConf.ID, Is.EqualTo(expectedBranchName));
            Assert.That(config.LineConfigs[0].ID, Is.EqualTo(expectedBranchName));

            Dictionary<string, string> children = GetStoredChildNames(parent);

            Assert.That(children.ContainsKey(childName), Is.False);
            Assert.That(children.ContainsKey(child.ID), Is.True);
            Assert.That(children[child.ID], Is.EqualTo(expectedBranchName));
        }

        /// <summary>
        /// Verifies that a renamed parent is correctly referenced by an
        /// unchanged child, even when the child occurs first in the
        /// imported configuration.
        /// </summary>
        [Test]
        public void TestRenameMindMapWithOnlyParentCollision()
        {
            string parentName = "Theme-ParentA";
            string childName = "Subtheme-ChildB";

            CreateNode(parentName, MindMapNodeKind.Theme);

            MindMapNodeConf parent =
                CreateImportedNode(parentName, MindMapNodeKind.Theme, "");

            MindMapNodeConf child =
                CreateImportedNode(
                    childName,
                    MindMapNodeKind.Subtheme,
                    parentName);

            string oldBranchName = child.BranchLineToParent;

            GetStoredChildNames(parent).Add(childName, oldBranchName);

            DrawableConfig config = new();

            // Deliberately process the child before its parent.
            config.MindMapNodeConfigs.Add(child);
            config.MindMapNodeConfigs.Add(parent);
            config.LineConfigs.Add(new LineConf { ID = oldBranchName });

            Assert.DoesNotThrow(
                () => GameMindMap.RenameMindMap(config, attachedObjects));

            Assert.That(parent.ID, Is.Not.EqualTo(parentName));
            Assert.That(parent.ID, Does.StartWith("Theme-"));

            Assert.That(child.ID, Is.EqualTo(childName));
            Assert.That(child.ParentNode, Is.EqualTo(parent.ID));

            string expectedBranchName =
                ValueHolder.MindMapBranchLine + "-"
                + GameMindMap.GetIDofName(parent.ID) + "-"
                + GameMindMap.GetIDofName(child.ID);

            Assert.That(child.BranchLineToParent, Is.EqualTo(expectedBranchName));
            Assert.That(child.BranchLineConf.ID, Is.EqualTo(expectedBranchName));
            Assert.That(config.LineConfigs[0].ID, Is.EqualTo(expectedBranchName));

            Assert.That(
                GetStoredChildNames(parent)[childName],
                Is.EqualTo(expectedBranchName));
        }

        /// <summary>
        /// Verifies that renaming both an imported parent and its child
        /// preserves their relationship and all associated branch names.
        /// </summary>
        [Test]
        public void TestRenameMindMapWithParentAndChildCollision()
        {
            string parentName = "Theme-ParentA";
            string childName = "Subtheme-ChildB";

            CreateNode(parentName, MindMapNodeKind.Theme);
            CreateNode(childName, MindMapNodeKind.Subtheme);

            MindMapNodeConf parent =
                CreateImportedNode(parentName, MindMapNodeKind.Theme, "");

            MindMapNodeConf child =
                CreateImportedNode(
                    childName,
                    MindMapNodeKind.Subtheme,
                    parentName);

            string oldBranchName = child.BranchLineToParent;

            GetStoredChildNames(parent).Add(childName, oldBranchName);

            DrawableConfig config = new();

            config.MindMapNodeConfigs.Add(parent);
            config.MindMapNodeConfigs.Add(child);
            config.LineConfigs.Add(new LineConf { ID = oldBranchName });

            Assert.DoesNotThrow(
                () => GameMindMap.RenameMindMap(config, attachedObjects));

            Assert.That(parent.ID, Is.Not.EqualTo(parentName));
            Assert.That(child.ID, Is.Not.EqualTo(childName));
            Assert.That(child.ParentNode, Is.EqualTo(parent.ID));

            string expectedBranchName =
                ValueHolder.MindMapBranchLine + "-"
                + GameMindMap.GetIDofName(parent.ID) + "-"
                + GameMindMap.GetIDofName(child.ID);

            Assert.That(child.BranchLineToParent, Is.EqualTo(expectedBranchName));
            Assert.That(child.BranchLineConf.ID, Is.EqualTo(expectedBranchName));
            Assert.That(config.LineConfigs[0].ID, Is.EqualTo(expectedBranchName));

            Assert.That(
                parent.BorderConf.ID,
                Is.EqualTo(ValueHolder.LinePrefix + "-"
                           + GameMindMap.GetIDofName(parent.ID)));

            Assert.That(
                child.TextConf.ID,
                Is.EqualTo(ValueHolder.TextPrefix + "-"
                           + GameMindMap.GetIDofName(child.ID)));

            Dictionary<string, string> children = GetStoredChildNames(parent);

            Assert.That(children.ContainsKey(childName), Is.False);
            Assert.That(children[child.ID], Is.EqualTo(expectedBranchName));
        }

        /// <summary>
        /// Verifies that importing Mind Map nodes without collisions
        /// preserves their names and existing parent-child references.
        /// </summary>
        [Test]
        public void TestRenameMindMapWithoutCollisions()
        {
            string parentName = "Theme-ParentA";
            string childName = "Subtheme-ChildB";

            MindMapNodeConf parent =
                CreateImportedNode(parentName, MindMapNodeKind.Theme, "");

            MindMapNodeConf child =
                CreateImportedNode(
                    childName,
                    MindMapNodeKind.Subtheme,
                    parentName);

            string branchName = child.BranchLineToParent;

            GetStoredChildNames(parent).Add(childName, branchName);

            DrawableConfig config = new();
            config.MindMapNodeConfigs.Add(parent);
            config.MindMapNodeConfigs.Add(child);
            config.LineConfigs.Add(new LineConf { ID = branchName });

            GameMindMap.RenameMindMap(config, attachedObjects);

            Assert.That(parent.ID, Is.EqualTo(parentName));
            Assert.That(child.ID, Is.EqualTo(childName));
            Assert.That(child.ParentNode, Is.EqualTo(parentName));
            Assert.That(child.BranchLineToParent, Is.EqualTo(branchName));
            Assert.That(child.BranchLineConf.ID, Is.EqualTo(branchName));
            Assert.That(config.LineConfigs[0].ID, Is.EqualTo(branchName));

            Assert.That(
                GetStoredChildNames(parent)[childName],
                Is.EqualTo(branchName));
        }

        /// <summary>
        /// Verifies that importing a mind map node renames the node and
        /// its associated drawable objects when only the border or text
        /// ID conflicts with an existing object.
        /// </summary>
        /// <param name="borderCollision">
        /// True to introduce a border collision; false for a text collision.
        /// </param>
        [TestCase(true)]
        [TestCase(false)]
        public void TestRenameMindMapWithOnlyNestedObjectCollision(
            bool borderCollision)
        {
            string originalName = "Theme-ParentA";

            MindMapNodeConf node =
                CreateImportedNode(originalName, MindMapNodeKind.Theme, "");

            string conflictingName = borderCollision
                ? node.BorderConf.ID
                : node.TextConf.ID;

            GameObject existingObject = new(conflictingName);
            existingObject.transform.SetParent(attachedObjects.transform);

            DrawableConfig config = new();
            config.MindMapNodeConfigs.Add(node);

            Assert.DoesNotThrow(
                () => GameMindMap.RenameMindMap(config, attachedObjects));

            Assert.That(node.ID, Is.Not.EqualTo(originalName));
            Assert.That(node.ID, Does.StartWith("Theme-"));

            string newID = GameMindMap.GetIDofName(node.ID);

            Assert.That(
                node.BorderConf.ID,
                Is.EqualTo(ValueHolder.LinePrefix + "-" + newID));

            Assert.That(
                node.TextConf.ID,
                Is.EqualTo(ValueHolder.TextPrefix + "-" + newID));

            if (borderCollision)
            {
                Assert.That(node.BorderConf.ID, Is.Not.EqualTo(conflictingName));
            }
            else
            {
                Assert.That(node.TextConf.ID, Is.Not.EqualTo(conflictingName));
            }

            Assert.That(existingObject.name, Is.EqualTo(conflictingName));
            Assert.That(
                GameFinder.FindAttachedOrLocalDescendant(
                    attachedObjects, conflictingName),
                Is.SameAs(existingObject));
        }

        /// <summary>
        /// Verifies that an imported branch line cannot overwrite an
        /// existing branch line when neither node name conflicts.
        /// </summary>
        [Test]
        public void TestRenameMindMapWithOnlyBranchLineCollision()
        {
            string parentName = "Theme-ParentA";
            string childName = "Subtheme-ChildB";

            MindMapNodeConf parent =
                CreateImportedNode(parentName, MindMapNodeKind.Theme, "");

            MindMapNodeConf child =
                CreateImportedNode(
                    childName,
                    MindMapNodeKind.Subtheme,
                    parentName);

            string oldBranchName = child.BranchLineToParent;

            GameObject existingBranch = new(oldBranchName);
            existingBranch.transform.SetParent(attachedObjects.transform);

            GetStoredChildNames(parent).Add(childName, oldBranchName);

            DrawableConfig config = new();
            config.MindMapNodeConfigs.Add(parent);
            config.MindMapNodeConfigs.Add(child);
            config.LineConfigs.Add(new LineConf { ID = oldBranchName });

            Assert.DoesNotThrow(
                () => GameMindMap.RenameMindMap(config, attachedObjects));

            Assert.That(parent.ID, Is.EqualTo(parentName));
            Assert.That(child.ID, Is.Not.EqualTo(childName));
            Assert.That(child.ParentNode, Is.EqualTo(parentName));

            string expectedBranchName =
                ValueHolder.MindMapBranchLine + "-"
                + GameMindMap.GetIDofName(parent.ID) + "-"
                + GameMindMap.GetIDofName(child.ID);

            Assert.That(expectedBranchName, Is.Not.EqualTo(oldBranchName));
            Assert.That(child.BranchLineToParent, Is.EqualTo(expectedBranchName));
            Assert.That(child.BranchLineConf.ID, Is.EqualTo(expectedBranchName));
            Assert.That(config.LineConfigs[0].ID, Is.EqualTo(expectedBranchName));

            Dictionary<string, string> children = GetStoredChildNames(parent);

            Assert.That(children.ContainsKey(childName), Is.False);
            Assert.That(children[child.ID], Is.EqualTo(expectedBranchName));

            Assert.That(existingBranch.name, Is.EqualTo(oldBranchName));
            Assert.That(
                GameFinder.FindAttachedOrLocalDescendant(
                    attachedObjects, oldBranchName),
                Is.SameAs(existingBranch));
        }

        /// <summary>
        /// Creates a minimal Mind Map node configuration for testing
        /// imported names and parent-child references.
        /// </summary>
        /// <param name="name">The imported node name.</param>
        /// <param name="nodeKind">The node kind.</param>
        /// <param name="parentName">
        /// The imported parent name, or an empty string for a root node.
        /// </param>
        /// <returns>The imported node configuration.</returns>
        private static MindMapNodeConf CreateImportedNode(
            string name,
            MindMapNodeKind nodeKind,
            string parentName)
        {
            string id = GameMindMap.GetIDofName(name);

            string branchName = string.IsNullOrEmpty(parentName)
                ? ""
                : ValueHolder.MindMapBranchLine + "-"
                  + GameMindMap.GetIDofName(parentName) + "-"
                  + id;

            return new MindMapNodeConf
            {
                ID = name,
                NodeKind = nodeKind,
                ParentNode = parentName,
                BranchLineToParent = branchName,
                BorderConf = new LineConf
                {
                    ID = ValueHolder.LinePrefix + id
                },
                TextConf = new TextConf
                {
                    ID = ValueHolder.TextPrefix + id
                },
                BranchLineConf = string.IsNullOrEmpty(parentName)
                    ? null
                    : new LineConf
                    {
                        ID = branchName
                    }
            };
        }

        /// <summary>
        /// Retrieves the serialized child-to-branch name mapping of a
        /// Mind Map node configuration for regression assertions.
        /// </summary>
        /// <param name="node">The imported node configuration.</param>
        /// <returns>The stored child-name mapping.</returns>
        private static Dictionary<string, string> GetStoredChildNames(
            MindMapNodeConf node)
        {
            FieldInfo field = typeof(MindMapNodeConf).GetField(
                "childrenNames",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            return (Dictionary<string, string>)field.GetValue(node);
        }

        /// <summary>
        /// Creates and initializes a Mind Map node with the given name and node kind.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="nodeKind">The kind of the node.</param>
        /// <returns>The created Mind Map node.</returns>
        private GameObject CreateNode(string name, MindMapNodeKind nodeKind)
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
