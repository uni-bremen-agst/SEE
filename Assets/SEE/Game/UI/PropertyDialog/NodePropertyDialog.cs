using SEE.Controls;
using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A dialog to enter the source name and type of a graph node.
    /// </summary>
    public class NodePropertyDialog
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the graph node to be manipulated</param>
        public NodePropertyDialog(Node node)
        {
            this.node = node;
        }

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
        /// The graph node to be manipulated by this dialog.
        /// </summary>
        private readonly Node node;

        /// <summary>
        /// The dialog used to manipulate the node.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The dialog property for the name of the node to be entered in the dialog.
        /// </summary>
        private StringProperty nodeName;

        /// <summary>
        /// The dialog property for the type of the node to be entered in the dialog.
        /// </summary>
        private StringProperty nodeType;

        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            dialog = new GameObject("Node attributes");

            // Name of the node
            nodeName = dialog.AddComponent<StringProperty>();
            nodeName.Name = "Node name";
            nodeName.Value = node.SourceName;
            nodeName.Description = "Name of the node";

            // Type of the node
            nodeType = dialog.AddComponent<StringProperty>();
            nodeType.Name = "Node type";
            nodeType.Value = node.Type;
            nodeType.Description = "Type of the node";

            // Group for node name and type
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Node attributes";
            group.AddProperty(nodeName);
            group.AddProperty(nodeType);

            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Node attributes";
            propertyDialog.Description = "Enter the node attributes";
            propertyDialog.AddGroup(group);

            // Register listeners
            propertyDialog.OnConfirm.AddListener(OKButtonPressed);
            propertyDialog.OnCancel.AddListener(CancelButtonPressed);

            SEEInput.KeyboardShortcutsEnabled = false;
            // Go online
            propertyDialog.DialogShouldBeShown = true;
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
            node.SourceName = nodeName.Value.Trim();
            node.Type = nodeType.Value.Trim();
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
