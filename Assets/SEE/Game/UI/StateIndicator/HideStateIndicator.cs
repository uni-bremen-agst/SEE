using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;
using SEE.Controls.Actions;
using SEE.Game.UI.Tooltip;

namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// Indicates some kind of state with which a text and a color is associated.
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

        public string buttonName;

        private Tooltip.Tooltip tooltip;

        public string description;

        public HideModeSelector hideMode;

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

        [System.Obsolete]
        void SetButtonName(GameObject indicator)
        {
            GameObject button = indicator.transform.Find("Button").gameObject;
            GameObject text = button.transform.Find("Text").gameObject;
            GameObject icon = button.transform.Find("Icon").gameObject;


            button.name = buttonName;
            if (!button.TryGetComponentOrLog(out Michsky.UI.ModernUIPack.ButtonManagerBasicWithIcon buttonManager) ||
                !button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                return;
            }

            buttonManager.buttonText = buttonName;
            buttonManager.clickEvent.AddListener(() => Clicked());
            pointerHelper.EnterEvent.AddListener(() => tooltip.Show(description));
        }

        void SetupTooltip(GameObject indicator)
        {
            GameObject button = indicator.transform.Find("Button").gameObject;
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(description));
                pointerHelper.ExitEvent.AddListener(tooltip.Hide);
                // FIXME scrolling doesn't work while hovering above the field, because
                // the Modern UI Pack uses an Event Trigger (see Utils/PointerHelper for an explanation.)
                // It is unclear how to resolve this without either abstaining from using the Modern UI Pack
                // in this instance or without modifying the Modern UI Pack, which would complicate 
                // updates greatly. Perhaps the author of the Modern UI Pack (or Unity developers?) should
                // be contacted about this.
            }
        }

        void Clicked()
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
            SetupTooltip(indicator);
            SetButtonName(indicator);


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
                // ModePanelImage.color = color.ColorWithAlpha(0.5f);
                ModePanelText.text = text;
            }
            else
            {
                // Indicator has not yet been initialized
                //  StartColor = color.ColorWithAlpha(0.5f);
                StartText = text;
            }
        }
    }
}
