using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.UI.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu.Drawable
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
        /// Whether this class has a finished rotation in store that wasn't yet fetched.
        /// </summary>
        private static bool isFinish;

        /// <summary>
        /// Enables the scale menu and set ups the handler for the menu components.
        /// </summary>
        /// <param name="objToScale">Is the drawable type object that should be scaled.</param>
        /// <param name="stickyNoteMode">Enables the menu for the sticky notes.</param>
        /// <param name="returnCall"></param>
        public static void Enable(GameObject objToScale, bool stickyNoteMode = false, UnityAction returnCall = null)
        {
            /// Instantiate the menu.
            instance = PrefabInstantiator.InstantiatePrefab(drawableScalePrefab,
                        GameObject.Find("UI Canvas").transform, false);

            /// Initialize the GUI elements of the menu.
            xScale = GameFinder.FindChild(instance, "XScale").GetComponent<InputFieldWithButtons>();
            yScale = GameFinder.FindChild(instance, "YScale").GetComponent<InputFieldWithButtons>();
            switchManager = GameFinder.FindChild(instance, "Switch").GetComponent<SwitchManager>();
            doneObject = GameFinder.FindChild(instance, "Done");

            /// Sets up the x scale component.
            XScale(objToScale);

            /// Sets up the y scale component.
            YScale(objToScale);

            /// Enables the proportional scaling.
            EnableProportionalScaling();

            /// Sets up the switch for turning on / off proportional scaling.
            SetUpSwitch();

            /// Sets up the done button.
            SetUpDone(stickyNoteMode);

            /// Sets up the return button.
            SetUpReturn(returnCall);

            instance.SetActive(true);
        }

        /// <summary>
        /// Sets up the x scale component.
        /// </summary>
        /// <param name="objToScale">Is the object to be scaled.</param>
        private static void XScale(GameObject objToScale)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            xScale.onValueChanged.AddListener(xScale =>
            {
                Vector3 newScale = new(xScale, yScale.GetValue(), 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject drawable = GameFinder.GetDrawable(objToScale);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                new ScaleNetAction(drawable.name, drawableParent, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Sets up the y scale component.
        /// </summary>
        /// <param name="objToScale">Is the object to be scaled.</param>
        private static void YScale(GameObject objToScale)
        {
            yScale.AssignValue(objToScale.transform.localScale.y);
            yScale.onValueChanged.AddListener(yScale =>
            {
                Vector3 newScale = new (xScale.GetValue(), yScale, 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject drawable = GameFinder.GetDrawable(objToScale);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                new ScaleNetAction(drawable.name, drawableParent, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Enables the proportional scaling for the x and y scale component.
        /// </summary>
        private static void EnableProportionalScaling()
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
        }

        /// <summary>
        /// Sets up the switch to turning on/off the proportional scaling.
        /// </summary>
        private static void SetUpSwitch()
        {
            switchManager.isOn = true;
            /// Turns on proportional scaling.
            switchManager.OnEvents.AddListener(() =>
            {
                EnableProportionalScaling();
            });

            /// Turns off proportional scaling.
            switchManager.OffEvents.AddListener(() =>
            {
                xScale.onProportionalValueChanged = null;
                yScale.onProportionalValueChanged = null;
            });
        }

        /// <summary>
        /// Sets up the done button, if the <paramref name="stickyNoteMode"/> is true.
        /// Otherwise, the button will be disable.
        /// </summary>
        /// <param name="stickyNoteMode">true, if the menu was called from edit of a sticky note.</param>
        private static void SetUpDone(bool stickyNoteMode)
        {
            if (stickyNoteMode)
            {
                doneObject.SetActive(true);
                doneObject.GetComponent<ButtonManagerBasic>().clickEvent.RemoveAllListeners();
                doneObject.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
                {
                    Disable();
                    isFinish = true;
                });
            }
            else
            {
                doneObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets up the return button, if <paramref name="returnCall"/> is not null.
        /// Otherwise, the button will be disable.
        /// </summary>
        /// <param name="returnCall">The return call action.</param>
        private static void SetUpReturn(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                instance.transform.Find("ReturnBtn").gameObject.SetActive(true);
                GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                instance.transform.Find("ReturnBtn").gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Destroyes the scale menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
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

        /// <summary>
        /// If <see cref="isFinish"/> is true, the <paramref name="finish"/> will be the state. Otherwise it will be false.
        /// </summary>
        /// <param name="finish">The finish state</param>
        /// <returns><see cref="isFinish"/></returns>
        public static bool TryGetFinish(out bool finish)
        {
            if (isFinish)
            {
                finish = isFinish;
                isFinish = false;
                return true;
            }

            finish = false;
            return false;
        }

        /// <summary>
        /// Gets the state if the menu is active.
        /// </summary>
        /// <returns>true, if the menu is enabled.</returns>
        public static bool IsActive()
        {
            return instance != null;
        }
    }
}