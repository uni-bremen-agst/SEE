using System;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Architecture;
using SEE.DataModel.DG;
using SEE.Game.UI.PropertyDialog;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Game.UI.Architecture
{
    /// <summary>
    /// Component that handles the context menu of a node.
    /// Used by Prefabs/Architecture/UI/NodeContextMenuHolder
    /// </summary>
    public class NodeContextMenuHolder : MonoBehaviour
    {
        
        /// <summary>
        /// The delete action button.
        /// </summary>
        public ButtonManagerBasicIcon DeleteCommand;
        /// <summary>
        /// The edit action button.
        /// </summary>
        public ButtonManagerBasicIcon EditCommand;
        /// <summary>
        /// The context menu UI element that this component holds.
        /// </summary>
        public GameObject contextMenu;
        /// <summary>
        /// The node element this context menu is shown for
        /// </summary>
        private GameObject element;
        
        public delegate void DeleteNodeConfirmation(GameObject graphElement);
        public static event DeleteNodeConfirmation OnDeleteNodeConfirmation;
        
        /// <summary>
        ///  The graph element this context menu is shown for. Necessary for the <see cref="NodePropertyDialog"/>.
        /// </summary>
        private Node node;

        private void Start()
        {
            EditCommand.clickEvent.AddListener(OnEditCommand);
            DeleteCommand.clickEvent.AddListener(OnDeleteCommand);
            SelectArchitectureAction.OnSpawnContextMenu += OnSpawnContextMenu;
            SelectArchitectureAction.OnHideContextMenu += () => contextMenu.SetActive(false);
        }

        // <summary>
        /// Event listener method for the <see cref="SelectArchitectureAction.OnSpawnContextMenu"/> event.
        /// Precondition: The <paramref name="graphelement"/> has an <see cref="NodeRef"/>.
        /// </summary>
        /// <param name="graphelement">The selected edge</param>
        private void OnSpawnContextMenu(GameObject graphelement)
        {
            
            if (graphelement.TryGetNode(out Node node))
            {
                transform.position = Pen.current.position.ReadValue();
                element = graphelement;
                this.node = node;
                contextMenu.SetActive(true);
            }
            
        }
        
        /// <summary>
        /// Listener for the OnClick event from <see cref="DeleteCommand"/>. Opens the delete confirmation dialog.
        /// </summary>
        public void OnDeleteCommand()
        {
            OnDeleteNodeConfirmation?.Invoke(element);
            contextMenu.SetActive(false);
        }

        /// <summary>
        /// Listener for the OnClick event from <see cref="EditCommandCommand"/>. Opens the delete confirmation dialog.
        /// </summary>
        public void OnEditCommand()
        {
            if (node != null)
            {
                NodePropertyDialog dialog = new NodePropertyDialog(node);
                dialog.Open();
                contextMenu.SetActive(false);
            }
            else
            {
                throw new Exception($"Cant open edit dialog. No graph Node was found for {element.name}");
            }
            
        }
    }
}