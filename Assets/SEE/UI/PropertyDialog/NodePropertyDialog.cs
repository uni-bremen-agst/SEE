using SEE.Controls;
using SEE.Controls.Actions;
/// Reference in comment.
using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.PropertyDialog
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
        public readonly UnityEvent OnConfirm = new();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnCancel = new();

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
        private SelectionProperty nodeType;

        /// <summary>
        /// The last chosen node type for the <see cref="AddNodeAction"/> mode.
        /// </summary>
        private static string lastUsed = string.Empty;

        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        /// <param name="useLastUsed">Whether the last used index should be used and updated.</param>
        public void Open(bool useLastUsed = false)
        {
            dialog = new GameObject("Node attributes");

            // Name of the node
            nodeName = dialog.AddComponent<StringProperty>();
            nodeName.Name = "Node name";
            nodeName.Value = node.SourceName;
            nodeName.Description = "Name of the node";

            if (!node.HasRootToogle())
            {
                // Type of the node
                nodeType = dialog.AddComponent<SelectionProperty>();
                nodeType.Name = "Node type";

                nodeType.AddOptions(GetNonRootTypes().OrderBy(t => t));
                if (!useLastUsed || string.IsNullOrEmpty(lastUsed)
                    || !node.GameObject().ContainingCity().NodeTypes.Types.Contains(lastUsed))
                {
                    nodeType.Value = node.Type;
                }
                else if (useLastUsed)
                {
                    if (node.Type != lastUsed)
                    {
                        GameNodeEditor.ChangeType(node, lastUsed);
                        new EditNodeNetAction(node.ID, node.SourceName, lastUsed).Execute();
                    }
                    nodeType.Value = lastUsed;
                }
                nodeType.Description = "Type of the node";
            }

            // Group for node name and type
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Node attributes";
            group.AddProperty(nodeName);
            if (nodeType != null)
            {
                group.AddProperty(nodeType);
            }

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

            /// <summary>
            /// Sets the attributes of <see cref="node"/> to the trimmed values entered in the dialog,
            /// notifies all listeners on <see cref="OnConfirm"/>, and closes the dialog.
            /// </summary>
            void OKButtonPressed()
            {
                GameNodeEditor.ChangeName(node, nodeName.Value);
                if (nodeType != null)
                {
                    GameNodeEditor.ChangeType(node, nodeType.Value);
                }

                /// Updates the <see cref="lastUsed"/> attribute.
                if (useLastUsed)
                {
                    lastUsed = nodeType.Value;
                }
                OnConfirm.Invoke();
                SEEInput.KeyboardShortcutsEnabled = true;
                Close();
            }
        }

        /// <summary>
        /// Returns the node types of the graph
        /// except for the root types.
        /// </summary>
        /// <returns>The node types execpt the root types.</returns>
        private ISet<string> GetNonRootTypes()
        {
            ISet<string> types = node.GameObject().ContainingCity().NodeTypes.Types;
            types.Remove(Graph.RootType);
            types.Remove(ReflexionGraph.ArchitectureType);
            types.Remove(ReflexionGraph.ImplementationType);
            return types;
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
        /// Destroys <see cref="dialog"/>. <see cref="dialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            Destroyer.Destroy(dialog);
            dialog = null;
        }
    }
}
