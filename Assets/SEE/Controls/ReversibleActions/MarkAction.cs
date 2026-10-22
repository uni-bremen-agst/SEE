using System;
using System.Collections.Generic;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.Extensions;
using SEE.XR;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    public class MarkAction : AbstractPlayerAction
    {
        private enum ProgressState
        {
            NoNodeSelected,
            Finish
        }
        private ProgressState progress = ProgressState.NoNodeSelected;
        private GameObject markedGameNode;

        private Memento memento;
        private struct Memento
        {
            public bool WasMarked;
            public string NodeID;
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.NewMarker;
        }

        public override bool Update()
        {
            bool result = false;
            switch (progress)
            {
                case ProgressState.NoNodeSelected:
                {
                    GameObject selectedNode = null;

                    if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                                              && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                                              && ger.gameObject.TryGetComponent(out InteractableObject io)
                                              && io.IsInteractable(raycastHit.point))
                    {
                        selectedNode = ger.gameObject;
                    }
                    else if (UserSetting.IsVR
                             && XRSEEActions.Selected
                             && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                             && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
                    {
                        selectedNode = InteractableObject.HoveredObjectWithWorldFlag.gameObject;
                        XRSEEActions.Selected = false;
                    }

                    if (selectedNode != null)
                    {
                        memento.NodeID = selectedNode.name;
                        memento.WasMarked = GameNodeMarker.IsMarked(selectedNode);
                        bool shouldMark = !memento.WasMarked;
                        SetMarked(selectedNode, shouldMark);
                        progress = ProgressState.Finish;
                    }
                    break;
                }
        
                case ProgressState.Finish:
                    result = true;
                    CurrentState = IReversibleAction.Progress.Completed;
                    break;
                default:
                    throw new NotImplementedException($"Unhandled case {nameof(progress)}.");
            }
            return result;
        }
        public override void Undo()
        {
            base.Undo();
            GameObject node = GraphElementIDMap.Find(memento.NodeID);
            if (node != null)
            {
                SetMarked(node, memento.WasMarked);
            }
        }
        public override void Redo()
        {
            base.Redo();
            GameObject node = GraphElementIDMap.Find(memento.NodeID);
            if (node != null)
            {
                SetMarked(node, !memento.WasMarked);
            }
        }

        /// <summary>
        /// Applies the requested marking state locally and replicates it to the other clients.
        /// </summary>
        private void SetMarked(GameObject node, bool shouldMark)
        {
            if (shouldMark)
            {
                markedGameNode = GameNodeMarker.MarkNode(node);
            }
            else
            {
                GameNodeMarker.UnmarkNode(node);
                markedGameNode = null;
            }
            new MarkNetAction(node.name, shouldMark).Execute();
        }

        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }
        
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.NodeID
            };
        }
        
    }
}
