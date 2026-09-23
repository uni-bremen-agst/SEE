using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;
using System.Reflection;
using TMPro;
using SEE.Game.Drawable.MindMap;

namespace SEE.UI.Menu.Drawable.MindMap
{
    /// <summary>
    /// Tests the node kind selection behavior of
    /// <see cref="MindMapChangeNodeKindMenu"/>.
    /// </summary>
    [TestFixture]
    public class TestMindMapChangeNodeKindMenu : DrawableMenuTestBase
    {
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
        /// Creates the Drawable hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
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
            node.AddComponent<MMNodeValueHolder>();
        }

        /// <summary>
        /// Destroys the menu and test hierarchy.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            MindMapChangeNodeKindMenu.Instance.Destroy();
            MindMapParentSelectionMenu.Instance.Destroy();

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Verifies that the node kind selector contains all available Mind Map node kinds.
        /// </summary>
        [Test]
        public void TestSelectorContainsAllNodeKinds()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();

            Assert.That(selector.itemList.Count, Is.EqualTo(3));
            Assert.That(selector.itemList[0].itemTitle, Is.EqualTo(MindMapNodeKind.Theme.ToString()));
            Assert.That(selector.itemList[1].itemTitle, Is.EqualTo(MindMapNodeKind.Subtheme.ToString()));
            Assert.That(selector.itemList[2].itemTitle, Is.EqualTo(MindMapNodeKind.Leaf.ToString()));
        }

        /// <summary>
        /// Verifies that the selector uses the configured node kind as its default item.
        /// </summary>
        [Test]
        public void TestConfiguredNodeKindIsUsedAsDefault()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Subtheme
            };

            MindMapChangeNodeKindMenu.Enable(node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();

            Assert.That(selector.defaultIndex, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that the currently selected selector index is returned as the
        /// corresponding Mind Map node kind.
        /// </summary>
        [Test]
        public void TestGetSelectedNodeKindUsesCurrentSelectorIndex()
        {
            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();
            selector.index = 2;

            Assert.That(
                MindMapChangeNodeKindMenu.GetSelectedNodeKind(),
                Is.EqualTo(MindMapNodeKind.Leaf));
        }

        /// <summary>
        /// Verifies that pressing the return button invokes the provided callback.
        /// </summary>
        [Test]
        public void TestReturnButtonInvokesCallback()
        {
            bool callbackInvoked = false;

            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(
                node,
                configuration,
                () => callbackInvoked = true);

            ButtonManagerBasic returnButton = FindReturnButton();
            returnButton.clickEvent.Invoke();

            Assert.That(callbackInvoked, Is.True);
        }

        /// <summary>
        /// Verifies that changing a Theme to a node kind that requires a parent does not
        /// modify the node until a valid parent has been explicitly confirmed.
        /// </summary>
        /// <param name="newNodeKind">The node kind that requires a parent.</param>
        [TestCase(MindMapNodeKind.Subtheme)]
        [TestCase(MindMapNodeKind.Leaf)]
        public void TestParentRequiredNodeKindChangeRemainsPendingUntilConfirmation(
            MindMapNodeKind newNodeKind)
        {
            MMNodeValueHolder nodeValueHolder = node.GetComponent<MMNodeValueHolder>();
            InitializeValueHolder(nodeValueHolder);
            nodeValueHolder.NodeKind = MindMapNodeKind.Theme;

            CreateParentCandidate();

            MindMapNodeConf configuration = new MindMapNodeConf
            {
                NodeKind = MindMapNodeKind.Theme
            };

            MindMapChangeNodeKindMenu.Enable(node, configuration, () => { });

            HorizontalSelector selector = FindNodeKindSelector();
            int index = GameMindMap.GetNodeKinds().IndexOf(newNodeKind);

            selector.index = index;
            selector.selectorEvent.Invoke(index);

            Assert.That(nodeValueHolder.NodeKind, Is.EqualTo(MindMapNodeKind.Theme));
            Assert.That(configuration.NodeKind, Is.EqualTo(MindMapNodeKind.Theme));
            Assert.That(nodeValueHolder.GetParent(), Is.Null);
        }

        /// <summary>
        /// Finds the node kind selector of the currently instantiated menu.
        /// </summary>
        /// <returns>The node kind selector.</returns>
        private static HorizontalSelector FindNodeKindSelector()
        {
            foreach (HorizontalSelector candidate in Object.FindObjectsByType<HorizontalSelector>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Selection")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the Mind Map node kind selector.");
            return null;
        }

        /// <summary>
        /// Finds the return button manager of the currently instantiated menu.
        /// </summary>
        /// <returns>The return button manager.</returns>
        private static ButtonManagerBasic FindReturnButton()
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "ReturnBtn")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the return button of the Mind Map node kind menu.");
            return null;
        }

        /// <summary>
        /// Creates a valid Theme that can be selected as the parent of the node under test.
        /// </summary>
        /// <returns>The created parent candidate.</returns>
        private GameObject CreateParentCandidate()
        {
            GameObject parent = new GameObject("Parent");
            parent.tag = Tags.MindMapNode;
            parent.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder valueHolder = parent.AddComponent<MMNodeValueHolder>();
            InitializeValueHolder(valueHolder);
            valueHolder.NodeKind = MindMapNodeKind.Theme;

            TextMeshPro text = parent.AddComponent<TextMeshPro>();
            text.text = parent.name;

            return parent;
        }

        /// <summary>
        /// Invokes the Unity initialization of the given Mind Map node value holder.
        /// EditMode tests do not execute the regular MonoBehaviour lifecycle reliably
        /// for this component.
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
