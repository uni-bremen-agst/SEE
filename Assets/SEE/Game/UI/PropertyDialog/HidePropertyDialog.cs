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
    /// A dialog to enter the source name and type of a graph node.
    /// </summary>
    public class HidePropertyDialog
    {
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
        /// The dialog used to manipulate the node.
        /// </summary>
        private GameObject dialog;

        private GameObject selection;

        private HideStateIndicator indicator;

        /// <summary>
        /// The dialog property for the name of the node to be entered in the dialog.
        /// </summary>
        ///

        private ButtonProperty fdb1;
        private ButtonProperty fdb2;

        private ButtonProperty sdb1;
        private ButtonProperty sdb2;
        private ButtonProperty sdb3;
        private ButtonProperty sdb4;
        private ButtonProperty sdb5;
        private ButtonProperty sdb6;

        private ButtonProperty mdb1;
        private ButtonProperty mdb2;
        private ButtonProperty mdb3;
        private ButtonProperty mdb4;

        public HideInInspector selectedMode;


        public void OpenSelectionMenu()
        {
            selection = new GameObject("Indicator");
            indicator = selection.AddComponent<HideStateIndicator>();
            indicator.buttonName = "Done Selection";
            indicator.AnchorMin = Vector2.zero;
            indicator.AnchorMax = Vector2.zero;
            indicator.Pivot = Vector2.zero;
            indicator.ChangeState("Select Objects");

            indicator.OnSelected.AddListener(() => SetMode(indicator.hideMode));
        }

        public void Open()
        {
            dialog = new GameObject("Hideaction mode selector");

            fdb1 = dialog.AddComponent<ButtonProperty>();
            fdb1.Name = "Single Selection";
            fdb1.Description = "Select objects";
            fdb1.Value = HideModeSelector.SelectSingleHide;

            fdb2 = dialog.AddComponent<ButtonProperty>();
            fdb2.Name = "Multiple Selection";
            fdb2.Description = "Select objects";
            fdb2.Value = HideModeSelector.SelectMultipleHide;

            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.AddProperty(fdb1);
            group.AddProperty(fdb2);

            fdb1.OnSelected.AddListener(() => SetMode(fdb1.hideMode));
            fdb2.OnSelected.AddListener(() => SetMode(fdb2.hideMode));

            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
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


        public void OpenSinge()
        {
            dialog = new GameObject("Hideaction mode selector");

            sdb1 = dialog.AddComponent<ButtonProperty>();
            sdb1.Name = "Hide all";
            sdb1.Description = "Hides everything";
            sdb1.Value = HideModeSelector.HideAll;

            sdb2 = dialog.AddComponent<ButtonProperty>();
            sdb2.Name = "Hide incoming";
            sdb2.Description = "Hides only incoming edges";
            sdb2.Value = HideModeSelector.HideIncoming;

            sdb3 = dialog.AddComponent<ButtonProperty>();
            sdb3.Name = "Hide outgoing";
            sdb3.Description = "Beschreibung";
            sdb3.Value = HideModeSelector.HideOutgoing;

            sdb4 = dialog.AddComponent<ButtonProperty>();
            sdb4.Name = "Hide forward transitive closure";
            sdb4.Description = "Beschreibung";
            sdb4.Value = HideModeSelector.HideForwardTransitveClosure;

            sdb5 = dialog.AddComponent<ButtonProperty>();
            sdb5.Name = "Hide backward transitive closure";
            sdb5.Description = "Beschreibung";
            sdb5.Value = HideModeSelector.HideBackwardTransitiveClosure;

            sdb6 = dialog.AddComponent<ButtonProperty>();
            sdb6.Name = "Hide transitive closure";
            sdb6.Description = "Beschreibung";
            sdb6.Value = HideModeSelector.HideAllTransitiveClosure;

            // Group for node name and type
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.AddProperty(sdb1);
            group.AddProperty(sdb2);
            group.AddProperty(sdb3);

            group.AddProperty(sdb4);
            group.AddProperty(sdb5);
            group.AddProperty(sdb6);

            sdb1.OnSelected.AddListener(() => SetMode(sdb1.hideMode));
            sdb2.OnSelected.AddListener(() => SetMode(sdb2.hideMode));
            sdb3.OnSelected.AddListener(() => SetMode(sdb3.hideMode));
            sdb4.OnSelected.AddListener(() => SetMode(sdb4.hideMode));
            sdb5.OnSelected.AddListener(() => SetMode(sdb5.hideMode));
            sdb6.OnSelected.AddListener(() => SetMode(sdb6.hideMode));

            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
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
        /// Creates and opens the dialog.
        /// </summary>
        public void OpenMultiple()
        {
            dialog = new GameObject("Hideaction mode selector");

            mdb1 = dialog.AddComponent<ButtonProperty>();
            mdb1.Name = "Hide selected";
            mdb1.Description = "Hides only the selected objects";
            mdb1.Value = HideModeSelector.HideSelected;

            mdb2 = dialog.AddComponent<ButtonProperty>();
            mdb2.Name = "Hide unselceted";
            mdb2.Description = "Hides only the unselected objects";
            mdb2.Value = HideModeSelector.HideUnselected;

            mdb3 = dialog.AddComponent<ButtonProperty>();
            mdb3.Name = "Hide all edges of selected";
            mdb3.Description = "Beschreibung";
            mdb3.Value = HideModeSelector.HideAllEdgesOfSelected;

            mdb4 = dialog.AddComponent<ButtonProperty>();
            mdb4.Name = "Highlight connection Edges";
            mdb4.Description = "Beschreibung";
            mdb4.Value = HideModeSelector.HighlightEdges;

            // Group for node name and type
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.AddProperty(mdb1);
            group.AddProperty(mdb2);
            group.AddProperty(mdb3);
            group.AddProperty(mdb4);

            mdb1.OnSelected.AddListener(() => SetMode(mdb1.hideMode));
            mdb2.OnSelected.AddListener(() => SetMode(mdb2.hideMode));
            mdb3.OnSelected.AddListener(() => SetMode(mdb3.hideMode));
            mdb4.OnSelected.AddListener(() => SetMode(mdb4.hideMode));

            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
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

        void SetMode(HideModeSelector mode)
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
        /// Destroys <see cref="dialog"/>. <see cref="dialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            Object.Destroy(dialog);
            dialog = null;
            Object.Destroy(selection);
            selection = null;
        }
    }
}
