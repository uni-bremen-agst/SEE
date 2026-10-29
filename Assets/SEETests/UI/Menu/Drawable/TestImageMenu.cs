using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the menu used for editing drawable images.
    /// </summary>
    [TestFixture]
    public class TestImageMenu : DrawableMenuTestBase
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
        /// The container holding attached Drawable objects.
        /// </summary>
        private GameObject attachedObjects;

        /// <summary>
        /// The image object edited by the menu.
        /// </summary>
        private GameObject imageObject;

        /// <summary>
        /// The texture backing the test sprite.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// The sprite displayed by the test image.
        /// </summary>
        private Sprite sprite;

        /// <summary>
        /// Creates the Drawable hierarchy and image required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DrawableRoot");

            surface = new GameObject("Surface");
            surface.tag = Tags.Drawable;
            surface.transform.SetParent(root.transform);
            surface.AddComponent<DrawableHolder>().OrderInLayer = 10;

            attachedObjects = new GameObject("AttachedObjects");
            attachedObjects.tag = Tags.AttachedObjects;
            attachedObjects.transform.SetParent(surface.transform);

            imageObject = new GameObject("TestImage");
            imageObject.tag = Tags.Image;
            imageObject.transform.SetParent(attachedObjects.transform);

            texture = new Texture2D(2, 2);
            sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
        }

        /// <summary>
        /// Destroys the image menu and all objects created for the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ImageMenu.Instance.Destroy();

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (sprite != null)
            {
                Object.DestroyImmediate(sprite);
            }

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Verifies that opening the image menu displays the sprite of the selected image.
        /// </summary>
        [Test]
        public void TestEnableDisplaysSelectedImageThumbnail()
        {
            ImageConf configuration = CreateConfiguration(false);

            ImageMenu.Instance.Enable(imageObject, configuration);

            Assert.That(FindThumbnail().sprite, Is.SameAs(sprite));
        }

        /// <summary>
        /// Verifies that the mirror switch reflects a non-mirrored image configuration.
        /// </summary>
        [Test]
        public void TestMirrorSwitchIsDisabledForNormalImage()
        {
            ImageConf configuration = CreateConfiguration(false);

            ImageMenu.Instance.Enable(imageObject, configuration);

            Assert.That(FindMirrorSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Verifies that the mirror switch reflects a mirrored image configuration.
        /// </summary>
        [Test]
        public void TestMirrorSwitchIsEnabledForMirroredImage()
        {
            ImageConf configuration = CreateConfiguration(true);

            ImageMenu.Instance.Enable(imageObject, configuration);

            Assert.That(FindMirrorSwitch().isOn, Is.True);
        }

        /// <summary>
        /// Verifies that reopening the image menu after destruction uses the new
        /// image configuration instead of retaining the previous mirror state.
        /// </summary>
        [Test]
        public void TestReopeningMenuUsesCurrentMirrorConfiguration()
        {
            ImageMenu.Instance.Enable(imageObject, CreateConfiguration(true));
            Assert.That(FindMirrorSwitch().isOn, Is.True);

            ImageMenu.Instance.Destroy();

            ImageMenu.Instance.Enable(imageObject, CreateConfiguration(false));
            Assert.That(FindMirrorSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Creates an image configuration for the test image.
        /// </summary>
        /// <param name="mirrored">Whether the image should be mirrored around the y axis.</param>
        /// <returns>The created image configuration.</returns>
        private static ImageConf CreateConfiguration(bool mirrored)
        {
            return new ImageConf
            {
                OrderInLayer = 3,
                ImageColor = Color.white,
                EulerAngles = new Vector3(0, mirrored ? 180 : 0, 0)
            };
        }

        /// <summary>
        /// Finds the mirror switch of the image menu.
        /// </summary>
        /// <returns>The mirror switch.</returns>
        private static SwitchManager FindMirrorSwitch()
        {
            foreach (SwitchManager candidate in Object.FindObjectsByType<SwitchManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Switch")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the mirror switch of the image menu.");
            return null;
        }

        /// <summary>
        /// Finds the thumbnail image of the image menu.
        /// </summary>
        /// <returns>The thumbnail image.</returns>
        private static Image FindThumbnail()
        {
            foreach (Image candidate in Object.FindObjectsByType<Image>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Image")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the thumbnail image of the image menu.");
            return null;
        }
    }
}
