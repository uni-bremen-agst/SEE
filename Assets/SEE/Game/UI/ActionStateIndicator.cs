using SEE.Controls.Actions;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

namespace SEE.Game.UI
{
    /**
     * Represents an indicator which displays the current <see cref="ActionState"/>.
     */
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
        
        protected override void StartDesktop()
        {
            // Add the indicator prefab and parent it to the UI Canvas
            Object indicatorPrefab = Resources.Load<GameObject>(MODE_PANEL_PREFAB);
            GameObject indicator = Instantiate(indicatorPrefab, Canvas.transform, false) as GameObject;
            if (indicator == null)
            {
                Debug.LogError("Couldn't instantiate ModePanel prefab\n");
                return;
            }

            if (indicator.TryGetComponentOrLog(out ModePanelImage))
            {
                ModePanelImage.color = ActionState.Value.Color.ColorWithAlpha(0.5f);
            }

            if (indicator.transform.Find("ModeText")?.gameObject.TryGetComponentOrLog(out ModePanelText) != null)
            {
                ModePanelText.SetText(ActionState.Value.Name);
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
            ModePanelImage.color = newState.Color.ColorWithAlpha(0.5f);
            ModePanelText.text = newState.Name;
        }
    }
}