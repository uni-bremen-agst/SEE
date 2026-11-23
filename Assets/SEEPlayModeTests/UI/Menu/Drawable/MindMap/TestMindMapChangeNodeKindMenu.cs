
using Michsky.MUIP;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu.Drawable.MindMap
{
    /// <summary>
    /// PlayMode tests for the node kind selection behavior of
    /// <see cref="MindMapChangeNodeKindMenu"/>.
    /// </summary>
    [TestFixture]
    internal class TestMindMapChangeNodeKindMenu : TestUI
    {
        /// <summary>
        /// The UI canvas used by the Drawable menus.
        /// </summary>
        private GameObject uiCanvasObject;

        /// <summary>
        /// The root object of the test Drawable hierarchy.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// The Drawable surface used by the tests.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The container for objects attached to the Drawable.
        /// </summary>
        private GameObject attachedObjects;

        /// <summary>
        /// The Mind Map node used by the tests.
        /// </summary>
        private GameObject node;

        /// <summary>
        /// Creates the UI canvas and Drawable hierarchy required by each test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for the Unity lifecycle to initialize.
        /// </returns>
        [UnitySetUp]
        public IEnumerator SetUpMindMapChangeNodeKindMenu()
        {
            uiCanvasObject = new GameObject(
                "UI Canvas",
                typeof(RectTransform),
                typeof(Canvas));

            root = new GameObject("DrawableRoot");

            surface = new GameObject("Surface");
            surface.tag = Tags.Drawable;
            surface.transform.SetParent(root.transform);

            attachedObjects = new GameObject("AttachedObjects");
            attachedObjects.tag = Tags.AttachedObjects;
            attachedObjects.transform.SetParent(root.transform);

            node = new GameObject("Node");
            node.tag = Tags.MindMapNode;
            node.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder nodeValueHolder =
                node.AddComponent<MMNodeValueHolder>();

            nodeValueHolder.NodeKind = MindMapNodeKind.Theme;

            yield return null;
        }

        /// <summary>
        /// Destroys the menus, UI canvas, and test hierarchy after each test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for deferred object destruction.
        /// </returns>
        [UnityTearDown]
        public IEnumerator TearDownMindMapChangeNodeKindMenu()
        {
            MindMapChangeNodeKindMenu.Instance.Destroy();
            MindMapParentSelectionMenu.Instance.Destroy();

            if (root != null)
            {
                Object.Destroy(root);
            }

            if (uiCanvasObject != null)
            {
                Object.Destroy(uiCanvasObject);
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that the node kind selector contains all available
        /// Mind Map node kinds.
        /// </summary>
        [Test]
        public void TestSelectorContainsAllNodeKinds()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(
                node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();

            Assert.That(selector.items.Count, Is.EqualTo(3));
            Assert.That(
                selector.items[0].itemTitle,
                Is.EqualTo(MindMapNodeKind.Theme.ToString()));
            Assert.That(
                selector.items[1].itemTitle,
                Is.EqualTo(MindMapNodeKind.Subtheme.ToString()));
            Assert.That(
                selector.items[2].itemTitle,
                Is.EqualTo(MindMapNodeKind.Leaf.ToString()));
        }

        /// <summary>
        /// Verifies that the selector uses the configured node kind
        /// as its default item.
        /// </summary>
        [Test]
        public void TestConfiguredNodeKindIsUsedAsDefault()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Subtheme
            };

            MindMapChangeNodeKindMenu.Enable(
                node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();

            Assert.That(selector.defaultIndex, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that the selected selector index is returned as
        /// the corresponding Mind Map node kind.
        /// </summary>
        [Test]
        public void TestGetSelectedNodeKindUsesCurrentSelectorIndex()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(
                node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();
            selector.index = 2;

            Assert.That(
                MindMapChangeNodeKindMenu.GetSelectedNodeKind(),
                Is.EqualTo(MindMapNodeKind.Leaf));
        }

        /// <summary>
        /// Verifies that pressing the return button invokes the provided
        /// callback when the selected node is a valid root Theme.
        /// </summary>
        [Test]
        public void TestReturnButtonInvokesCallback()
        {
            bool callbackInvoked = false;

            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            Assert.That(valueHolder, Is.Not.Null);

            /// In Play Mode, Awake determines the node kind from its name.
            /// Explicitly configure the test node as the root Theme
            /// required by this scenario.
            valueHolder.NodeKind = MindMapNodeKind.Theme;

            MindMapChangeNodeKindMenu.Enable(
                node,
                configuration,
                () => callbackInvoked = true);

            ButtonManager returnButton = FindReturnButton();
            returnButton.onClick.Invoke();

            Assert.That(callbackInvoked, Is.True);
        }

        /// <summary>
        /// Verifies that changing a Theme to a node kind requiring
        /// a parent does not modify the node before confirmation.
        /// </summary>
        /// <param name="newNodeKind">
        /// The node kind that requires a parent.
        /// </param>
        [TestCase(MindMapNodeKind.Subtheme)]
        [TestCase(MindMapNodeKind.Leaf)]
        public void TestParentRequiredNodeKindChangeRemainsPendingUntilConfirmation(
            MindMapNodeKind newNodeKind)
        {
            MMNodeValueHolder nodeValueHolder =
                node.GetComponent<MMNodeValueHolder>();

            nodeValueHolder.NodeKind = MindMapNodeKind.Theme;

            CreateParentCandidate();

            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(
                node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();
            int index = GameMindMap.GetNodeKinds().IndexOf(newNodeKind);

            selector.index = index;
            selector.onValueChanged.Invoke(index);

            Assert.That(
                nodeValueHolder.NodeKind,
                Is.EqualTo(MindMapNodeKind.Theme));

            Assert.That(
                configuration.NodeKind,
                Is.EqualTo(MindMapNodeKind.Theme));

            Assert.That(nodeValueHolder.GetParent(), Is.Null);
        }

        /// <summary>
        /// Verifies that rejecting a node kind change also cancels
        /// a pending parent selection from a previous node kind.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for the pending parent menu
        /// to be destroyed.
        /// </returns>
        [UnityTest]
        public IEnumerator TestRejectedNodeKindChangeCancelsPendingParentSelection()
        {
            MMNodeValueHolder nodeValueHolder =
                node.GetComponent<MMNodeValueHolder>();

            nodeValueHolder.NodeKind = MindMapNodeKind.Theme;

            CreateParentCandidate();

            GameObject child = new GameObject("Child");
            child.tag = Tags.MindMapNode;
            child.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder childValueHolder =
                child.AddComponent<MMNodeValueHolder>();

            childValueHolder.NodeKind = MindMapNodeKind.Leaf;

            GameObject branchLine = new GameObject("BranchLine");
            branchLine.transform.SetParent(attachedObjects.transform);

            nodeValueHolder.AddChild(child, branchLine);

            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(
                node,
                configuration,
                () => { });

            HorizontalSelector selector = FindNodeKindSelector();

            int subthemeIndex =
                GameMindMap.GetNodeKinds()
                    .IndexOf(MindMapNodeKind.Subtheme);

            selector.index = subthemeIndex;
            selector.onValueChanged.Invoke(subthemeIndex);

            Assert.That(
                MindMapParentSelectionMenu.Instance.IsOpen(),
                Is.True);

            int leafIndex =
                GameMindMap.GetNodeKinds()
                    .IndexOf(MindMapNodeKind.Leaf);

            selector.index = leafIndex;
            selector.onValueChanged.Invoke(leafIndex);

            yield return null;

            Assert.That(
                selector.index,
                Is.EqualTo(
                    GameMindMap.GetNodeKinds()
                        .IndexOf(MindMapNodeKind.Theme)));

            Assert.That(
                MindMapParentSelectionMenu.Instance.IsOpen(),
                Is.False);

            Assert.That(
                nodeValueHolder.NodeKind,
                Is.EqualTo(MindMapNodeKind.Theme));

            Assert.That(
                configuration.NodeKind,
                Is.EqualTo(MindMapNodeKind.Theme));
        }

        /// <summary>
        /// Finds the node-kind selector belonging to the currently
        /// instantiated Mind Map node-kind menu.
        /// </summary>
        /// <returns>The node-kind selector.</returns>
        private static HorizontalSelector FindNodeKindSelector()
        {
            GameObject selectorObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    GetNodeKindMenuObject(),
                    "Selection");

            Assert.That(selectorObject, Is.Not.Null);

            HorizontalSelector result =
                selectorObject.GetComponent<HorizontalSelector>();

            Assert.That(result, Is.Not.Null);

            return result;
        }

        /// <summary>
        /// Retrieves the game object owned by the current node-kind menu
        /// instance so test controls are not confused with controls
        /// belonging to other open or inactive menus.
        /// </summary>
        /// <returns>The current node-kind menu game object.</returns>
        private static GameObject GetNodeKindMenuObject()
        {
            FieldInfo field = typeof(Menu).GetField(
                "gameObject",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);

            GameObject result =
                field.GetValue(MindMapChangeNodeKindMenu.Instance)
                    as GameObject;

            Assert.That(result, Is.Not.Null);

            return result;
        }

        /// <summary>
        /// Finds the return button belonging to the currently instantiated
        /// Mind Map node-kind menu.
        /// </summary>
        /// <returns>The return button manager.</returns>
        private static ButtonManager FindReturnButton()
        {
            GameObject buttonObject =
                GameFinder.FindAttachedOrLocalDescendant(
                    GetNodeKindMenuObject(),
                    "ReturnBtn");

            Assert.That(buttonObject, Is.Not.Null);

            ButtonManager result =
                buttonObject.GetComponent<ButtonManager>();

            Assert.That(result, Is.Not.Null);

            return result;
        }

        /// <summary>
        /// Creates a valid Theme that can be selected as the parent
        /// of the node under test.
        /// </summary>
        /// <returns>The created parent candidate.</returns>
        private GameObject CreateParentCandidate()
        {
            GameObject parent = new GameObject("Parent");
            parent.tag = Tags.MindMapNode;
            parent.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder valueHolder =
                parent.AddComponent<MMNodeValueHolder>();

            valueHolder.NodeKind = MindMapNodeKind.Theme;

            TextMeshPro text = parent.AddComponent<TextMeshPro>();
            text.text = parent.name;

            return parent;
        }
    }
}
