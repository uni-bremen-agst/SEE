using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;
using SEE.Controls.Actions;
using SEE.Game.UI.Tooltip;
using Michsky.UI.ModernUIPack;

namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// Indicates some kind of state with which a text is associated.
    /// The state will be displayed on the screen.
    /// </summary>
    public class HideStateIndicator : PlatformDependentComponent
    {
        /// <summary>
        /// Text of the mode panel.
        /// </summary>
        private TextMeshProUGUI ModePanelText;

        /// <summary>
        /// Background image (color) of the mode panel.
        /// </summary>
        private Image ModePanelImage;

        /// <summary>
        /// Represents the button that confirms the selection in the UI
        /// </summary>
        private GameObject doneButton;

        /// <summary>
        /// Represents the button that cancels the selection in the UI
        /// </summary>
        private GameObject backbutton;

        /// <summary>
        /// Path to the prefab of the mode panel.
        /// </summary>
        private const string HIDE_MODE_PANEL_PREFAB = "Prefabs/UI/HideModePanel";

        /// <summary>
        /// The color of the state indicator after it has been instantiated.
        /// </summary>
        private Color StartColor = Color.gray.ColorWithAlpha(0.5f);

        /// <summary>
        /// The text of the state indicator after it has been instantiated.
        /// </summary>
        private string StartText = "Select Objects";

        /// <summary>
        /// Saves the name of <see cref="doneButton"/>
        /// </summary>
        public string buttonNameDone;

        /// <summary>
        /// Saves the name of the button that cancels the selection in the UI.
        /// </summary>
        public string buttonNameBack;

        /// <summary>
        /// Used to store the color of <see cref="doneButton"/>.
        /// </summary>
        public Color buttonColorDone;

        /// <summary>
        /// Used to change the color of the cancels the selection in the UI.
        /// </summary>
        public Color buttonColorBack;

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
        public string descriptionDone;

        /// <summary>
        /// Saves the description of <see cref="backbutton"/>
        /// </summary>
        public string descriptionBack;

        /// <summary>
        /// Saves which function from the <see cref=HideAction"/> is to be executed upon confirmation.
        /// </summary>
        public HideModeSelector hideMode;

        /// <summary>
        /// Saves which property <see cref="doneButton"/> has.
        /// </summary>
        public HideModeSelector selectionTypeDone;

        /// <summary>
        /// Saves which property <see cref="backbutton"/> has.
        /// </summary>
        public HideModeSelector selectionTypeBack;

        /// <summary>
        /// Saves whether the selection was cancelled or confirmed.
        /// </summary>
        public HideModeSelector confirmCancel;

        /// <summary>
        /// Used to store the icon of <see cref="doneButton"/>
        /// </summary>
        public Sprite iconSpriteDone;

        /// <summary>
        /// Used to store the icon of <see cref="backbutton"/>
        /// </summary>
        public Sprite iconSpriteBack;

        /// <summary>
        /// Event triggered when the user presses the button.
        /// </summary>
        public readonly UnityEvent OnSelected = new UnityEvent();

        /// <summary>
        /// The normalized position in the canvas that the upper right corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMin = Vector2.left;

        /// <summary>
        /// The normalized position in the canvas that the lower left corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMax = Vector2.left;

        /// <summary>
        /// The normalized position in this Canvas that it rotates around.
        /// </summary>
        public Vector2 Pivot = Vector2.left;

        /// <summary>
        /// Name of this state indicator. Will not be displayed to the player.
        /// </summary>
        public string Title = "Hide Selection Panel";

        private void OnDisable()
        {
            if (ModePanelImage != null)
            {
                ModePanelImage.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (ModePanelImage != null)
            {
                ModePanelImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Sets all relevant values for the button
        /// </summary>
        /// <param name="indicator">Parent GameObject via which the button is accessed</param>
        private void SetUpButtonDone(GameObject indicator)
        {
            doneButton = indicator.transform.Find("Button_Done").gameObject;
            GameObject text = doneButton.transform.Find("Text").gameObject;
            GameObject icon = doneButton.transform.Find("Icon").gameObject;

            doneButton.name = buttonNameDone;
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

            buttonImage.color = buttonColorDone;
            textMeshPro.color = buttonColorDone.IdealTextColor();
            iconImage.color = buttonColorBack.IdealTextColor();
            buttonManager.buttonText = buttonNameDone;

            buttonManager.clickEvent.AddListener(() => setSelectionType(selectionTypeDone));
            pointerHelper.EnterEvent.AddListener(() => tooltipDone.Show(descriptionDone));
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

            backbutton.name = buttonNameBack;
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

            buttonImage.color = buttonColorBack;
            textMeshPro.color = buttonColorBack.IdealTextColor();
            iconImage.color = buttonColorBack.IdealTextColor();
            buttonManager.buttonText = buttonNameBack;

            buttonManager.clickEvent.AddListener(() => setSelectionType(selectionTypeBack));
            pointerHelper.EnterEvent.AddListener(() => tooltipBack.Show(descriptionBack));
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
                pointerHelper.EnterEvent.AddListener(() => tooltipDone.Show(descriptionDone));
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
                pointerHelper.EnterEvent.AddListener(() => tooltipBack.Show(descriptionBack));
                pointerHelper.ExitEvent.AddListener(tooltipBack.Hide);
            }
        }

        /// <summary>
        /// Sets <see cref="confirmCancel"/> to decide whether the selection was confirmed or cancelled. Then calls <see cref="Clicked"/> to trigger the listener.
        /// </summary>
        /// <param name="selectionType"></param>
        private void setSelectionType(HideModeSelector selectionType)
        {
            confirmCancel = selectionType;
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
        /// </summary>
        protected override void StartDesktop()
        {
            GameObject indicator = PrefabInstantiator.InstantiatePrefab(HIDE_MODE_PANEL_PREFAB, Canvas.transform, false);
            indicator.name = Title;
            SetupTooltipDone(indicator);
            SetupTooltipBack(indicator);
            SetUpButtonDone(indicator);
            SetUpButtonBack(indicator);

            RectTransform rectTransform = (RectTransform)indicator.transform;
            rectTransform.anchorMin = AnchorMin;
            rectTransform.anchorMax = AnchorMax;
            rectTransform.pivot = Pivot;
            rectTransform.anchoredPosition = Vector2.zero;

            if (indicator.TryGetComponentOrLog(out ModePanelImage))
            {
                ModePanelImage.color = StartColor;
            }

            if (indicator.transform.Find("ModeText")?.gameObject.TryGetComponentOrLog(out ModePanelText) != null)
            {
                ModePanelText.SetText(StartText);
            }
            else
            {
                Debug.LogError("Couldn't find ModeText game object in ModePanel\n");
            }
        }

        /// <summary>
        /// Changes the indicator to display the given <paramref name="text"/> and the given <paramref name="color"/>.
        /// </summary>
        /// <param name="text">The text to display</param>
        /// <param name="color">The background color of the indicator</param>
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
