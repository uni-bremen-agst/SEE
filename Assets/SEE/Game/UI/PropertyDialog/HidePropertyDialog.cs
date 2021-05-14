using System.Collections;
using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.Events;
using SEE.Controls.Actions;
using SEE.Game.UI.StateIndicator;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A dialogue to select which objects should be hidden in the graph and how.
    /// </summary>
    public class HidePropertyDialog
    {
        /// <summary>
        /// Is used to select the actual HideMode.
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
        /// The dialog used to select single or multiple selection.
        /// </summary>
        private GameObject selectionDialog;

        /// <summary>
        /// Dialog providing all functions for multiple selection
        /// </summary>
        private GameObject multipleDialog;

        /// <summary>
        /// Dialogue providing all functions for single selection
        /// </summary>
        private GameObject singleDialog;

        /// <summary>
        /// Used to select multiple elements in a graph.
        /// </summary>
        private GameObject selection;

        /// <summary>
        /// Shows the current HideMode
        /// </summary>
        private HideStateIndicator indicator;

        /// <summary>
        /// Represents the Selected HideMode
        /// </summary>
        public HideInInspector selectedMode;


        /// <summary>
        /// Button to select the single selection menu
        /// </summary>
        private ButtonProperty bOpenSingleSelectionMenu;

        /// <summary>
        /// Button to select the multiple selection menue
        /// </summary>
        private ButtonProperty bOpenMultipleSelectionMenu;

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
        /// Creates a menu to select multiple elements from a graph
        /// </summary>
        private void OpenSelectionMenu()
        {
            // Creating a new selection
            selection = new GameObject("Indicator");
            indicator = selection.AddComponent<HideStateIndicator>();
            indicator.buttonName = "Done selecting";
            indicator.AnchorMin = Vector2.zero;
            indicator.AnchorMax = Vector2.zero;
            indicator.Pivot = Vector2.zero;
            indicator.ChangeState("Select Objects");

            // Register listeners for selection menu
            indicator.OnSelected.AddListener(() => SetMode(indicator.hideMode));
        }

        /// <summary>
        /// Opens a new dialogue that asks whether you want to select only one or several elements (for a better overview).
        /// </summary>
        public void Open()
        {
            // Creating a new dialog
            selectionDialog = new GameObject("HideAction mode selector");

            // Create new buttons 
            bOpenSingleSelectionMenu = selectionDialog.AddComponent<ButtonProperty>();
            bOpenSingleSelectionMenu.Name = "Single Selection";
            bOpenSingleSelectionMenu.Description = "Select objects";
            bOpenSingleSelectionMenu.Value = HideModeSelector.SelectSingleHide;

            bOpenMultipleSelectionMenu = selectionDialog.AddComponent<ButtonProperty>();
            bOpenMultipleSelectionMenu.Name = "Multiple Selection";
            bOpenMultipleSelectionMenu.Description = "Select objects";
            bOpenMultipleSelectionMenu.Value = HideModeSelector.SelectMultipleHide;

            // Group for buttons
            PropertyGroup group = selectionDialog.AddComponent<PropertyGroup>();
            group.AddProperty(bOpenSingleSelectionMenu);
            group.AddProperty(bOpenMultipleSelectionMenu);

            // Register listeners for buttons
            bOpenSingleSelectionMenu.OnSelected.AddListener(() => SetMode(bOpenSingleSelectionMenu.hideMode));
            bOpenMultipleSelectionMenu.OnSelected.AddListener(() => SetMode(bOpenMultipleSelectionMenu.hideMode));

            // Dialog
            PropertyDialog propertyDialog = selectionDialog.AddComponent<PropertyDialog>();
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
        /// Provides all possible functions that are available for the selection of a single element.
        /// </summary>
        public void OpenSinge()
        {
            // Creating a new dialog
            singleDialog = new GameObject("HideAction mode selector");

            // Create new buttons 
            bHideAll = singleDialog.AddComponent<ButtonProperty>();
            bHideAll.Name = "Hide all";
            bHideAll.Description = "Hides everything";
            bHideAll.Value = HideModeSelector.HideAll;

            bHideIncoming = singleDialog.AddComponent<ButtonProperty>();
            bHideIncoming.Name = "Hide incoming";
            bHideIncoming.Description = "Hides only incoming edges";
            bHideIncoming.Value = HideModeSelector.HideIncoming;

            bHideOutgoing = singleDialog.AddComponent<ButtonProperty>();
            bHideOutgoing.Name = "Hide outgoing";
            bHideOutgoing.Description = "Beschreibung";
            bHideOutgoing.Value = HideModeSelector.HideOutgoing;

            bHideForwardTransitiveClosure = singleDialog.AddComponent<ButtonProperty>();
            bHideForwardTransitiveClosure.Name = "Hide forward transitive closure";
            bHideForwardTransitiveClosure.Description = "Beschreibung";
            bHideForwardTransitiveClosure.Value = HideModeSelector.HideForwardTransitiveClosure;

            bHideBackwardTransitiveClosure = singleDialog.AddComponent<ButtonProperty>();
            bHideBackwardTransitiveClosure.Name = "Hide backward transitive closure";
            bHideBackwardTransitiveClosure.Description = "Beschreibung";
            bHideBackwardTransitiveClosure.Value = HideModeSelector.HideBackwardTransitiveClosure;

            bHideTransitiveClosure = singleDialog.AddComponent<ButtonProperty>();
            bHideTransitiveClosure.Name = "Hide transitive closure";
            bHideTransitiveClosure.Description = "Beschreibung";
            bHideTransitiveClosure.Value = HideModeSelector.HideAllTransitiveClosure;

            // Group for buttons
            PropertyGroup group = singleDialog.AddComponent<PropertyGroup>();
            group.AddProperty(bHideAll);
            group.AddProperty(bHideIncoming);
            group.AddProperty(bHideOutgoing);
            group.AddProperty(bHideForwardTransitiveClosure);
            group.AddProperty(bHideBackwardTransitiveClosure);
            group.AddProperty(bHideTransitiveClosure);

            // Register listeners for buttons
            bHideAll.OnSelected.AddListener(() => SetMode(bHideAll.hideMode));
            bHideIncoming.OnSelected.AddListener(() => SetMode(bHideIncoming.hideMode));
            bHideOutgoing.OnSelected.AddListener(() => SetMode(bHideOutgoing.hideMode));
            bHideForwardTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideForwardTransitiveClosure.hideMode));
            bHideBackwardTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideBackwardTransitiveClosure.hideMode));
            bHideTransitiveClosure.OnSelected.AddListener(() => SetMode(bHideTransitiveClosure.hideMode));

            // Dialog
            PropertyDialog propertyDialog = singleDialog.AddComponent<PropertyDialog>();
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
        /// Provides all possible functions that are available for the selection of multiple elements.
        /// </summary>
        private void OpenMultiple()
        {
            // Creating a new dialog
            multipleDialog = new GameObject("HideAction mode selector");

            // Create new buttons
            bHideSelected = multipleDialog.AddComponent<ButtonProperty>();
            bHideSelected.Name = "Hide selected";
            bHideSelected.Description = "Hides only the selected objects";
            bHideSelected.Value = HideModeSelector.HideSelected;

            bHideUnselected = multipleDialog.AddComponent<ButtonProperty>();
            bHideUnselected.Name = "Hide unselected";
            bHideUnselected.Description = "Hides only the unselected objects";
            bHideUnselected.Value = HideModeSelector.HideUnselected;

            bHideAllEdgesOfSelected = multipleDialog.AddComponent<ButtonProperty>();
            bHideAllEdgesOfSelected.Name = "Hide all edges of selected";
            bHideAllEdgesOfSelected.Description = "Beschreibung";
            bHideAllEdgesOfSelected.Value = HideModeSelector.HideAllEdgesOfSelected;

            bHighlightConnectingEdges = multipleDialog.AddComponent<ButtonProperty>();
            bHighlightConnectingEdges.Name = "Highlight connection Edges";
            bHighlightConnectingEdges.Description = "Beschreibung";
            bHighlightConnectingEdges.Value = HideModeSelector.HighlightEdges;

            // Group for node name and type
            PropertyGroup group = multipleDialog.AddComponent<PropertyGroup>();
            group.AddProperty(bHideSelected);
            group.AddProperty(bHideUnselected);
            group.AddProperty(bHideAllEdgesOfSelected);
            group.AddProperty(bHighlightConnectingEdges);

            bHideSelected.OnSelected.AddListener(() => SetMode(bHideSelected.hideMode));
            bHideUnselected.OnSelected.AddListener(() => SetMode(bHideUnselected.hideMode));
            bHideAllEdgesOfSelected.OnSelected.AddListener(() => SetMode(bHideAllEdgesOfSelected.hideMode));
            bHighlightConnectingEdges.OnSelected.AddListener(() => SetMode(bHighlightConnectingEdges.hideMode));

            // Dialog
            PropertyDialog propertyDialog = multipleDialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Select mode";
            propertyDialog.Description = "Select hide mode";
            propertyDialog.AddGroup(group);

            // Register listeners
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
        private void SetMode(HideModeSelector mode)
        {
            switch (mode)
            {
                case HideModeSelector.SelectSingleHide:
                    Close();
                    OpenSinge();
                    break;
                case HideModeSelector.SelectMultipleHide:
                    Close();
                    OpenSelectionMenu();
                    break;
                case HideModeSelector.Select:
                    Close();
                    OpenMultiple();
                    break;
                default:
                    this.mode = mode;
                    OKButtonPressed();
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
        /// Sets the attributes of <see cref="node"/> to the trimmed values entered in the dialog,
        /// notifies all listeners on <see cref="OnConfirm"/>, and closes the dialog.
        /// </summary>
        private void OKButtonPressed()
        {
            OnConfirm.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
        }

        /// <summary>
        /// Destroys <see cref="selectionDialog"/>. <see cref="selectionDialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            if (selectionDialog != null)
            {
                Object.Destroy(selectionDialog);
                selectionDialog = null;
            }
            if (singleDialog != null)
            {
                Object.Destroy(singleDialog);
                singleDialog = null;
            }
            if (multipleDialog != null)
            {
                Object.Destroy(multipleDialog);
                multipleDialog = null;
            }
            if (selection != null)
            {
                Object.Destroy(selection);
                selection = null;
            }
        }
    }
}
