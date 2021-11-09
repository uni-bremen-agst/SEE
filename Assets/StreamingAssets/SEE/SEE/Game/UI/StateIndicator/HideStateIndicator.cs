using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SEE.Controls.Actions;
using Michsky.UI.ModernUIPack;

namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// An indicator for hiding nodes and edges.
    /// The state will be displayed on the screen.
    /// </summary>
    public class HideStateIndicator : AbstractStateIndicator
    {
        /// <summary>
        /// Sets the <see cref="Title"/>, <see cref="PREFAB"/>, and <see cref="StartText"/> 
        /// of this state indicator.
        /// </summary>
        private void Awake()
        {
            Title = "Hide Selection Panel";
            PREFAB = "Prefabs/UI/HideModePanel";
            StartText = "Select Objects";
        }

        /// <summary>
        /// Represents the button that confirms the selection in the UI.
        /// </summary>
        private GameObject doneButton;

        /// <summary>
        /// Represents the button that cancels the selection in the UI.
        /// </summary>
        private GameObject backbutton;

        /// <summary>
        /// Saves the name of <see cref="doneButton"/>
        /// </summary>
        public string ButtonNameDone;

        /// <summary>
        /// Saves the name of the button that cancels the selection in the UI.
        /// </summary>
        public string ButtonNameBack;

        /// <summary>
        /// Used to store the color of <see cref="doneButton"/>.
        /// </summary>
        public Color ButtonColorDone;

        /// <summary>
        /// Used to change the color of the cancels the selection in the UI.
        /// </summary>
        public Color ButtonColorBack;

        /// <summary>
        /// The tooltip containing the <see cref=description"/> of this <see cref="Property"/>, which will
        /// be displayed when hovering above it.
        /// Used for <see cref="doneButton"/>.
        /// </summary>
        private Tooltip.Tooltip tooltipDone;

        /// <summary>
        /// The tooltip containing the <see cref=description"/> of this <see cref="Property"/>, which will
        /// be displayed when hovering above it.
        /// Used for <see cref="backbutton"/>.
        /// </summary>
        private Tooltip.Tooltip tooltipBack;

        /// <summary>
        /// Saves the description of <see cref="doneButton"/>
        /// </summary>
        public string DescriptionDone;

        /// <summary>
        /// Saves the description of <see cref="backbutton"/>
        /// </summary>
        public string DescriptionBack;

        /// <summary>
        /// Saves which function from the <see cref=HideAction"/> is to be executed upon confirmation.
        /// </summary>
        public HideModeSelector HideMode;

        /// <summary>
        /// Saves which property <see cref="doneButton"/> has.
        /// </summary>
        public HideModeSelector SelectionTypeDone;

        /// <summary>
        /// Saves which property <see cref="backbutton"/> has.
        /// </summary>
        public HideModeSelector SelectionTypeBack;

        /// <summary>
        /// Saves whether the selection was cancelled or confirmed.
        /// </summary>
        public HideModeSelector ConfirmCancel;

        /// <summary>
        /// Used to store the icon of <see cref="doneButton"/>
        /// </summary>
        private Sprite iconSpriteDone;

        /// <summary>
        /// Used to store the icon of <see cref="backbutton"/>
        /// </summary>
        private Sprite iconSpriteBack;

        /// <summary>
        /// Event triggered when the user presses the button.
        /// </summary>
        public readonly UnityEvent OnSelected = new UnityEvent();

        /// <summary>
        /// Sets all relevant values for the button
        /// </summary>
        /// <param name="indicator">Parent GameObject via which the button is accessed</param>
        private void SetUpButtonDone(GameObject indicator)
        {
            doneButton = indicator.transform.Find("Button_Done").gameObject;
            GameObject text = doneButton.transform.Find("Text").gameObject;
            GameObject icon = doneButton.transform.Find("Icon").gameObject;

            doneButton.name = ButtonNameDone;
            if (!doneButton.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager) ||
                    !doneButton.TryGetComponentOrLog(out Image buttonImage) ||
                    !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro) ||
                    !icon.TryGetComponentOrLog(out Image iconImage) ||
                    !doneButton.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                return;
            }

            if (iconSpriteDone != null)
            {
                buttonManager.buttonIcon = iconSpriteDone;
            }

            buttonImage.color = ButtonColorDone;
            textMeshPro.color = ButtonColorDone.IdealTextColor();
            iconImage.color = ButtonColorBack.IdealTextColor();
            buttonManager.buttonText = ButtonNameDone;

            buttonManager.clickEvent.AddListener(() => SetSelectionType(SelectionTypeDone));
            pointerHelper.EnterEvent.AddListener(() => tooltipDone.Show(DescriptionDone));
        }

        /// <summary>
        /// Sets all relevant values for the button
        /// </summary>
        /// <param name="indicator">Parent GameObject via which the button is accessed</param>
        private void SetUpButtonBack(GameObject indicator)
        {
            backbutton = indicator.transform.Find("Button_Back").gameObject;
            GameObject text = backbutton.transform.Find("Text").gameObject;
            GameObject icon = backbutton.transform.Find("Icon").gameObject;

            backbutton.name = ButtonNameBack;
            if (!backbutton.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager) ||
                    !backbutton.TryGetComponentOrLog(out Image buttonImage) ||
                    !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro) ||
                    !icon.TryGetComponentOrLog(out Image iconImage) ||
                    !backbutton.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                return;
            }
            
            if(iconSpriteBack != null)
            {
                buttonManager.buttonIcon = iconSpriteBack;
            }

            buttonImage.color = ButtonColorBack;
            textMeshPro.color = ButtonColorBack.IdealTextColor();
            iconImage.color = ButtonColorBack.IdealTextColor();
            buttonManager.buttonText = ButtonNameBack;

            buttonManager.clickEvent.AddListener(() => SetSelectionType(SelectionTypeBack));
            pointerHelper.EnterEvent.AddListener(() => tooltipBack.Show(DescriptionBack));
        }

        /// <summary>
        /// Sets up the tooltips for <see cref="doneButton"/>
        /// </summary>
        /// <param name="indicator">The parent object of <see cref="doneButton"/> to which the tooltip is to be attached</param>
        private void SetupTooltipDone(GameObject indicator)
        {
            GameObject button = indicator.transform.Find("Button_Done").gameObject;
            tooltipDone = gameObject.AddComponent<Tooltip.Tooltip>();
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(() => tooltipDone.Show(DescriptionDone));
                pointerHelper.ExitEvent.AddListener(tooltipDone.Hide);
            }
        }

        /// <summary>
        /// Sets up the tooltips for <see cref="backbutton"/>
        /// </summary>
        /// <param name="indicator">The parent object of <see cref="backbutton"/> to which the tooltip is to be attached</param>
        private void SetupTooltipBack(GameObject indicator)
        {
            GameObject button = indicator.transform.Find("Button_Back").gameObject;
            tooltipBack = gameObject.AddComponent<Tooltip.Tooltip>();
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(() => tooltipBack.Show(DescriptionBack));
                pointerHelper.ExitEvent.AddListener(tooltipBack.Hide);
            }
        }

        /// <summary>
        /// Sets <see cref="ConfirmCancel"/> to decide whether the selection was confirmed or cancelled. 
        /// Then calls <see cref="Clicked"/> to trigger the listener.
        /// </summary>
        /// <param name="selectionType"></param>
        private void SetSelectionType(HideModeSelector selectionType)
        {
            ConfirmCancel = selectionType;
            Clicked();
        }

        /// <summary>
        /// Event, is called when the button is clicked.
        /// </summary>
        private void Clicked()
        {
            OnSelected.Invoke();
        }

        /// <summary>
        /// Adds the indicator prefab and parents it to the UI Canvas.
        /// Sets up the tool tips and buttons.
        /// </summary>
        protected override void StartDesktop()
        {
            GameObject indicator = StartDesktopInit();
            SetupTooltipDone(indicator);
            SetupTooltipBack(indicator);
            SetUpButtonDone(indicator);
            SetUpButtonBack(indicator);
        }

        /// <summary>
        /// Changes the indicator to display the given <paramref name="text"/> and the given <paramref name="color"/>.
        /// </summary>
        /// <param name="text">The text to display</param>
        public void ChangeState(string text)
        {
            if (HasStarted)
            {
                ModePanelText.text = text;
            }
            else
            {
                // Indicator has not yet been initialized
                StartText = text;
            }
        }
    }
}
