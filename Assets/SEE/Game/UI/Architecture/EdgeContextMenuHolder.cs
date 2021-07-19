using System;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Architecture;
using SEE.DataModel.DG;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Game.UI.Architecture
{
    /// <summary>
    /// Component that handles the context menu of an edge.
    /// Used by Prefabs/Architecture/UI/EdgeContextMenuHolder
    /// </summary>
    public class EdgeContextMenuHolder: MonoBehaviour
    {
        
        /// <summary>
        /// The delete action button.
        /// </summary>
        public ButtonManagerBasicIcon DeleteCommand;
        /// <summary>
        /// The edge element this context menu is shown for.
        /// </summary>
        private GameObject element;
        /// <summary>
        /// The context menu UI element that this component holds.
        /// </summary>
        public GameObject contextMenu;
        
        public delegate void DeleteEdgeConfirmation(GameObject graphElement);

        public static event DeleteEdgeConfirmation OnDeleteEdgeConfirmation;
        
        private void OnEnable()
        {
            SelectArchitectureAction.OnSpawnContextMenu += OnSpawnContextMenu;
            SelectArchitectureAction.OnHideContextMenu += OnHideContextMenu;
        }
        private void OnDisable()
        {
            SelectArchitectureAction.OnSpawnContextMenu -= OnSpawnContextMenu;
            SelectArchitectureAction.OnHideContextMenu -= OnHideContextMenu;
        }
        private void OnHideContextMenu()
        {
            contextMenu.SetActive(false);
        }
        
        private void Start()
        {
            SelectArchitectureAction.OnSpawnContextMenu += OnSpawnContextMenu;
            SelectArchitectureAction.OnHideContextMenu += OnHideContextMenu;
            DeleteCommand.clickEvent.AddListener(OnDeleteCommand);
        }

        private void OnDestroy()
        {
            SelectArchitectureAction.OnSpawnContextMenu -= OnSpawnContextMenu;
            SelectArchitectureAction.OnHideContextMenu -= OnHideContextMenu;
        }

        /// <summary>
        /// Event listener method for the <see cref="SelectArchitectureAction.OnSpawnContextMenu"/> event.
        /// Precondition: The <paramref name="graphelement"/> has an <see cref="EdgeRef"/>.
        /// </summary>
        /// <param name="graphelement">The selected edge</param>
        private void OnSpawnContextMenu(GameObject graphelement)
        {
            if (graphelement.HasEdgeRef())
            {
                this.transform.position = Pen.current.position.ReadValue();
                this.element = graphelement;
                contextMenu.SetActive(true);
                return;
            }
            contextMenu.SetActive(false);
        }

        /// <summary>
        /// Listener for the OnClick event from <see cref="DeleteCommand"/>. Opens the delete confirmation dialog.
        /// </summary>
        private void OnDeleteCommand()
        {
            OnDeleteEdgeConfirmation?.Invoke(element);
            contextMenu.SetActive(false);
        }
    }
}