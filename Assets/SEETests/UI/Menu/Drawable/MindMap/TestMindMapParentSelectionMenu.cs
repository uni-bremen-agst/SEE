using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using System.Reflection;
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
        /// Creates the Mind Map hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            attachedObjects = new GameObject("AttachedObjects");

            addedNode = CreateNode("AddedNode", GameMindMap.NodeKind.Leaf);
            firstParent = CreateNode("FirstParent", GameMindMap.NodeKind.Theme);
            secondParent = CreateNode("SecondParent", GameMindMap.NodeKind.Subtheme);
        }

        /// <summary>
        /// Destroys the parent selection menu and all objects created for the current test.
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
        /// Verifies that reopening the parent selection menu initializes the chosen parent
        /// with the currently displayed default item instead of retaining a selection from
        /// a previous menu instance.
        /// </summary>
        [Test]
        public void TestReopeningMenuUsesDisplayedDefaultParent()
        {
            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            HorizontalSelector selector = FindParentSelector();

            /// Select the second parent in the first menu instance.
            SelectItem(selector, 1);

            Assert.That(MindMapParentSelectionMenu.GetChosenParent(), Is.SameAs(secondParent));

            MindMapParentSelectionMenu.Instance.Destroy();

            /// Reopen the menu without interacting with its selector.
            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            /// The selected parent must correspond to the displayed default item and must
            /// not retain the previous selection.
            Assert.That(MindMapParentSelectionMenu.GetChosenParent(), Is.SameAs(firstParent));
        }

        /// <summary>
        /// Verifies that leaf nodes are not offered as possible parents.
        /// </summary>
        [Test]
        public void TestLeafNodeIsExcludedFromParentCandidates()
        {
            GameObject leaf = CreateNode("Leaf", GameMindMap.NodeKind.Leaf);

            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            HorizontalSelector selector = FindParentSelector();

            Assert.That(selector.itemList.Exists(item => item.itemTitle == leaf.name), Is.False);
        }

        /// <summary>
        /// Verifies that the node for which a parent is selected cannot be selected
        /// as its own parent.
        /// </summary>
        [Test]
        public void TestAddedNodeIsExcludedFromParentCandidates()
        {
            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            HorizontalSelector selector = FindParentSelector();

            Assert.That(selector.itemList.Exists(item => item.itemTitle == addedNode.name), Is.False);
        }

        /// <summary>
        /// Verifies that inactive nodes are included when selecting a parent for a
        /// newly created Mind Map node.
        /// </summary>
        [Test]
        public void TestInactiveParentIsIncludedForNormalSelection()
        {
            firstParent.SetActive(false);

            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            HorizontalSelector selector = FindParentSelector();

            Assert.That(selector.itemList.Exists(item => item.itemTitle == firstParent.name), Is.True);
        }

        /// <summary>
        /// Verifies that inactive Mind Map nodes are not offered as parent candidates
        /// while editing an existing node.
        /// </summary>
        [Test]
        public void TestInactiveParentIsExcludedForEditing()
        {
            MindMapNodeConf configuration = ConfigureExistingParent(addedNode, secondParent);

            firstParent.SetActive(false);

            MindMapParentSelectionMenu.EnableForEditing(
                attachedObjects, addedNode, configuration, () => { });

            HorizontalSelector selector = FindParentSelector();

            Assert.That(selector.itemList.Exists(item => item.itemTitle == firstParent.name), Is.False);
            Assert.That(selector.itemList.Exists(item => item.itemTitle == secondParent.name), Is.True);
        }

        /// <summary>
        /// Verifies that a descendant of the edited node cannot be selected as its
        /// new parent because doing so would create a cycle.
        /// </summary>
        [Test]
        public void TestDescendantIsExcludedForEditing()
        {
            MindMapNodeConf configuration = ConfigureExistingParent(addedNode, firstParent);
            GameObject descendant = CreateNode("Descendant", GameMindMap.NodeKind.Subtheme);

            GameObject descendantBranchLine = new GameObject("DescendantBranchLine");
            descendantBranchLine.transform.SetParent(attachedObjects.transform);

            descendant.GetComponent<MMNodeValueHolder>().SetParent(addedNode, descendantBranchLine);

            MindMapParentSelectionMenu.EnableForEditing(
                attachedObjects, addedNode, configuration, () => { });

            HorizontalSelector selector = FindParentSelector();

            Assert.That(selector.itemList.Exists(item => item.itemTitle == descendant.name), Is.False);
            Assert.That(selector.itemList.Exists(item => item.itemTitle == firstParent.name), Is.True);
        }

        /// <summary>
        /// Verifies that finishing the parent selection stores the currently selected
        /// parent as the confirmed result.
        /// </summary>
        [Test]
        public void TestFinishConfirmsSelectedParent()
        {
            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            HorizontalSelector selector = FindParentSelector();
            SelectItem(selector, 1);

            Assert.That(MindMapParentSelectionMenu.GetChosenParent(), Is.SameAs(secondParent));

            ButtonManagerBasic finish = FindFinishButton();
            finish.clickEvent.Invoke();

            bool hasParent = MindMapParentSelectionMenu.TryGetParent(out GameObject parent);

            Assert.That(hasParent, Is.True);
            Assert.That(parent, Is.SameAs(secondParent));
        }

        /// <summary>
        /// Verifies that a confirmed parent selection can only be consumed once.
        /// </summary>
        [Test]
        public void TestConfirmedParentCanOnlyBeConsumedOnce()
        {
            MindMapParentSelectionMenu.Enable(attachedObjects, addedNode);

            ButtonManagerBasic finish = FindFinishButton();
            finish.clickEvent.Invoke();

            bool firstResult = MindMapParentSelectionMenu.TryGetParent(out GameObject firstResultParent);
            bool secondResult = MindMapParentSelectionMenu.TryGetParent(out GameObject secondResultParent);

            Assert.That(firstResult, Is.True);
            Assert.That(firstResultParent, Is.SameAs(firstParent));
            Assert.That(secondResult, Is.False);
            Assert.That(secondResultParent, Is.Null);
        }

        /// <summary>
        /// Finds the parent selector of the currently instantiated parent selection menu.
        /// </summary>
        /// <returns>The parent selector.</returns>
        private static HorizontalSelector FindParentSelector()
        {
            foreach (HorizontalSelector candidate in Object.FindObjectsByType<HorizontalSelector>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "ParentSelection")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the parent selector of the Mind Map parent selection menu.");
            return null;
        }

        /// <summary>
        /// Finds the finish button manager of the currently instantiated parent selection menu.
        /// </summary>
        /// <returns>The finish button manager.</returns>
        private static ButtonManagerBasic FindFinishButton()
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Finish")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the finish button of the Mind Map parent selection menu.");
            return null;
        }

        /// <summary>
        /// Selects the item at the given index and invokes the corresponding selector event.
        /// </summary>
        /// <param name="selector">The selector whose item should be selected.</param>
        /// <param name="index">The index of the item to select.</param>
        private static void SelectItem(HorizontalSelector selector, int index)
        {
            selector.index = index;
            selector.selectorEvent.Invoke(index);
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

            TextMeshPro text = node.AddComponent<TextMeshPro>();
            text.text = name;

            return node;
        }

        /// <summary>
        /// Invokes the Unity initialization of the given Mind Map node value holder.
        /// EditMode tests do not execute the regular MonoBehaviour lifecycle used during
        /// normal gameplay.
        /// </summary>
        /// <param name="valueHolder">The value holder that should be initialized.</param>
        private static void InitializeValueHolder(MMNodeValueHolder valueHolder)
        {
            MethodInfo awake = typeof(MMNodeValueHolder).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(awake, Is.Not.Null, "Could not find MMNodeValueHolder.Awake().");

            awake.Invoke(valueHolder, null);
        }

        /// <summary>
        /// Configures the given node with an existing parent and branch line so that
        /// opening the editing parent selection does not immediately change its parent.
        /// </summary>
        /// <param name="node">The node whose existing parent should be configured.</param>
        /// <param name="parent">The existing parent of the node.</param>
        /// <returns>The configuration representing the current parent.</returns>
        private MindMapNodeConf ConfigureExistingParent(GameObject node, GameObject parent)
        {
            GameObject branchLine = new GameObject("ParentBranchLine");
            branchLine.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
            valueHolder.NodeKind = GameMindMap.NodeKind.Subtheme;
            valueHolder.SetParent(parent, branchLine);

            return new MindMapNodeConf
            {
                ParentNode = parent.name
            };
        }
    }
}
