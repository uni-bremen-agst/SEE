using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the menu for editing drawable images.
    /// </summary>
    public class ImageMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the image menu prefab is placed.
        /// </summary>
        private const string imageMenuPrefab = "Prefabs/UI/Drawable/ImageMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ImageMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ImageMenu Instance { get; private set; }

        /// <summary>
        /// The additional action registered at the HSV color picker.
        /// </summary>
        private UnityAction<Color> pickerAction;

        /// <summary>
        /// The slider controller for the order in layer.
        /// </summary>
        private LayerSliderController orderInLayerSlider;

        /// <summary>
        /// The HSV color picker.
        /// </summary>
        private HSVPicker.ColorPicker picker;

        /// <summary>
        /// The switch used to mirror the image around the y axis.
        /// </summary>
        private SwitchManager mirrorSwitch;

        /// <summary>
        /// The thumbnail displaying the selected image.
        /// </summary>
        private Image thumbnail;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static ImageMenu()
        {
            Instance = new ImageMenu();
        }

        /// <summary>
        /// Enables the image menu and registers the handlers required for editing
        /// the given image.
        /// </summary>
        /// <param name="imageObj">The image object that should be changed.</param>
        /// <param name="newValueHolder">The configuration that stores the changed values.</param>
        public void Enable(GameObject imageObj, DrawableType newValueHolder)
        {
            if (newValueHolder is not ImageConf imageConf)
            {
                return;
            }

            InitializeMenu();

            GameObject surface = GameFinder.GetDrawableSurface(imageObj);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            orderInLayerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);

            /// Assigns an action to the slider that is executed together with
            /// the current order-in-layer value.
            AssignOrderInLayer(order =>
            {
                GameLayerChanger.SetOrderInLayer(imageObj, order);
                imageConf.OrderInLayer = order;

                ImageConf conf = ImageConf.GetImageConf(imageObj);
                conf.FileData = null;

                new EditImageNetAction(surface.name, surfaceParentName, conf).Execute();
            }, imageConf.OrderInLayer);

            /// Assigns an action to the color picker that is executed together
            /// with the currently selected color.
            AssignColorArea(color =>
            {
                GameEdit.ChangeImageColor(imageObj, color);
                imageConf.ImageColor = color;

                ImageConf conf = ImageConf.GetImageConf(imageObj);
                conf.FileData = null;

                new EditImageNetAction(surface.name, surfaceParentName, conf).Execute();
            }, imageConf.ImageColor);

            /// Initializes the switch for mirroring the image.
            InitializeMirrorSwitch(imageObj, imageConf, surface, surfaceParentName);

            /// Displays the original image as thumbnail.
            thumbnail.sprite = imageObj.GetComponent<Image>().sprite;
        }

        /// <summary>
        /// Instantiates the image menu and resolves its UI controls.
        /// </summary>
        private void InitializeMenu()
        {
            Instantiate(imageMenuPrefab);

            orderInLayerSlider = gameObject.GetComponentInChildren<LayerSliderController>(true);
            picker = gameObject.GetComponentInChildren<HSVPicker.ColorPicker>(true);
            mirrorSwitch = gameObject.GetComponentInChildren<SwitchManager>(true);
            thumbnail = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Image").GetComponent<Image>();
        }

        /// <summary>
        /// Initializes the mirror switch.
        /// An enabled switch mirrors the image around the y axis by 180 degrees.
        /// </summary>
        /// <param name="imageObj">The image object.</param>
        /// <param name="imageConf">The configuration storing the changed values.</param>
        /// <param name="surface">The drawable surface containing the image.</param>
        /// <param name="surfaceParentName">The ID of the drawable surface parent.</param>
        private void InitializeMirrorSwitch(GameObject imageObj, ImageConf imageConf,
            GameObject surface, string surfaceParentName)
        {
            mirrorSwitch.OnEvents.RemoveAllListeners();
            mirrorSwitch.OffEvents.RemoveAllListeners();

            /// Mirrored display.
            mirrorSwitch.OnEvents.AddListener(() =>
            {
                GameMoveRotator.SetRotateY(imageObj, 180f);
                imageConf.EulerAngles = new Vector3(0, 180, imageConf.EulerAngles.z);
                new RotatorYNetAction(surface.name, surfaceParentName, imageObj.name, 180f).Execute();
            });

            /// Normal display.
            mirrorSwitch.OffEvents.AddListener(() =>
            {
                GameMoveRotator.SetRotateY(imageObj, 0);
                imageConf.EulerAngles = new Vector3(0, 0, imageConf.EulerAngles.z);
                new RotatorYNetAction(surface.name, surfaceParentName, imageObj.name, 0).Execute();
            });

            mirrorSwitch.isOn = imageConf.EulerAngles.y == 180;
            mirrorSwitch.UpdateUI();
        }

        /// <summary>
        /// Assigns an action and an order to the order-in-layer slider.
        /// </summary>
        /// <param name="orderInLayerAction">The action that should be assigned.</param>
        /// <param name="order">The order that should be assigned.</param>
        private void AssignOrderInLayer(UnityAction<int> orderInLayerAction, int order)
        {
            orderInLayerSlider.OnValueChanged.RemoveAllListeners();
            orderInLayerSlider.AssignValue(order);
            orderInLayerSlider.OnValueChanged.AddListener(orderInLayerAction);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker.
        /// The previously registered image-menu action is removed first.
        /// </summary>
        /// <param name="colorAction">The color action that should be assigned.</param>
        /// <param name="color">The color that should be assigned.</param>
        private void AssignColorArea(UnityAction<Color> colorAction, Color color)
        {
            if (pickerAction != null)
            {
                picker.onValueChanged.RemoveListener(pickerAction);
            }

            pickerAction = colorAction;
            picker.AssignColor(color);
            picker.onValueChanged.AddListener(pickerAction);
        }

        /// <summary>
        /// Destroys the image menu and clears its cached UI references and callbacks.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            pickerAction = null;
            orderInLayerSlider = null;
            picker = null;
            mirrorSwitch = null;
            thumbnail = null;
        }
    }
}
