
using Michsky.MUIP;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Drawable;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Play-mode integration tests for the menu used to edit drawable images.
    /// The tests use the real image-menu prefab and allow MUIP components
    /// to initialize through the Unity runtime lifecycle.
    /// </summary>
    [TestFixture]
    internal class TestImageMenu : TestUI
    {
        /// <summary>
        /// The UI canvas hosting the image menu.
        /// </summary>
        private GameObject uiCanvasObject;

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
        /// Creates the UI canvas, Drawable hierarchy, and image
        /// required by each test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for Unity's initialization lifecycle.
        /// </returns>
        [UnitySetUp]
        public IEnumerator SetUpImageMenu()
        {
            uiCanvasObject = new GameObject(
                "UI Canvas",
                typeof(RectTransform),
                typeof(Canvas));

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

            imageObject.AddComponent<AssociatedPageHolder>().AssociatedPage = 0;
            imageObject.AddComponent<OrderInLayerValueHolder>().OrderInLayer = 3;
            imageObject.AddComponent<ImageValueHolder>();

            texture = new Texture2D(2, 2);
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 2, 2),
                new Vector2(0.5f, 0.5f));

            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;

            yield return null;
        }

        /// <summary>
        /// Destroys the image menu and all objects created by the test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for deferred object destruction.
        /// </returns>
        [UnityTearDown]
        public IEnumerator TearDownImageMenu()
        {
            ImageMenu.Instance.Destroy();

            if (root != null)
            {
                Object.Destroy(root);
            }

            if (sprite != null)
            {
                Object.Destroy(sprite);
            }

            if (texture != null)
            {
                Object.Destroy(texture);
            }

            if (uiCanvasObject != null)
            {
                Object.Destroy(uiCanvasObject);
            }

            yield return null;
        }

        /// <summary>
        /// Opens the image menu and isolates the unrelated slider and
        /// color-picker callbacks from the tests of its thumbnail and
        /// mirror switch. The MUIP components remain enabled so their
        /// normal PlayMode initialization still takes place.
        /// </summary>
        /// <param name="configuration">
        /// The image configuration used to initialize the menu.
        /// </param>
        private void OpenImageMenu(ImageConf configuration)
        {
            ImageMenu.Instance.Enable(imageObject, configuration);

            LayerSliderController slider =
                uiCanvasObject.GetComponentInChildren<LayerSliderController>(true);

            Assert.That(slider, Is.Not.Null);
            slider.OnValueChanged.RemoveAllListeners();

            HSVPicker.ColorPicker colorPicker =
                uiCanvasObject.GetComponentInChildren<HSVPicker.ColorPicker>(true);

            Assert.That(colorPicker, Is.Not.Null);
            colorPicker.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Verifies that opening the image menu displays the sprite
        /// of the selected image.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestEnableDisplaysSelectedImageThumbnail()
        {
            ImageConf configuration = CreateConfiguration(false);

            OpenImageMenu(configuration);

            yield return null;

            Assert.That(FindThumbnail().sprite, Is.SameAs(sprite));
        }

        /// <summary>
        /// Verifies that the mirror switch reflects a non-mirrored
        /// image configuration.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestMirrorSwitchIsDisabledForNormalImage()
        {
            ImageConf configuration = CreateConfiguration(false);

            OpenImageMenu(configuration);

            yield return null;

            Assert.That(FindMirrorSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Verifies that the mirror switch reflects a mirrored
        /// image configuration.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestMirrorSwitchIsEnabledForMirroredImage()
        {
            ImageConf configuration = CreateConfiguration(true);

            OpenImageMenu(configuration);

            yield return null;

            Assert.That(FindMirrorSwitch().isOn, Is.True);
        }

        /// <summary>
        /// Verifies that reopening the image menu after destruction
        /// uses the new image configuration instead of retaining
        /// the previous mirror state.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestReopeningMenuUsesCurrentMirrorConfiguration()
        {
            OpenImageMenu(CreateConfiguration(true));

            yield return null;

            Assert.That(FindMirrorSwitch().isOn, Is.True);

            ImageMenu.Instance.Destroy();

            // Object destruction is deferred until the end of the frame.
            yield return null;

            Assert.That(ImageMenu.Instance.IsOpen(), Is.False);

            OpenImageMenu(CreateConfiguration(false));

            yield return null;

            Assert.That(FindMirrorSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Creates an image configuration for the test image.
        /// </summary>
        /// <param name="mirrored">
        /// Whether the image should be mirrored around the y axis.
        /// </param>
        /// <returns>The created configuration.</returns>
        private static ImageConf CreateConfiguration(bool mirrored)
        {
            return new ImageConf
            {
                OrderInLayer = 3,
                ImageColor = Color.white,
                EulerAngles = new Vector3(
                    0,
                    mirrored ? 180 : 0,
                    0)
            };
        }

        /// <summary>
        /// Finds the mirror switch belonging to the image menu
        /// instantiated on the test canvas.
        /// </summary>
        /// <returns>The mirror switch.</returns>
        private SwitchManager FindMirrorSwitch()
        {
            SwitchManager[] candidates =
                uiCanvasObject.GetComponentsInChildren<SwitchManager>(true);

            foreach (SwitchManager candidate in candidates)
            {
                if (candidate.gameObject.name == "Switch")
                {
                    return candidate;
                }
            }

            Assert.Fail(
                "Could not find the mirror switch of the image menu.");

            return null;
        }

        /// <summary>
        /// Finds the thumbnail image belonging to the image menu
        /// instantiated on the test canvas.
        /// </summary>
        /// <returns>The thumbnail image.</returns>
        private Image FindThumbnail()
        {
            Image[] candidates =
                uiCanvasObject.GetComponentsInChildren<Image>(true);

            foreach (Image candidate in candidates)
            {
                if (candidate.gameObject.name == "Image")
                {
                    return candidate;
                }
            }

            Assert.Fail(
                "Could not find the thumbnail image of the image menu.");

            return null;
        }
    }
}
