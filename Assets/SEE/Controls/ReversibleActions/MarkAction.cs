using SEE.Audio;
using SEE.Controls.KeyActions;
using SEE.Extensions;
using SEE.Game.City;
using SEE.GraphElementRefs;
using SEE.Graphs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    internal class MarkAction : AbstractPlayerAction
    {
        private string nodeID;
        private bool previouslyMarked;

        public override bool Update()
        {
            if(UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
               && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef) == HitGraphElement.Node
               && elementRef.gameObject.TryGetComponent(out InteractableObject io)
               && io.IsInteractable(raycastHit.point))
            {
                GameObject node = elementRef.gameObject;
                nodeID = node.name;
                previouslyMarked = GameNodeMarker.IsMarked(node);
                SetMarked(!previouslyMarked);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }

            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                GameObject node = elementRef.gameObject;
                nodeID = node.name;
                previouslyMarked = GameNodeMarker.IsMarked(node);
                SetMarked(!previouslyMarked);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        private void SetMarked(bool marked)
        {
            GameObject node = GraphElementIDMap.Find(nodeID);
            if (node != null)
            {
                GameNodeMarker.SetMarked(node, marked);
                new MarkNetAction(nodeID, marked).Execute();
            }

        }
        public override void Undo()
        {
            base.Undo();
            SetMarked(previouslyMarked);
        }
        public override void Redo()
        {
            base.Redo();
            SetMarked(!previouslyMarked);
        }
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }
        public override HashSet<string> GetChangedObjects()
        {
            return nodeID == null ? new HashSet<string>() : new HashSet<string> { nodeID };
        }
    }

}