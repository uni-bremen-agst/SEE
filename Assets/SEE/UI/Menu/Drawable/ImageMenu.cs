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
    /// This class provides a menu for editing an image.
    /// </summary>
    public class ImageMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the image menu prefeb is placed.
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

        static ImageMenu()
        {
            Instance = new ImageMenu();
        }

        /// <summary>
        /// The action for the HSV Color Picker that should also be carried out.
        /// </summary>
        private static UnityAction<Color> pickerAction;

        /// <summary>
        /// The slider controller for the order in layer.
        /// </summary>
        private static LayerSliderController orderInLayerSlider;

        /// <summary>
        /// The HSV color picker.
        /// </summary>
        private static HSVPicker.ColorPicker picker;

        /// <summary>
        /// The mirror switch. It will be needed to mirror the image on the y axis at 180°.
        /// </summary>
        private static SwitchManager mirrorSwitch;

        /// <summary>
        /// The thumbnail image of the chosen image.
        /// </summary>
        private static Image thumbnail;

        /// <summary>
        /// Enables the image menu and register the necessary Handler to the components.
        /// </summary>
        /// <param name="imageObj">The image object which should be changed.</param>
        /// <param name="newValueHolder">The configuration file which should be changed.</param>
        public static void Enable(GameObject imageObj, DrawableType newValueHolder)
        {
            if (newValueHolder is ImageConf imageConf)
            {
                Instantiate();
                GameObject surface = GameFinder.GetDrawableSurface(imageObj);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                orderInLayerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);

                /// Assigns an action to the slider that should be executed
                /// along with the current order in layer value
                AssignOrderInLayer(order =>
                {
                    GameEdit.ChangeLayer(imageObj, order);
                    imageConf.OrderInLayer = order;
                    ImageConf conf = ImageConf.GetImageConf(imageObj);
                    conf.FileData = null;
                    new EditImageNetAction(surface.name, surfaceParentName, conf).Execute();
                }, imageConf.OrderInLayer);

                /// Assigns an action to the color picker that should be executed
                /// along with the current color.
                AssignColorArea(color =>
                {
                    GameEdit.ChangeImageColor(imageObj, color);
                    imageConf.ImageColor = color;
                    ImageConf conf = ImageConf.GetImageConf(imageObj);
                    conf.FileData = null;
                    new EditImageNetAction(surface.name, surfaceParentName, conf).Execute();
                }, imageConf.ImageColor);

                /// Initialize the switch for mirror the image.
                InitMirrorSwitch(imageObj, imageConf, surface, surfaceParentName);

                /// Sets the thumbnail of the original image.
                thumbnail.sprite = imageObj.GetComponent<Image>().sprite;
            }
        }

        /// <summary>
        /// Creates the instance for the image menu and initializes the GUI elements.
        /// </summary>
        private static void Instantiate()
        {
            Instance.Instantiate(imageMenuPrefab);
            orderInLayerSlider = Instance.gameObject.GetComponentInChildren<LayerSliderController>();
            picker = Instance.gameObject.GetComponentInChildren<HSVPicker.ColorPicker>();
            mirrorSwitch = Instance.gameObject.GetComponentInChildren<SwitchManager>();
            thumbnail = GameFinder.FindChild(Instance.gameObject, "Image").GetComponent<Image>();
        }

        /// <summary>
        /// Initializes the mirror switch.
        /// It mirrors an image.
        /// Off is normal and on is mirrored.
        /// </summary>
        /// <param name="imageObj">The image object</param>
        /// <param name="imageConf">The configuration which holds the new values.</param>
        /// <param name="surface">The drawable surface on which the image is displayed</param>
        /// <param name="surfaceParentName">The id of the drawable surface parent</param>
        private static void InitMirrorSwitch(GameObject imageObj, ImageConf imageConf,
            GameObject surface, string surfaceParentName)
        {
            /// Removes the old handler.
            mirrorSwitch.OnEvents.RemoveAllListeners();
            mirrorSwitch.OffEvents.RemoveAllListeners();

            /// Mirrored display
            mirrorSwitch.OnEvents.AddListener(() =>
            {
                GameMoveRotator.SetRotateY(imageObj, 180f);
                imageConf.EulerAngles = new Vector3(0, 180, imageConf.EulerAngles.z);
                new RotatorYNetAction(surface.name, surfaceParentName, imageObj.name, 180f).Execute();
            });

            /// Normal display
            mirrorSwitch.OffEvents.AddListener(() =>
            {
                GameMoveRotator.SetRotateY(imageObj, 0);
                imageConf.EulerAngles = new Vector3(0, 0, imageConf.EulerAngles.z);
                new RotatorYNetAction(surface.name, surfaceParentName, imageObj.name, 0).Execute();
            });

            /// Sets the state of the switch.
            mirrorSwitch.isOn = imageConf.EulerAngles.y == 180;
        }

        /// <summary>
        /// Assigns an action and an order within the layer-slider order.
        /// </summary>
        /// <param name="orderInLayerAction">The action that should be assigned</param>
        /// <param name="order">The order that should be assigned.</param>
        private static void AssignOrderInLayer(UnityAction<int> orderInLayerAction, int order)
        {
            orderInLayerSlider.OnValueChanged.RemoveAllListeners();
            orderInLayerSlider.AssignValue(order);
            orderInLayerSlider.OnValueChanged.AddListener(orderInLayerAction);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
        /// It removes the previous additional action, if there was one.
        /// </summary>
        /// <param name="colorAction">The color action that should be assigned</param>
        /// <param name="color">The color that should be assigned.</param>
        private static void AssignColorArea(UnityAction<Color> colorAction, Color color)
        {
            if (pickerAction != null)
            {
                picker.onValueChanged.RemoveListener(pickerAction);
            }
            pickerAction = colorAction;
            picker.AssignColor(color);
            picker.onValueChanged.AddListener(colorAction);
        }
    }
}
