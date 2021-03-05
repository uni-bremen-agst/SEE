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
        public TextMeshProUGUI ModePanelText;

        /// <summary>
        /// Background image (color) of the mode panel.
        /// </summary>
        private Image ModePanelImage;

        /// <summary>
        /// Path to the prefab of the mode panel.
        /// </summary>
        private const string MODE_PANEL_PREFAB = "Prefabs/UI/ModePanel";

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
        public void ChangeState(ActionStateType newState, ActionHistory ah)
        {
            ModePanelImage.color = newState.Color.ColorWithAlpha(0.5f);
            ModePanelText.text = newState.Name;

            if (newState.Name.Equals("New Node"))
            {
                AddNodeAction nodeAction = new AddNodeAction();
                nodeAction.Start();
                ah.ActionHistoryList.Add(nodeAction);
                ah.Pointer++;
            }
            if (newState.Name.Equals("New Edge"))
            {
                AddEdgeAction addEdgeAction = new AddEdgeAction();
                addEdgeAction.Start();
                ah.ActionHistoryList.Add(addEdgeAction);
                ah.Pointer++;
            }
            if (newState.Name.Equals("Scale Node"))
            {
                ScaleNodeAction scaleNodeAction = new ScaleNodeAction();
                scaleNodeAction.Start();
                ah.ActionHistoryList.Add(scaleNodeAction);
                ah.Pointer++;
            }
            if (newState.Name.Equals("Edit Node"))
            {
                EditNodeAction editNodeAction = new EditNodeAction();
                editNodeAction.Start();
                ah.ActionHistoryList.Add(editNodeAction);
                ah.Pointer++;
            }
            if (newState.Name.Equals("Rotate"))
            {
                // Fixme: Is this an action?
            }
            if (newState.Name.Equals("Map"))
            {
                // Fixme: Is this an action?
            }
            if (newState.Name.Equals("Delete Node"))
            {
                DeleteAction deleteAction = new DeleteAction();
                deleteAction.Start();
                ah.ActionHistoryList.Add(deleteAction);
                ah.Pointer++;
            }
        }
    }
}