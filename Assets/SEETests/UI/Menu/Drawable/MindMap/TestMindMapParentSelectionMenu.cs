using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using TMPro;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the parent selection behavior of
    /// <see cref="MindMapParentSelectionMenu"/>.
    /// </summary>
    [TestFixture]
    public class TestMindMapParentSelectionMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The object containing the Mind Map nodes used by the tests.
        /// </summary>
        private GameObject attachedObjects;

        /// <summary>
        /// The node for which a parent should be selected.
        /// </summary>
        private GameObject addedNode;

        /// <summary>
        /// The first available parent node.
        /// </summary>
        private GameObject firstParent;

        /// <summary>
        /// The second available parent node.
        /// </summary>
        private GameObject secondParent;

        /// <summary>
        /// Creates the UI canvas and Mind Map hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            attachedObjects =
                new GameObject("AttachedObjects");

            addedNode = CreateNode(
                "AddedNode",
                GameMindMap.NodeKind.Leaf);

            firstParent = CreateNode(
                "FirstParent",
                GameMindMap.NodeKind.Theme);

            secondParent = CreateNode(
                "SecondParent",
                GameMindMap.NodeKind.Subtheme);
        }

        /// <summary>
        /// Destroys the parent selection menu and all objects created for the
        /// current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            MindMapParentSelectionMenu.Instance.Destroy();

            if (attachedObjects != null)
            {
                Object.DestroyImmediate(attachedObjects);
            }
        }

        /// <summary>
        /// Verifies that reopening the parent selection menu initializes the
        /// chosen parent with the currently displayed default item instead of
        /// retaining a selection from a previous menu instance.
        /// </summary>
        [Test]
        public void TestReopeningMenuUsesDisplayedDefaultParent()
        {
            MindMapParentSelectionMenu.Enable(
                attachedObjects,
                addedNode);

            HorizontalSelector selector =
                FindParentSelector();

            /// Select the second parent in the first menu instance.
            selector.selectorEvent.Invoke(1);

            Assert.That(
                MindMapParentSelectionMenu.GetChosenParent(),
                Is.SameAs(secondParent));

            MindMapParentSelectionMenu.Instance.Destroy();

            /// Reopen the menu without interacting with its selector.
            MindMapParentSelectionMenu.Enable(
                attachedObjects,
                addedNode);

            /// The selected parent must correspond to the displayed default
            /// item and must not retain the previous selection.
            Assert.That(
                MindMapParentSelectionMenu.GetChosenParent(),
                Is.SameAs(firstParent));
        }

        /// <summary>
        /// Finds the parent selector of the currently instantiated parent
        /// selection menu.
        /// </summary>
        /// <returns>The parent selector.</returns>
        private static HorizontalSelector FindParentSelector()
        {
            foreach (HorizontalSelector candidate in
                     Object.FindObjectsByType<HorizontalSelector>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "ParentSelection")
                {
                    return candidate;
                }
            }

            Assert.Fail(
                "Could not find the parent selector of the Mind Map parent selection menu.");

            return null;
        }

        /// <summary>
        /// Creates a Mind Map node with the given name and node kind.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="nodeKind">The kind of the node.</param>
        /// <returns>The created Mind Map node.</returns>
        private GameObject CreateNode(
            string name,
            GameMindMap.NodeKind nodeKind)
        {
            GameObject node =
                new GameObject(name);

            node.tag = Tags.MindMapNode;
            node.transform.SetParent(
                attachedObjects.transform);

            MMNodeValueHolder valueHolder =
                node.AddComponent<MMNodeValueHolder>();
            valueHolder.NodeKind = nodeKind;

            TextMeshPro text =
                node.AddComponent<TextMeshPro>();
            text.text = name;

            return node;
        }
    }
}
