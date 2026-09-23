using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using System.Reflection;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the Drawable rotation menu and its Mind Map specific child inclusion controls.
    /// </summary>
    [TestFixture]
    public class TestRotationMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The root object of the Drawable hierarchy used by the tests.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// The Drawable surface used by the tests.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The container holding the objects attached to the Drawable.
        /// </summary>
        private GameObject attachedObjects;

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
        }

        /// <summary>
        /// Destroys the rotation menu and all objects created for the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            RotationMenu.Instance.Destroy();

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Verifies that the Mind Map child controls are hidden for regular Drawable objects.
        /// </summary>
        [Test]
        public void TestChildrenControlsAreHiddenForNonMindMapObject()
        {
            GameObject selectedObject = new GameObject("SelectedObject");
            selectedObject.transform.SetParent(attachedObjects.transform);

            RotationMenu.Instance.Enable(selectedObject);

            SwitchManager childrenSwitch = FindChildrenSwitch();

            Assert.That(childrenSwitch.gameObject.activeInHierarchy, Is.False);
            Assert.That(RotationMenu.Instance.IncludeChildren, Is.False);
        }

        /// <summary>
        /// Verifies that the Mind Map child controls are visible when rotating a Mind Map node.
        /// </summary>
        [Test]
        public void TestChildrenControlsAreVisibleForMindMapNode()
        {
            GameObject selectedObject = CreateMindMapNode("SelectedNode");

            RotationMenu.Instance.Enable(selectedObject);

            SwitchManager childrenSwitch = FindChildrenSwitch();

            Assert.That(childrenSwitch.gameObject.activeInHierarchy, Is.True);
            Assert.That(RotationMenu.Instance.IncludeChildren, Is.False);
        }

        /// <summary>
        /// Verifies that destroying the rotation menu resets its child inclusion state.
        /// </summary>
        [Test]
        public void TestDestroyKeepsIncludeChildrenDisabledForNewSession()
        {
            GameObject selectedObject = CreateMindMapNode("SelectedNode");

            RotationMenu.Instance.Enable(selectedObject);
            Assert.That(RotationMenu.Instance.IncludeChildren, Is.False);

            RotationMenu.Instance.Destroy();

            Assert.That(RotationMenu.Instance.IncludeChildren, Is.False);
        }

        /// <summary>
        /// Finds the switch controlling whether Mind Map children are included.
        /// </summary>
        /// <returns>The child inclusion switch manager.</returns>
        private static SwitchManager FindChildrenSwitch()
        {
            foreach (SwitchManager candidate in Object.FindObjectsByType<SwitchManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "ChildrenSwitch")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the child inclusion switch of the rotation menu.");
            return null;
        }

        /// <summary>
        /// Creates and initializes a Mind Map node for the rotation menu tests.
        /// </summary>
        /// <param name="name">The name of the Mind Map node.</param>
        /// <returns>The created Mind Map node.</returns>
        private GameObject CreateMindMapNode(string name)
        {
            GameObject node = new GameObject(name);
            node.tag = Tags.MindMapNode;
            node.transform.SetParent(attachedObjects.transform);

            MMNodeValueHolder valueHolder = node.AddComponent<MMNodeValueHolder>();
            InitializeValueHolder(valueHolder);
            valueHolder.NodeKind = MindMapNodeKind.Subtheme;

            return node;
        }

        /// <summary>
        /// Invokes the Unity initialization of the given Mind Map node value holder.
        /// EditMode tests do not reliably execute the regular MonoBehaviour lifecycle
        /// used during gameplay.
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
