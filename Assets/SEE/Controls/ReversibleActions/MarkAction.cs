using System.Collections.Generic;
using SEE.Extensions;
using UnityEngine;

using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action for marking or unmarking a game node.
    /// </summary>

    internal class MarkAction : AbstractPlayerAction
    {
        private GameObject gameNode;
        private bool wasMarked;

        public override bool Update()
        {
            if (UserSetting.IsDesktop
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef graphElementRef,
                    false) == HitGraphElement.Node
                && graphElementRef.gameObject.TryGetComponent(out InteractableObject interactableObject)
                && interactableObject.IsInteractable(raycastHit.point))
            {
                ExecuteMarking(graphElementRef.gameObject);
                return true;
            }

            if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit _))
            {
                XRSEEActions.Selected = false;

                ExecuteMarking(InteractableObject.HoveredObjectWithWorldFlag.gameObject);
                return true;
            }

            return false;
        }

        private void ExecuteMarking(GameObject target)
        {
            gameNode = target;
            wasMarked = GameNodeMarker.IsMarked(gameNode);

            bool marked = !wasMarked;

            GameNodeMarker.SetMarked(gameNode, marked);
            new MarkNetAction(gameNode.name, marked).Execute();

            CurrentState = IReversibleAction.Progress.Completed;
        }

        public override void Undo()
        {
            base.Undo();

            if (gameNode != null)
            {
                GameNodeMarker.SetMarked(gameNode, wasMarked);
                new MarkNetAction(gameNode.name, wasMarked).Execute();
            }
        }

        public override void Redo()
        {
            base.Redo();

            if (gameNode != null)
            {
                bool marked = !wasMarked;

                GameNodeMarker.SetMarked(gameNode, marked);
                new MarkNetAction(gameNode.name, marked).Execute();
            }
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
            HashSet<string> changedObjects = new();

            if (gameNode != null)
            {
                changedObjects.Add(gameNode.name);
            }

            return changedObjects;
        }
    }
}