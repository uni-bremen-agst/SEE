using Microsoft.MixedReality.Toolkit;
using SEE.Controls.Architecture;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.Actions.Architecture
{
    /// <summary>
    /// Implementation of <see cref="AbstractArchitectureAction"/> for selecting graph elements.
    /// </summary>
    public class SelectArchitectureAction : AbstractArchitectureAction
    {
        public override ArchitectureActionType GetActionType()
        {
            return ArchitectureActionType.Select;
        }


        public static AbstractArchitectureAction NewInstance()
        {
            return new SelectArchitectureAction();
        }

        
        /// <summary>
        /// The selected object. It has a <see cref="InteractableObject"/> component attached.
        /// </summary>
        private GameObject selectedObject;

        public delegate void SpawnContextMenu(GameObject graphElement);

        public delegate void HideContextMenu();
        
        
        public static event SpawnContextMenu OnSpawnContextMenu;
        public static event HideContextMenu OnHideContextMenu;
        
        public override void Start()
        {
            PenInteractionController.ObjectPrimaryClicked += ObjectPriamryClicked;
        }

        private void ObjectPriamryClicked(ObjectPrimaryClicked data)
        {
            if (selectedObject != null)
            {
                OnHideContextMenu?.Invoke();
                selectedObject = null;
            }
            GameObject go = data.Object;
            if (go.TryGetNode(out Node node))
            {
                selectedObject = go;
                OnSpawnContextMenu?.Invoke(go);
            }else if (go.TryGetEdge(out Edge edge))
            {
                selectedObject = go;
                OnSpawnContextMenu?.Invoke(go);
            }
            else
            {
                OnHideContextMenu?.Invoke();
            }
        }


        public override void Stop()
        {
            PenInteractionController.ObjectPrimaryClicked -= ObjectPriamryClicked;
            selectedObject = null;
            OnHideContextMenu?.Invoke();
        }
    }
}