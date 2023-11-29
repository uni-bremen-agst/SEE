using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// The class for the scale menu. It delievers a instance.
    /// Use ScaleMenu.Enable(GameObject objectToScale) and ScaleMenu().Disable()
    /// </summary>
    public static class ScaleMenu
    {
        /// <summary>
        /// The prefab for the scale menu.
        /// </summary>
        private const string drawableScalePrefab = "Prefabs/UI/Drawable/ScaleMenu";

        /// <summary>
        /// The input field with up and down button component for the x-scale.
        /// </summary>
        private static InputFieldWithButtons xScale;

        /// <summary>
        /// The input field with up and down button component for the x-scale.
        /// </summary>
        private static InputFieldWithButtons yScale;

        /// <summary>
        /// The switch for scale proportional or unproportional.
        /// </summary>
        private static SwitchManager switchManager;

        /// <summary>
        /// The object which contains the button done.
        /// </summary>
        private static GameObject doneObject;

        /// <summary>
        /// Holds the scale menu instance
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Basic constructor. It will create the instance of the scale menu.
        /// </summary>
        static ScaleMenu()
        {
            instance = PrefabInstantiator.InstantiatePrefab(drawableScalePrefab,
                                    GameObject.Find("UI Canvas").transform, false);

            xScale = GameFinder.FindChild(instance, "XScale").GetComponent<InputFieldWithButtons>();
            yScale = GameFinder.FindChild(instance, "YScale").GetComponent<InputFieldWithButtons>();
            switchManager = GameFinder.FindChild(instance, "Switch").GetComponent<SwitchManager>();
            doneObject = GameFinder.FindChild(instance, "Done");
        }

        /// <summary>
        /// Enables the scale menu and set ups the handler for the menu components.
        /// </summary>
        /// <param name="objToScale">Is the drawable type object that should be scaled.</param>
        /// <param name="stickyNoteMode">Enables the menu for the sticky notes.</param>
        /// <param name="returnCall"></param>
        public static void Enable(GameObject objToScale, bool stickyNoteMode = false, UnityAction returnCall = null)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            xScale.onValueChanged.AddListener(xScale =>
            {
                Vector3 newScale = new Vector3(xScale, yScale.GetValue(), 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject drawable = GameFinder.GetDrawable(objToScale);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                new ScaleNetAction(drawable.name, drawableParent, objToScale.name, newScale).Execute();
            });

            yScale.AssignValue(objToScale.transform.localScale.y);
            yScale.onValueChanged.AddListener(yScale =>
            {
                Vector3 newScale = new Vector3(xScale.GetValue(), yScale, 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject drawable = GameFinder.GetDrawable(objToScale);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                new ScaleNetAction(drawable.name, drawableParent, objToScale.name, newScale).Execute();
            });

            xScale.onProportionalValueChanged = new UnityEvent<float>();
            xScale.onProportionalValueChanged.AddListener(diff =>
            {
                yScale.AssignValue(yScale.GetValue() + diff);
            });

            yScale.onProportionalValueChanged = new UnityEvent<float>();
            yScale.onProportionalValueChanged.AddListener(diff =>
            {
                xScale.AssignValue(xScale.GetValue() + diff);
            });

            switchManager.isOn = true;
            switchManager.OnEvents.AddListener(() =>
            {
                xScale.onProportionalValueChanged = new UnityEvent<float>();
                xScale.onProportionalValueChanged.AddListener(diff =>
                {
                    yScale.AssignValue(yScale.GetValue() + diff);
                });

                yScale.onProportionalValueChanged = new UnityEvent<float>();
                yScale.onProportionalValueChanged.AddListener(diff =>
                {
                    xScale.AssignValue(xScale.GetValue() + diff);
                });
            });
            switchManager.OffEvents.AddListener(() =>
            {
                xScale.onProportionalValueChanged = null;
                yScale.onProportionalValueChanged = null;
            });

            if (stickyNoteMode)
            {
                doneObject.SetActive(true);
                doneObject.GetComponent<ButtonManagerBasic>().clickEvent.RemoveAllListeners();
                doneObject.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
                {
                    Disable();
                    StickyNoteAction.finish = true;
                });
            } else
            {
                doneObject.SetActive(false);
            }
            if (returnCall != null)
            {
                instance.transform.Find("ReturnBtn").gameObject.SetActive(true);
                GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(returnCall);
            }
            else
            {
                instance.transform.Find("ReturnBtn").gameObject.SetActive(false);
            }
            instance.SetActive(true);
        }

        /// <summary>
        /// Hides the scale menu and removes the handler.
        /// </summary>
        public static void Disable()
        {
            xScale.onValueChanged.RemoveAllListeners();
            yScale.onValueChanged.RemoveAllListeners();
            instance.SetActive(false);
        }

        /// <summary>
        /// Assigns the x and y scale value of the selected object to the input fields of this menu.
        /// </summary>
        /// <param name="objToScale">The drawable type object that should be scaled.</param>
        public static void AssignValue(GameObject objToScale)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            yScale.AssignValue(objToScale.transform.localScale.y);
        }
    }
}