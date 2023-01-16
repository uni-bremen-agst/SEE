using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// Indicates some kind of state with which a text and a color is associated.
    /// The state will be displayed on the screen.
    /// </summary>
    public class AbstractStateIndicator : PlatformDependentComponent
    {
        /// <summary>
        /// Path to the prefab of the panel.
        /// </summary>
        protected string PREFAB;

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
        public string Title;

        /// <summary>
        /// Text of the mode panel.
        /// </summary>
        protected TextMeshProUGUI ModePanelText;

        /// <summary>
        /// Background image (color) of the mode panel.
        /// </summary>
        protected Image ModePanelImage;

        /// <summary>
        /// The color of the state indicator after it has been instantiated.
        /// </summary>
        protected Color StartColor = Color.gray.WithAlpha(0.5f);

        /// <summary>
        /// The text of the state indicator after it has been instantiated.
        /// </summary>
        protected string StartText = "Unknown";

        /// <summary>
        /// Changes the indicator to display the given <paramref name="text"/> and the given <paramref name="color"/>.
        /// </summary>
        /// <param name="text">The text to display</param>
        /// <param name="color">The background color of the indicator</param>
        public void ChangeState(string text, Color color)
        {
            if (HasStarted)
            {
                ModePanelImage.color = color.WithAlpha(0.5f);
                ModePanelText.text = text;
            }
            else
            {
                // Indicator has not yet been initialized
                StartColor = color.WithAlpha(0.5f);
                StartText = text;
            }
        }

        protected GameObject StartDesktopInit()
        {
            GameObject indicator = PrefabInstantiator.InstantiatePrefab(PREFAB, Canvas.transform, false);
            indicator.name = Title;

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
            return indicator;
        }

        protected virtual void OnDisable()
        {
            if (ModePanelImage != null)
            {
                ModePanelImage.gameObject.SetActive(false);
            }
        }

        protected virtual void OnEnable()
        {
            if (ModePanelImage != null)
            {
                ModePanelImage.gameObject.SetActive(true);
            }
        }
    }
}