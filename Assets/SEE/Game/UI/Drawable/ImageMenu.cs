using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    public static class ImageMenu
    {
        /// <summary>
        /// The location where the image menu prefeb is placed.
        /// </summary>
        private const string imageMenuPrefab = "Prefabs/UI/Drawable/ImageMenu";

        /// <summary>
        /// The instance of the image menu.
        /// </summary>
        public static GameObject instance;

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
        /// The mirror switch. It will needed to mirror the image on the y axis at 180°.
        /// </summary>
        private static SwitchManager mirrorSwitch;

        /// <summary>
        /// The thubnail image of the chosen image.
        /// </summary>
        private static Image thumbnail;

        /// <summary>
        /// The init constructor that create the instance for the image menu.
        /// It hides the image menu by default.
        /// </summary>
        static ImageMenu()
        {
            instance = PrefabInstantiator.InstantiatePrefab(imageMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            orderInLayerSlider = instance.GetComponentInChildren<LayerSliderController>();
            picker = instance.GetComponentInChildren<HSVPicker.ColorPicker>();
            mirrorSwitch = instance.GetComponentInChildren<SwitchManager>();
            thumbnail = GameDrawableFinder.FindChild(instance, "Image").GetComponent<Image>();
            instance.SetActive(false);
        }

        /// <summary>
        /// To hide the image menu.
        /// </summary>
        public static void Disable()
        {
            instance.SetActive(false);
        }

        /// <summary>
        /// Enables the image menu and register the necressary Handler to the components.
        /// </summary>
        /// <param name="imageObj">The image object which should be changed.</param>
        /// <param name="newValueHolder">The configuration file which should be changed.</param>
        public static void Enable(GameObject imageObj, DrawableType newValueHolder)
        {
            if (newValueHolder is ImageConf image)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(imageObj);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                AssignOrderInLayer(order =>
                {
                    GameEdit.ChangeLayer(imageObj, order);
                    image.orderInLayer = order;
                    ImageConf conf = ImageConf.GetImageConf(imageObj);
                    conf.fileData = null;
                    new EditImageNetAction(drawable.name, drawableParentName, conf).Execute();
                }, image.orderInLayer);
                AssignColorArea(color =>
                {
                    GameEdit.ChangeImageColor(imageObj, color);
                    image.imageColor = color;
                    ImageConf conf = ImageConf.GetImageConf(imageObj);
                    conf.fileData = null;
                    new EditImageNetAction(drawable.name, drawableParentName, conf).Execute();
                }, image.imageColor);
                mirrorSwitch.OnEvents.RemoveAllListeners();
                mirrorSwitch.OffEvents.RemoveAllListeners();
                mirrorSwitch.OnEvents.AddListener(() =>
                {
                    GameMoveRotator.SetRotateY(imageObj, 180f);
                    image.eulerAngles = new Vector3(0, 180, image.eulerAngles.z);
                    new RotatorYNetAction(drawable.name, drawableParentName, imageObj.name, 180f).Execute();
                });
                mirrorSwitch.OffEvents.AddListener(() =>
                {
                    GameMoveRotator.SetRotateY(imageObj, 0);
                    image.eulerAngles = new Vector3(0, 0, image.eulerAngles.z);
                    new RotatorYNetAction(drawable.name, drawableParentName, imageObj.name, 0).Execute();
                });
                thumbnail.sprite = imageObj.GetComponent<Image>().sprite;
                instance.SetActive(true);
            }
        }

        /// <summary>
        /// Assigns an action and a order to the order in layer slider.
        /// </summary>
        /// <param name="orderInLayerAction">The int action that should be assigned</param>
        /// <param name="order">The order that should be assigned.</param>
        private static void AssignOrderInLayer(UnityAction<int> orderInLayerAction, int order)
        {
            orderInLayerSlider.onValueChanged.RemoveAllListeners();
            orderInLayerSlider.AssignValue(order);
            orderInLayerSlider.onValueChanged.AddListener(orderInLayerAction);
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
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