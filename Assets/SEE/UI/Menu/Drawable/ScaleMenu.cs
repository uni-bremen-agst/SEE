using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The class for the scale menu. It delivers an instance.
    /// Use ScaleMenu.Enable(GameObject objectToScale) and ScaleMenu().Destroy()
    /// </summary>
    public class ScaleMenu : SingletonMenu
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
        /// The switch for scaling proportionally or unproportionally.
        /// </summary>
        private static SwitchManager switchManager;

        /// <summary>
        /// The object which contains the button done.
        /// </summary>
        private static GameObject doneObject;

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ScaleMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ScaleMenu Instance { get; private set; }

        static ScaleMenu()
        {
            Instance = new ScaleMenu();
        }

        /// <summary>
        /// Whether this class has a finished rotation in store that hasn't been fetched yet.
        /// </summary>
        private static bool isFinish;

        /// <summary>
        /// Enables the scale menu and sets up the handler for the menu components.
        /// </summary>
        /// <param name="objToScale">Is the drawable type object that should be scaled.</param>
        /// <param name="stickyNoteMode">Enables the menu for the sticky notes.</param>
        /// <param name="returnCall"></param>
        public static void Enable(GameObject objToScale, bool stickyNoteMode = false, UnityAction returnCall = null)
        {
            Instance.Instantiate(drawableScalePrefab);

            /// Initialize the GUI elements of the menu.
            xScale = GameFinder.FindChild(Instance.gameObject, "XScale").GetComponent<InputFieldWithButtons>();
            yScale = GameFinder.FindChild(Instance.gameObject, "YScale").GetComponent<InputFieldWithButtons>();
            switchManager = GameFinder.FindChild(Instance.gameObject, "Switch").GetComponent<SwitchManager>();
            doneObject = GameFinder.FindChild(Instance.gameObject, "Done");

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

            Instance.Enable();
        }

        /// <summary>
        /// Sets up the x scale component.
        /// </summary>
        /// <param name="objToScale">Is the object to be scaled.</param>
        private static void XScale(GameObject objToScale)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            xScale.OnValueChanged.AddListener(xScale =>
            {
                Vector3 newScale = new(xScale, yScale.GetValue(), 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject surface = GameFinder.GetDrawableSurface(objToScale);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                new ScaleNetAction(surface.name, surfaceParentName, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Sets up the y scale component.
        /// </summary>
        /// <param name="objToScale">Is the object to be scaled.</param>
        private static void YScale(GameObject objToScale)
        {
            yScale.AssignValue(objToScale.transform.localScale.y);
            yScale.OnValueChanged.AddListener(yScale =>
            {
                Vector3 newScale = new(xScale.GetValue(), yScale, 1);
                GameScaler.SetScale(objToScale, newScale);
                GameObject surface = GameFinder.GetDrawableSurface(objToScale);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                new ScaleNetAction(surface.name, surfaceParentName, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Enables the proportional scaling for the x and y scale component.
        /// To prevent floating-point errors, rounding is applied to a maximum of 6 decimal places.
        /// </summary>
        private static void EnableProportionalScaling()
        {
            xScale.OnProportionalValueChanged = new UnityEvent<float>();
            xScale.OnProportionalValueChanged.AddListener(diff =>
            {
                yScale.AssignValue((float)decimal.Round((decimal)(yScale.GetValue() + diff), 6));
            });
            yScale.OnProportionalValueChanged = new UnityEvent<float>();
            yScale.OnProportionalValueChanged.AddListener(diff =>
            {
                xScale.AssignValue((float)decimal.Round((decimal)(xScale.GetValue() + diff), 6));
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
                xScale.OnProportionalValueChanged = null;
                yScale.OnProportionalValueChanged = null;
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
                    Instance.Destroy();
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
                Instance.gameObject.transform.Find("ReturnBtn").gameObject.SetActive(true);
                GameFinder.FindChild(Instance.gameObject, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                Instance.gameObject.transform.Find("ReturnBtn").gameObject.SetActive(false);
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
        /// If <see cref="isFinish"/> is true, the <paramref name="finish"/> will be the state.
        /// Otherwise it will be false.
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
    }
}
