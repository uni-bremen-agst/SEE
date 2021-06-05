using SEE.Controls;
using UnityEngine;
using UnityEngine.Events;
using SEE.Controls.Actions;
using SEE.Game.UI.StateIndicator;
using SEE.Utils;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// Creates different dialogues to access all functions from the <see cref="HideAction"/> within the UI.
    /// </summary>
    public class HidePropertyDialog
    {
        /// <summary>
        /// Stores which function from the <see cref="HideAction"/> is to be executed.
        /// </summary>
        public HideModeSelector mode;

        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnConfirm = new UnityEvent();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnCancel = new UnityEvent();

        /// <summary>
        /// Dialogue providing all functions from the <see cref="HideAction"/> class
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// Used to select multiple elements in a graph.
        /// </summary>
        private GameObject selectionDialog;

        /// <summary>
        /// Shows the current HideMode
        /// </summary>
        private HideStateIndicator indicator;

        /// <summary>
        /// Represents the Selected HideMode
        /// </summary>
        public HideInInspector selectedMode;

        /// <summary>
        /// Button to hide all elements
        /// </summary>
        private ButtonProperty bHideAll;

        /// <summary>
        /// Button to hide all incoming edges and connected nodes
        /// </summary>
        private ButtonProperty bHideIncoming;

        /// <summary>
        /// Button to hide all outgoing edges and connected nodes
        /// </summary>
        private ButtonProperty bHideOutgoing;

        /// <summary>
        /// Button to hide all nodes that are reachable with a transitive forward closure
        /// </summary>
        private ButtonProperty bHideForwardTransitiveClosure;

        /// <summary>
        /// Button to hide all nodes that are reachable with a transitive backward closure
        /// </summary>
        private ButtonProperty bHideBackwardTransitiveClosure;

        /// <summary>
        /// Button to hide all nodes that are reachable with a transitive closure
        /// </summary>
        private ButtonProperty bHideTransitiveClosure;

        /// <summary>
        /// Button to hide all selected nodes and their edges
        /// </summary>
        private ButtonProperty bHideSelected;

        /// <summary>
        /// Button to hide all unselected nodes and their edges
        /// </summary>
        private ButtonProperty bHideUnselected;

        /// <summary>
        /// Button to hide all edges of the selected nodes
        /// </summary>
        private ButtonProperty bHideAllEdgesOfSelected;

        /// <summary>
        /// Button to highlight all edges that lie between nodes
        /// </summary>
        private ButtonProperty bHighlightConnectingEdges;

        /// <summary>
        /// Creates a new dialogue to realise functions where several elements are selected.
        /// The dialogue contains two buttons to confirm and cancel the selection.
        /// </summary>
        private void OpenSelectionMenu(HideModeSelector hidemode)
        {
            // Creating a new selection
            selectionDialog = new GameObject("Indicator");

            indicator = selectionDialog.AddComponent<HideStateIndicator>();
            indicator.ButtonNameDone = "Done";
            indicator.ButtonNameBack = "Back";
            indicator.ButtonColorBack = Color.red.Darker();
            indicator.ButtonColorDone = Color.green.Darker();
            indicator.AnchorMin = Vector2.zero;
            indicator.AnchorMax = Vector2.zero;
            indicator.Pivot = Vector2.zero;
            indicator.SelectionTypeDone = HideModeSelector.Confirmed;
            indicator.SelectionTypeBack = HideModeSelector.Back;
            indicator.DescriptionBack = "Back to selection";
            indicator.DescriptionDone = "Confirm selection";
            indicator.HideMode = hidemode;
            indicator.ChangeState("Select Objects");
           
            // Register listeners for selection menu
            indicator.OnSelected.AddListener(() => SetMode(indicator.HideMode, indicator.ConfirmCancel));
        }

        /// <summary>
        /// Provides all possible functions that are available for the selection of a single element.
        /// </summary>
        public void Open()
        {
            // Creating a new dialog
            dialog = new GameObject("HideAction mode selector");

            // Create new buttons 
            bHideAll = dialog.AddComponent<ButtonProperty>();
            bHideAll.Name = "Hide all";
            bHideAll.Description = "Hides everything";
            bHideAll.buttonColor = Color.red.Darker();
            bHideAll.Value = HideModeSelector.HideAll;
            bHideAll.selectionType = HideModeSelector.SelectSingle;

            bHideIncoming = dialog.AddComponent<ButtonProperty>();
            bHideIncoming.Name = "Hide incoming";
            bHideIncoming.Description = "Hides only incoming edges";
            bHideIncoming.buttonColor = Color.blue.Darker();
            bHideIncoming.Value = HideModeSelector.HideIncoming;
            bHideIncoming.selectionType = HideModeSelector.SelectSingle;

            bHideOutgoing = dialog.AddComponent<ButtonProperty>();
            bHideOutgoing.Name = "Hide outgoing";
            bHideOutgoing.Description = "Hides only outgoing edges";
            bHideOutgoing.buttonColor = Color.magenta.Darker();
            bHideOutgoing.Value = HideModeSelector.HideOutgoing;
            bHideOutgoing.selectionType = HideModeSelector.SelectSingle;

            bHideForwardTransitiveClosure = dialog.AddComponent<ButtonProperty>();
            bHideForwardTransitiveClosure.Name = "Hide forward transitive closure";
            bHideForwardTransitiveClosure.Description = "Hides nodes reachable transitively via outgoing edges of the selected node.";
            bHideForwardTransitiveClosure.buttonColor = Color.cyan.Darker();
            bHideForwardTransitiveClosure.Value = HideModeSelector.HideForwardTransitiveClosure;
            bHideForwardTransitiveClosure.selectionType = HideModeSelector.SelectSingle;

            bHideBackwardTransitiveClosure = dialog.AddComponent<ButtonProperty>();
            bHideBackwardTransitiveClosure.Name = "Hide backward transitive closure";
            bHideBackwardTransitiveClosure.Description = "Hides nodes reachable transitively via incoming edges of the selected node.";
            bHideBackwardTransitiveClosure.buttonColor = Color.red.Darker();
            bHideBackwardTransitiveClosure.Value = HideModeSelector.HideBackwardTransitiveClosure;
            bHideBackwardTransitiveClosure.selectionType = HideModeSelector.SelectSingle;

            bHideTransitiveClosure = dialog.AddComponent<ButtonProperty>();
            bHideTransitiveClosure.Name = "Hide transitive closure";
            bHideTransitiveClosure.Description = "Hides nodes reachable transitively via incoming and outgoing edges of the selected node.";
            bHideTransitiveClosure.buttonColor = Color.yellow.Darker();
            bHideTransitiveClosure.Value = HideModeSelector.HideAllTransitiveClosure;
            bHideTransitiveClosure.selectionType = HideModeSelector.SelectSingle;

            bHideSelected = dialog.AddComponent<ButtonProperty>();
            bHideSelected.Name = "Hide selected";
            bHideSelected.Description = "Hides only the selected objects.";
            bHideSelected.buttonColor = Color.green.Darker();
            bHideSelected.Value = HideModeSelector.HideSelected;
            bHideSelected.selectionType = HideModeSelector.SelectMultiple;

            bHideUnselected = dialog.AddComponent<ButtonProperty>();
            bHideUnselected.Name = "Hide unselected";
            bHideUnselected.Description = "Hides only the unselected objects.";
            bHideUnselected.buttonColor = Color.yellow.Darker();
            bHideUnselected.Value = HideModeSelector.HideUnselected;
            bHideUnselected.selectionType = HideModeSelector.SelectMultiple;

            bHideAllEdgesOfSelected = dialog.AddComponent<ButtonProperty>();
            bHideAllEdgesOfSelected.Name = "Hide edges";
            bHideAllEdgesOfSelected.Description = "Hides the edges for all selected nodes.";
            bHideAllEdgesOfSelected.buttonColor = Color.magenta.Darker();
            bHideAllEdgesOfSelected.Value = HideModeSelector.HideAllEdgesOfSelected;
            bHideAllEdgesOfSelected.selectionType = HideModeSelector.SelectSingle;

            bHighlightConnectingEdges = dialog.AddComponent<ButtonProperty>();
            bHighlightConnectingEdges.Name = "Highlight connecting edges";
            bHighlightConnectingEdges.Description = "Highlights edges connecting the selected nodes.";
            bHighlightConnectingEdges.buttonColor = Color.yellow.Darker();
            bHighlightConnectingEdges.Value = HideModeSelector.HighlightEdges;
            bHighlightConnectingEdges.selectionType = HideModeSelector.SelectMultiple;

            // Group for buttons
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.AddProperty(bHideAll);
            group.AddProperty(bHideIncoming);
            group.AddProperty(bHideOutgoing);
            group.AddProperty(bHideForwardTransitiveClosure);
            group.AddProperty(bHideBackwardTransitiveClosure);
            group.AddProperty(bHideTransitiveClosure);
            group.AddProperty(bHideSelected);
            group.AddProperty(bHideUnselected);
            group.AddProperty(bHideAllEdgesOfSelected);
            group.AddProperty(bHighlightConnectingEdges);

            // Register listeners for buttons
            bHideAll.OnSelected.AddListener(() => SetMode(bHideAll.hideMode, bHideAll.selectionType));
            bHideIncoming.OnSelected.AddListener(() => SetMode(bHideIncoming.hideMode, bHideIncoming.selectionType));
            bHideOutgoing.OnSelected.AddListener(() => SetMode(bHideOutgoing.hideMode, bHideOutgoing.selectionType));
            bHideForwardTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideForwardTransitiveClosure.hideMode, bHideForwardTransitiveClosure.selectionType));
            bHideBackwardTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideBackwardTransitiveClosure.hideMode, bHideForwardTransitiveClosure.selectionType));
            bHideTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideTransitiveClosure.hideMode, bHideTransitiveClosure.selectionType));
            bHideSelected.OnSelected.AddListener(() => SetMode(bHideSelected.hideMode, bHideSelected.selectionType));
            bHideUnselected.OnSelected.AddListener(() => SetMode(bHideUnselected.hideMode, bHideUnselected.selectionType));
            bHideAllEdgesOfSelected.OnSelected.AddListener(() => SetMode(bHideAllEdgesOfSelected.hideMode, bHideAllEdgesOfSelected.selectionType));
            bHighlightConnectingEdges.OnSelected.AddListener(() => SetMode(bHighlightConnectingEdges.hideMode, bHighlightConnectingEdges.selectionType));

            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select mode";
            propertyDialog.Description = "Select hide mode";
            propertyDialog.AddGroup(group);

            // Register listeners for dialog
            propertyDialog.OnConfirm.AddListener(OKButtonPressed);
            propertyDialog.OnCancel.AddListener(CancelButtonPressed);

            SEEInput.KeyboardShortcutsEnabled = false;
            // Go online
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// Opens the appropriate dialogues for the corresponding selections.
        /// </summary>
        /// <param name="mode">The mode associated with the button pressed</param>
        private void SetMode(HideModeSelector mode, HideModeSelector selectionType)
        {
            switch (selectionType)
            {
                case HideModeSelector.SelectSingle:
                    this.mode = mode;
                    OKButtonPressed();
                    break;
                case HideModeSelector.SelectMultiple:
                    Close();
                    OpenSelectionMenu(mode);
                    break;
                case HideModeSelector.Confirmed:
                    this.mode = mode;
                    OKButtonPressed();
                    break;
                case HideModeSelector.Back:
                    Close();
                    Open();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Notifies all listeners on <see cref="OnCancel"/> and closes the dialog.
        /// </summary>
        private void CancelButtonPressed()
        {
            OnCancel.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
        }

        /// <summary>
        /// Called after selecting the desired <see cref="HideAction"/> function and selecting the desired element(s).
        /// Completes the selection
        /// </summary>
        private void OKButtonPressed()
        {
            OnConfirm.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
        }

        /// <summary>
        /// Destroys all open dialogues and sets them to null
        /// </summary>
        private void Close()
        {
            if (dialog != null)
            {
                Object.Destroy(dialog);
                dialog = null;
            }
            if (selectionDialog != null)
            {
                Object.Destroy(selectionDialog);
                selectionDialog = null;
            }
        }
    }
}
