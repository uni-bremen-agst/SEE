using SEE.Controls.Actions;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

namespace SEE.Game.UI
{
    /// <summary>
    /// Represents an indicator which displays the current <see cref="ActionState"/>.
    /// </summary>
    public class ActionStateIndicator: PlatformDependentComponent
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
        /// The color of the action state indicator after it has been instantiated.
        /// </summary>
        private Color StartColor = Color.gray.ColorWithAlpha(0.5f);

        /// <summary>
        /// The text of the action state indicator after it has been instantiated.
        /// </summary>
        private string StartText = "Unknown";

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
        /// Changes the indicator to display the new action state type.
        /// </summary>
        /// <param name="newState">New state which shall be displayed in the indicator</param>
        public void ChangeState(ActionStateType newState)
        {
            if (ModePanelImage)
            {
                ModePanelImage.color = newState.Color.ColorWithAlpha(0.5f);
                ModePanelText.text = newState.Name;
            }
            else
            {
                // Indicator has not yet been initialized
                StartColor = newState.Color.ColorWithAlpha(0.5f);
                StartText = newState.Name;
            }
        }
    }
}