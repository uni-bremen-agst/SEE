using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the menu for scaling drawable objects.
    /// </summary>
    public class ScaleMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab for the scale menu.
        /// </summary>
        private const string drawableScalePrefab = "Prefabs/UI/Drawable/ScaleMenu";

        /// <summary>
        /// The input field with buttons for the x scale.
        /// </summary>
        private InputFieldWithButtons xScale;

        /// <summary>
        /// The input field with buttons for the y scale.
        /// </summary>
        private InputFieldWithButtons yScale;

        /// <summary>
        /// The switch controlling proportional scaling.
        /// </summary>
        private SwitchManager switchManager;

        /// <summary>
        /// The object containing the finish button.
        /// </summary>
        private GameObject doneObject;

        /// <summary>
        /// Whether this menu has a finished scaling operation that has not yet been consumed.
        /// </summary>
        private bool isFinish;

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ScaleMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ScaleMenu Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static ScaleMenu()
        {
            Instance = new ScaleMenu();
        }

        /// <summary>
        /// Enables the scale menu and sets up the handlers for its controls.
        /// </summary>
        /// <param name="objToScale">The drawable object that should be scaled.</param>
        /// <param name="stickyNoteMode">Whether the menu is used for editing a sticky note.</param>
        /// <param name="returnCall">
        /// An optional callback that is invoked when the return button is pressed.
        /// </param>
        public void Enable(GameObject objToScale, bool stickyNoteMode = false, UnityAction returnCall = null)
        {
            Instantiate(drawableScalePrefab);
            isFinish = false;

            /// Initialize the GUI elements of the menu.
            xScale = GameFinder.FindAttachedOrLocalDescendant(gameObject, "XScale").GetComponent<InputFieldWithButtons>();
            yScale = GameFinder.FindAttachedOrLocalDescendant(gameObject, "YScale").GetComponent<InputFieldWithButtons>();
            switchManager = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Switch").GetComponent<SwitchManager>();
            doneObject = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Done");

            /// Sets up the x scale component.
            SetUpXScale(objToScale);

            /// Sets up the y scale component.
            SetUpYScale(objToScale);

            /// Enables proportional scaling.
            EnableProportionalScaling();

            /// Sets up the switch for turning proportional scaling on and off.
            SetUpSwitch();

            /// Sets up the finish button.
            SetUpDone(stickyNoteMode);

            /// Sets up the return button.
            SetUpReturn(returnCall);

            base.Enable();
        }

        /// <summary>
        /// Sets up the x scale component.
        /// </summary>
        /// <param name="objToScale">The object to be scaled.</param>
        private void SetUpXScale(GameObject objToScale)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            xScale.OnValueChanged.AddListener(value =>
            {
                Vector3 newScale = new(value, yScale.GetValue(), 1);
                GameScaler.SetScale(objToScale, newScale);

                GameObject surface = GameFinder.GetDrawableSurface(objToScale);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                new ScaleNetAction(surface.name, surfaceParentName, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Sets up the y scale component.
        /// </summary>
        /// <param name="objToScale">The object to be scaled.</param>
        private void SetUpYScale(GameObject objToScale)
        {
            yScale.AssignValue(objToScale.transform.localScale.y);
            yScale.OnValueChanged.AddListener(value =>
            {
                Vector3 newScale = new(xScale.GetValue(), value, 1);
                GameScaler.SetScale(objToScale, newScale);

                GameObject surface = GameFinder.GetDrawableSurface(objToScale);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                new ScaleNetAction(surface.name, surfaceParentName, objToScale.name, newScale).Execute();
            });
        }

        /// <summary>
        /// Enables proportional scaling for the x and y scale components.
        /// To prevent floating-point errors, values are rounded to a maximum of six decimal places.
        /// </summary>
        private void EnableProportionalScaling()
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
        /// Sets up the switch for turning proportional scaling on and off.
        /// </summary>
        private void SetUpSwitch()
        {
            switchManager.isOn = true;

            /// Turns on proportional scaling.
            switchManager.OnEvents.AddListener(EnableProportionalScaling);

            /// Turns off proportional scaling.
            switchManager.OffEvents.AddListener(() =>
            {
                xScale.OnProportionalValueChanged = null;
                yScale.OnProportionalValueChanged = null;
            });
        }

        /// <summary>
        /// Sets up the finish button for sticky-note editing.
        /// The button is hidden outside sticky-note mode.
        /// </summary>
        /// <param name="stickyNoteMode">Whether the menu was opened while editing a sticky note.</param>
        private void SetUpDone(bool stickyNoteMode)
        {
            if (stickyNoteMode)
            {
                doneObject.SetActive(true);

                ButtonManagerBasic doneButton = doneObject.GetComponent<ButtonManagerBasic>();
                doneButton.clickEvent.RemoveAllListeners();
                doneButton.clickEvent.AddListener(() =>
                {
                    Destroy();
                    isFinish = true;
                });
            }
            else
            {
                doneObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets up the return button if a return callback is provided.
        /// The button is hidden otherwise.
        /// </summary>
        /// <param name="returnCall">The callback invoked when the return button is pressed.</param>
        private void SetUpReturn(UnityAction returnCall)
        {
            GameObject returnButtonObject = gameObject.transform.Find("ReturnBtn").gameObject;

            if (returnCall != null)
            {
                returnButtonObject.SetActive(true);

                ButtonManagerBasic returnButton = returnButtonObject.GetComponent<ButtonManagerBasic>();
                returnButton.clickEvent.RemoveAllListeners();
                returnButton.clickEvent.AddListener(returnCall);
            }
            else
            {
                returnButtonObject.SetActive(false);
            }
        }

        /// <summary>
        /// Assigns the x and y scale of the selected object to the scale controls.
        /// </summary>
        /// <param name="objToScale">The drawable object whose scale should be displayed.</param>
        public void AssignValue(GameObject objToScale)
        {
            xScale.AssignValue(objToScale.transform.localScale.x);
            yScale.AssignValue(objToScale.transform.localScale.y);
        }

        /// <summary>
        /// Tries to consume a previously completed scaling operation.
        /// </summary>
        /// <param name="finish">Whether a completed scaling operation was available.</param>
        /// <returns>True if a completed scaling operation was available; otherwise, false.</returns>
        public bool TryGetFinish(out bool finish)
        {
            if (isFinish)
            {
                finish = true;
                isFinish = false;
                return true;
            }

            finish = false;
            return false;
        }
    }
}
