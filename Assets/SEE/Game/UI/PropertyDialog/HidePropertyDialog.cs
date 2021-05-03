using System.Collections;
using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.Events;
using SEE.Controls.Actions;

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

        /// <summary>
        /// The dialog property for the name of the node to be entered in the dialog.
        /// </summary>
        private ButtonProperty b1;
        private ButtonProperty b2;
        private ButtonProperty b5;
        private ButtonProperty b4;
        private ButtonProperty b3;
        private ButtonProperty b6;
        private ButtonProperty b7;
        private ButtonProperty b8;
        private ButtonProperty b9;

        public HideInInspector selectedMode;


        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            dialog = new GameObject("Hideaction mode selector");

            // Name of the node

            b1 = dialog.AddComponent<ButtonProperty>();
            b1.Name = "Hide all";
            b1.Description = "Hides everything";
            b1.Value = HideModeSelector.HideAll;

            b2 = dialog.AddComponent<ButtonProperty>();
            b2.Name = "Hide selected";
            b2.Description = "Hides only the selected objects";
            b2.Value = HideModeSelector.HideSelected;

            b3 = dialog.AddComponent<ButtonProperty>();
            b3.Name = "Hide unselceted";
            b3.Description = "Hides only the unselected objects";
            b3.Value = HideModeSelector.HideUnselected;

            b4 = dialog.AddComponent<ButtonProperty>();
            b4.Name = "Hide incoming";
            b4.Description = "Hides only incoming edges";
            b4.Value = HideModeSelector.HideIncoming;

            b5 = dialog.AddComponent<ButtonProperty>();
            b5.Name = "Hide outgoing";
            b5.Description = "Beschreibung";
            b5.Value = HideModeSelector.HideOutgoing;

            b6 = dialog.AddComponent<ButtonProperty>();
            b6.Name = "Hide all edges of selected";
            b6.Description = "Beschreibung";
            b6.Value = HideModeSelector.HideAllEdgesOfSelected;

            b7 = dialog.AddComponent<ButtonProperty>();
            b7.Name = "Hide forward transitive closure";
            b7.Description = "Beschreibung";
            b7.Value = HideModeSelector.HideForwardTransitveClosure;

            b8 = dialog.AddComponent<ButtonProperty>();
            b8.Name = "Hide backward transitive closure";
            b8.Description = "Beschreibung";
            b8.Value = HideModeSelector.HideBackwardTransitiveClosure;

            b9 = dialog.AddComponent<ButtonProperty>();
            b9.Name = "Hide transitive closure";
            b9.Description = "Beschreibung";
            b9.Value = HideModeSelector.HideAllTransitiveClosure;

            // Group for node name and type
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.AddProperty(b1);
            group.AddProperty(b2);
            group.AddProperty(b3);
            group.AddProperty(b4);
            group.AddProperty(b5);
            group.AddProperty(b6);
            group.AddProperty(b7);
            group.AddProperty(b8);
            group.AddProperty(b9);

            b1.OnSelected.AddListener(() => SetMode(b1.hideMode));
            b2.OnSelected.AddListener(() => SetMode(b2.hideMode));
            b3.OnSelected.AddListener(() => SetMode(b3.hideMode));
            b4.OnSelected.AddListener(() => SetMode(b4.hideMode));
            b5.OnSelected.AddListener(() => SetMode(b5.hideMode));
            b6.OnSelected.AddListener(() => SetMode(b6.hideMode));
            b7.OnSelected.AddListener(() => SetMode(b7.hideMode));
            b8.OnSelected.AddListener(() => SetMode(b8.hideMode));
            b9.OnSelected.AddListener(() => SetMode(b9.hideMode));



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
            this.mode = mode;
            OKButtonPressed();
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
        }
    }
}
