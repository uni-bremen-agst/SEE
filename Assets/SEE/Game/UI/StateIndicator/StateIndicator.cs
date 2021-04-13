using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

namespace SEE.Game.UI
{
    /// <summary>
    /// Indicates some kind of state with which a text and a color is associated.
    /// The state will be displayed on the screen.
    /// </summary>
    public class StateIndicator: PlatformDependentComponent
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
        private const string MODE_PANEL_PREFAB = "Prefabs/UI/ModePanel";

        /// <summary>
        /// The color of the state indicator after it has been instantiated.
        /// </summary>
        private Color StartColor = Color.gray.ColorWithAlpha(0.5f);

        /// <summary>
        /// The text of the state indicator after it has been instantiated.
        /// </summary>
        private string StartText = "Unknown";

        /// <summary>
        /// The normalized position in the canvas that the upper right corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMin = Vector2.right;
        
        /// <summary>
        /// The normalized position in the canvas that the lower left corner is anchored to.
        /// Changes will only have an effect before Start() is called.
        /// </summary>
        public Vector2 AnchorMax = Vector2.right;

        /// <summary>
        /// The normalized position in this Canvas that it rotates around.
        /// </summary>
        public Vector2 Pivot = Vector2.right;

        /// <summary>
        /// Name of this state indicator. Will not be displayed to the player.
        /// </summary>
        public string Title = "StateIndicator";

        private void OnDisable()
        {
            ModePanelImage?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ModePanelImage?.gameObject.SetActive(true);
        }

        /// <summary>
        /// Adds the indicator prefab and parents it to the UI Canvas.
        /// </summary>
        protected override void StartDesktop()
        {
            Object indicatorPrefab = Resources.Load<GameObject>(MODE_PANEL_PREFAB);
            GameObject indicator = Instantiate(indicatorPrefab, Canvas.transform, false) as GameObject;
            if (indicator == null)
            {
                Debug.LogError("Couldn't instantiate ModePanel prefab\n");
                return;
            }
            indicator.name = Title;

            RectTransform rectTransform = (RectTransform) indicator.transform;
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
        public void ChangeState(string text, Color color)
        {
            if (HasStarted)
            {
                ModePanelImage.color = color.ColorWithAlpha(0.5f);
                ModePanelText.text = text;
            }
            else
            {
                // Indicator has not yet been initialized
                StartColor = color.ColorWithAlpha(0.5f);
                StartText = text;
            }
        }
    }
}
