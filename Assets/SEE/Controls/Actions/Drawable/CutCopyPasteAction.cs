using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;
using SEE.Game.Drawable.ActionHelpers;
using SEE.UI;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action provides the cut, copy, and paste functionality
    /// for a <see cref="DrawableType"/> object.
    /// </summary>
    public class CutCopyPasteAction : DrawableAction
    {
        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="CutCopyPasteAction"/>.
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            CutCopyPaste,
            OpenSelectParentMenu,
            SelectParent,
            Finish
        }

        /// <summary>
        /// Holds the current state.
        /// </summary>
        private CutCopy state = CutCopy.None;

        /// <summary>
        /// The state of cut or copy is chosen.
        /// </summary>
        private enum CutCopy
        {
            None,
            Cut,
            Copy
        }

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to
        /// revert or repeat a <see cref="CutCopyPasteAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The old values of the drawable type object.
            /// </summary>
            public readonly DrawableType OldValueHolder;
            /// <summary>
            /// The new values of the drawable type object.
            /// </summary>
            public readonly DrawableType NewValueHolder;
            /// <summary>
            /// The drawable surface on which the drawable type object was displayed.
            /// </summary>
            public readonly DrawableConfig OldSurface;
            /// <summary>
            /// The drawable surface on which the drawable type object is now displayed.
            /// </summary>
            public readonly DrawableConfig NewSurface;
            /// <summary>
            /// Holds the original configurations for the children and branches of a mind map node.
            /// </summary>
            public DrawableConfig OldNodesHolder;
            /// <summary>
            /// Holds the new configurations for the children and branches of a mind map node.
            /// </summary>
            public DrawableConfig NewNodesHolder;
            /// <summary>
            /// The old branch line config; will be needed for restore the visual data of it.
            /// </summary>
            public LineConf OldBranchLineConfig;
            /// <summary>
            /// The state if it was copied or cut.
            /// </summary>
            public readonly CutCopy State;

            /// <summary>
            /// The constructor, which simply assigns its parameters to the fields of this class.
            /// </summary>
            /// <param name="oldValueHolder">The old values of the drawable type object.</param>
            /// <param name="newValueHolder">The new edited values of the drawable type object.</param>
            /// <param name="oldSurface">The drawable surface on which the drawable type object was displayed.</param>
            /// <param name="newSurface">The drawable surface on which the drawable type object is displayed.</param>
            /// <param name="cutCopy">The state whether it was cut or copied.</param>
            public Memento(DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject oldSurface, GameObject newSurface, CutCopy cutCopy)
            {
                OldValueHolder = oldValueHolder;
                NewValueHolder = newValueHolder;
                OldSurface = DrawableConfigManager.GetDrawableConfig(oldSurface);
                NewSurface = DrawableConfigManager.GetDrawableConfig(newSurface);
                State = cutCopy;
                OldNodesHolder = null;
                NewNodesHolder = null;
                OldBranchLineConfig = null;
            }
        }

        /// <summary>
        /// The prefab of the cut-copy-paste menu.
        /// </summary>
        private const string cutCopyPasteMenuPrefab = "Prefabs/UI/Drawable/CutCopyPaste";
        /// <summary>
        /// The instance of the cut-copy-paste menu
        /// </summary>
        private GameObject cutCopyPasteMenu;

        /// <summary>
        /// The newly created object.
        /// </summary>
        private GameObject newObject;

        /// <summary>
        /// The selected drawable type object that should be cut or copied.
        /// </summary>
        private GameObject selectedObj;

        /// <summary>
        /// The old selected object of the privous run.
        /// </summary>
        private static GameObject oldSelectedObj;

        /// <summary>
        /// True if the left mouse button was released after finish.
        /// It is necessary to prevent the previously selected object from being accidentally selected again.
        /// After the action has successfully completed, it starts again, allowing for the selection of a new object.
        /// This option enables the immediate selection of another object while pressing the mouse button.
        /// </summary>
        private static bool mouseWasReleased = true;

        /// <summary>
        /// The old values of the selected drawable type.
        /// </summary>
        private DrawableType oldValueHolder;

        /// <summary>
        /// The new values of the selected drawable type.
        /// </summary>
        private DrawableType newValueHolder;

        /// <summary>
        /// The drawable surface where the selected object was displayed.
        /// </summary>
        private GameObject oldSurface;

        /// <summary>
        /// The drawable surface where the new object is displayed.
        /// </summary>
        private GameObject newSurface;

        /// <summary>
        /// The configuration that includes the node children and their branch lines.
        /// Only necessary for interacting with a <see cref="MindMapNodeConf"/>.
        /// </summary>
        private DrawableConfig oldNodesBranchLineHolder = null;
        /// <summary>
        /// The configuration that includes the changed node children and their branch lines.
        /// Only necessary for interacting with a <see cref="MindMapNodeConf"/>.
        /// </summary>
        private DrawableConfig newNodesBranchLineHolder = null;
        /// <summary>
        /// The configuration of the old branch line to parent.
        /// </summary>
        private LineConf oldBranchLineConf = null;
        /// <summary>
        /// Query that ensures the visual data from the previous branch line is taken over.
        /// </summary>
        private bool editToOldBranchLine = false;

        /// <summary>
        /// Resets the old selected object, if the action state will be left.
        /// </summary>
        public static void Reset()
        {
            oldSelectedObj = null;
            mouseWasReleased = true;
        }

        /// <summary>
        /// Deactivates the blink effect if it is still active and destroys the
        /// cut-copy-paste menu.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            BlinkEffect.Deactivate(selectedObj);

            if (cutCopyPasteMenu != null)
            {
                Destroyer.Destroy(cutCopyPasteMenu);
            }
        }

        /// <summary>
        /// Adds the necessary handler for the cut, copy and paste button.
        /// </summary>
        /// <param name="menu">The instance of the cut copy paste menu</param>
        private void SetupButtons(GameObject menu)
        {
            Transform content = menu.transform.Find("Content");
            ButtonManagerBasic cut = content.Find("Cut").GetComponent<ButtonManagerBasic>();
            ButtonManagerBasic copy = content.Find("Copy").GetComponent<ButtonManagerBasic>();

            cut.clickEvent.AddListener(() =>
            {
                state = CutCopy.Cut;
                ShowNotification.Info("Select position", "Choose a suitable position for pasting the cut object.", 2);
            });

            copy.clickEvent.AddListener(() =>
            {
                state = CutCopy.Copy;
                ShowNotification.Info("Select position", "Choose a suitable position for pasting the copied object.", 2);
            });
        }

        /// <summary>
        /// This method manages the player's interaction with the action <see cref="ActionStateType.CutCopyPaste"/>.
        /// It allows to cut or copy drawable type objects and paste them on a specific position on a specific drawable.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            /// Block for canceling the action.
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// Block for selecting the drawable type object to copy/cut.
                    case ProgressState.SelectObject:
                        SelectObject();
                        break;

                    /// Block in which the object is duplicated at the desired location,
                    /// and the original may be deleted if necessary.
                    case ProgressState.CutCopyPaste:
                        CutCopyPaste();
                        break;

                    /// Block in which the selected parent menu will be open.
                    case ProgressState.OpenSelectParentMenu:
                        OpenSelectParent();
                        break;

                    /// Block in which we will wait for the user selection.
                    case ProgressState.SelectParent:
                        SelectParent();
                        break;

                    /// Block to finish this action.
                    case ProgressState.Finish:
                        mouseWasReleased = false;
                        memento = new Memento(oldValueHolder, newValueHolder, oldSurface, newSurface, state)
                        {
                            OldNodesHolder = oldNodesBranchLineHolder,
                            NewNodesHolder = newNodesBranchLineHolder,
                            OldBranchLineConfig = oldBranchLineConf
                        };
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if (selectedObj != null && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                SetToInitialState();
            }
        }

        /// <summary>
        /// Allows the user to choose a Drawable Type Object for copying/cutting.
        /// It activates the blink effect and enables the cut-copy-paste menu.
        /// </summary>
        private void SelectObject()
        {
            if (Selector.SelectObject(ref selectedObj, ref oldSelectedObj, ref mouseWasReleased, UICanvas.Canvas,
                false, true, false, GetActionStateType()))
            {
                oldSurface = GameFinder.GetDrawableSurface(selectedObj);
                oldValueHolder = DrawableType.Get(selectedObj);
                if (selectedObj.CompareTag(Tags.MindMapNode))
                {
                    oldNodesBranchLineHolder = GameMindMap.SummarizeSelectedNodeIncChildren(selectedObj);
                    newNodesBranchLineHolder = GameMindMap.SummarizeSelectedNodeIncChildren(selectedObj);
                }
                cutCopyPasteMenu = PrefabInstantiator.InstantiatePrefab(cutCopyPasteMenuPrefab,
                                                                        UICanvas.Canvas.transform, false);
                SetupButtons(cutCopyPasteMenu);
            }

            if (Queries.MouseUp(MouseButton.Left) && selectedObj == null)
            {
                mouseWasReleased = true;
            }

            if (Queries.MouseUp(MouseButton.Left) && selectedObj != null)
            {
                progressState = ProgressState.CutCopyPaste;
            }
        }

        /// <summary>
        /// Provides the interaction for cut/copy/paste.
        /// It closes the CutCopyPasteMenu, deactivates the BlinkEffect,
        /// and restores the drawable-type object to the new position.
        /// If Cut was chosen, the original object is destroyed.
        /// If no action (Cut/Copy) was chosen, and a left-click occurs, then the action is reset.
        /// </summary>
        private void CutCopyPaste()
        {
            if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit)
                && selectedObj != null
                && selectedObj.GetComponent<BlinkEffect>() != null
                && state != CutCopy.None)
            {
                Destroyer.Destroy(cutCopyPasteMenu);
                selectedObj.GetComponent<BlinkEffect>().Deactivate();
                Vector3 newPosition = raycastHit.point;
                newSurface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                switch (DrawableType.Get(selectedObj))
                {
                    case LineConf:
                    case TextConf:
                    case ImageConf:
                        ProcessPrimitiveType(newPosition);
                        break;
                    case MindMapNodeConf:
                        ProcessMindMapNode(newPosition);
                        break;
                }
                newValueHolder = DrawableType.Get(newObject);
                Cut();
                if (newObject.CompareTag(Tags.MindMapNode) &&
                    newObject.GetComponent<MMNodeValueHolder>().NodeKind != GameMindMap.NodeKind.Theme)
                {
                    progressState = ProgressState.OpenSelectParentMenu;
                }
            }

            if (Queries.MouseUp(MouseButton.Left) && state != CutCopy.None && newObject != null
                && progressState == ProgressState.CutCopyPaste)
            {
                progressState = ProgressState.Finish;
            }

            if (Queries.LeftMouseInteraction() && state == CutCopy.None)
            {
                SetToInitialState();
            }
        }

        /// <summary>
        /// Resets the action to the initial state.
        /// </summary>
        private void SetToInitialState()
        {
            Destroyer.Destroy(cutCopyPasteMenu);
            BlinkEffect.Deactivate(selectedObj);
            selectedObj = null;
            mouseWasReleased = false;
            editToOldBranchLine = false;
            state = CutCopy.None;
            progressState = ProgressState.SelectObject;
        }

        /// <summary>
        /// Deletes the original drawable type objects that were cut after pasting.
        /// </summary>
        private void Cut()
        {
            if (state == CutCopy.Cut)
            {
                if (selectedObj.CompareTag(Tags.MindMapNode))
                {
                    MMNodeValueHolder valueHolder = selectedObj.GetComponent<MMNodeValueHolder>();
                    if (valueHolder.GetParent() != null)
                    {
                        valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(selectedObj);
                        new MindMapRemoveChildNetAction(oldSurface.name,
                            GameFinder.GetDrawableSurfaceParentName(oldSurface),
                            MindMapNodeConf.GetNodeConf(selectedObj)).Execute();
                    }

                    if (valueHolder.GetParentBranchLine() != null)
                    {
                        new EraseNetAction(oldSurface.name, GameFinder.GetDrawableSurfaceParentName(oldSurface),
                            valueHolder.GetParentBranchLine().name).Execute();
                        Destroyer.Destroy(valueHolder.GetParentBranchLine());
                    }
                    foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                    {
                        new EraseNetAction(oldSurface.name, GameFinder.GetDrawableSurfaceParentName(oldSurface),
                            pair.Value.name).Execute();
                        Destroyer.Destroy(pair.Value);

                        new EraseNetAction(oldSurface.name, GameFinder.GetDrawableSurfaceParentName(oldSurface),
                            pair.Key.name).Execute();
                        Destroyer.Destroy(pair.Key);
                    }
                }
                new EraseNetAction(oldSurface.name, GameFinder.GetDrawableSurfaceParentName(oldSurface),
                    selectedObj.name).Execute();
                Destroyer.Destroy(selectedObj);
            }
        }

        /// <summary>
        /// Draws a clone of the chosen line to the chosen position.
        /// </summary>
        /// <param name="newPosition">The new position for the clone.</param>
        private void ProcessPrimitiveType(Vector3 newPosition)
        {
            DrawableType conf = DrawableType.Get(selectedObj);
            conf.Id = "";
            conf.AssociatedPage = newSurface.GetComponent<DrawableHolder>().CurrentPage;
            newObject = DrawableType.Restore(conf, newSurface);
            MoveWithWorldPosition(newPosition);
        }

        /// <summary>
        ///  Moves the clone of the selected node to the destination (new position).
        /// </summary>
        /// <param name="newPosition">destination position</param>
        private void MoveWithWorldPosition(Vector3 newPosition)
        {
            /// Moves the clone of the selected node to the destination (new position).
            Vector3 newLocalPosition = GameFinder.GetHighestParent(newSurface).transform.
                InverseTransformPoint(newPosition);
            newLocalPosition = new Vector3(newLocalPosition.x, newLocalPosition.y,
                selectedObj.transform.localPosition.z);
            GameMoveRotator.SetPosition(newObject, newLocalPosition, true);
            new MoveNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                newObject.name, newLocalPosition, true).Execute();
        }

        /// <summary>
        /// Adds a clone of the chosen node and its children to the chosen position.
        /// </summary>
        /// <param name="newPosition">The new position for the node.</param>
        private void ProcessMindMapNode(Vector3 newPosition)
        {
            if (selectedObj.GetComponent<MMNodeValueHolder>().GetParentBranchLine() != null)
            {
                oldBranchLineConf = LineConf.GetLine(selectedObj.GetComponent<MMNodeValueHolder>()
                    .GetParentBranchLine());
            }
            newNodesBranchLineHolder.MindMapNodeConfigs[0].BranchLineToParent = "";
            newNodesBranchLineHolder.MindMapNodeConfigs[0].ParentNode = "";
            GameMindMap.RenameMindMap(newNodesBranchLineHolder,
                GameFinder.GetAttachedObjectsObject(newSurface));

            foreach (DrawableType type in newNodesBranchLineHolder.GetAllDrawableTypes())
            {
                type.AssociatedPage = newSurface.GetComponent<DrawableHolder>().CurrentPage;
                DrawableType.Restore(type, newSurface);
            }

            newObject = GameFinder.FindChild(newSurface, newNodesBranchLineHolder.MindMapNodeConfigs[0].Id);
            MoveWithWorldPosition(newPosition);
            /// Updating positions.
            newNodesBranchLineHolder = GameMindMap.SummarizeSelectedNodeIncChildren(newObject);
        }

        /// <summary>
        /// Opens the selected parent menu and switches the progress state to select the parent.
        /// Is executed only if a subtheme or leaf node has been chosen for Cut/Copy.
        /// </summary>
        private void OpenSelectParent()
        {
            GameObject newAttachedObjects = GameFinder.GetAttachedObjectsObject(newSurface);
            if (newAttachedObjects != null)
            {
                MindMapParentSelectionMenu.EnableForEditing(newAttachedObjects, newObject,
                    MindMapNodeConf.GetNodeConf(newObject), null, true);
                progressState = ProgressState.SelectParent;
            }
        }

        /// <summary>
        /// Waits for user selection.
        /// A node can only be added if it is a theme node
        /// or if the drawable already has a theme node that qualifies as a parent node.
        /// If the node cannot be added, the action is canceled and reset.
        /// </summary>
        private void SelectParent()
        {
            if (MindMapParentSelectionMenu.TryGetParent(out GameObject parent))
            {
                MindMapParentSelectionMenu.Instance.Destroy();
                /// Block for the case when the node can be added.
                if (newValueHolder is MindMapNodeConf conf)
                {
                    conf.ParentNode = parent.name;
                    GameObject branchLineToParent = parent.GetComponent<MMNodeValueHolder>().GetChildren()[newObject];
                    conf.BranchLineToParent = branchLineToParent.name;
                    if (oldBranchLineConf != null)
                    {
                        GameEdit.ChangeLine(branchLineToParent, oldBranchLineConf);
                        new EditLineNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                            LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
                    }
                    newNodesBranchLineHolder = GameMindMap.SummarizeSelectedNodeIncChildren(newObject);
                }
                progressState = ProgressState.Finish;
            }
            else if (!MindMapParentSelectionMenu.Instance.IsOpen())
            { /// Block for the case when the node cannot be added.
              /// The previous changes are reverted.
              /// This means the clone nodes are deleted, and the original nodes are restored if they were deleted (cut).
                foreach (DrawableType type in newNodesBranchLineHolder.GetAllDrawableTypes())
                {
                    GameObject typeObject = GameFinder.FindChild(newSurface, type.Id);
                    new EraseNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                        typeObject.name).Execute();
                    Destroyer.Destroy(typeObject);
                }

                if (state == CutCopy.Cut)
                {
                    foreach (DrawableType type in oldNodesBranchLineHolder.GetAllDrawableTypes())
                    {
                        DrawableType.Restore(type, oldSurface);
                    }
                    if (oldBranchLineConf != null)
                    {
                        GameObject branchLineToParent = GameFinder.FindChild(oldSurface, oldValueHolder.Id).
                            GetComponent<MMNodeValueHolder>().GetParentBranchLine();
                        GameEdit.ChangeLine(branchLineToParent, oldBranchLineConf);
                        new EditLineNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                            LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
                    }
                }
                SetToInitialState();
            }
            else
            {
                /// This block is needed to restore the appearance of the parent branch line.
                if (oldBranchLineConf != null && !editToOldBranchLine)
                {
                    GameObject branchLineToParent = newObject.GetComponent<MMNodeValueHolder>().GetParentBranchLine();
                    GameEdit.ChangeLine(branchLineToParent, oldBranchLineConf);
                    new EditLineNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                        LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
                    editToOldBranchLine = true;
                }
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old object and destroys the new one.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            /// Block to restore the original cut object.
            if (memento.State == CutCopy.Cut)
            {
                GameObject oldSurface = memento.OldSurface.GetDrawableSurface();

                if (memento.OldValueHolder is MindMapNodeConf)
                {
                    foreach (DrawableType type in memento.OldNodesHolder.GetAllDrawableTypes())
                    {
                        DrawableType.Restore(type, oldSurface);
                    }
                    if (memento.OldBranchLineConfig != null)
                    {
                        GameObject oldObject = GameFinder.FindChild(oldSurface, memento.OldValueHolder.Id);
                        if (oldObject.GetComponent<MMNodeValueHolder>().GetParentBranchLine() != null)
                        {
                            GameObject branchLineToParent = oldObject.GetComponent<MMNodeValueHolder>().
                                GetParentBranchLine();
                            GameEdit.ChangeLine(branchLineToParent, memento.OldBranchLineConfig);
                            new EditLineNetAction(oldSurface.name, GameFinder.GetDrawableSurfaceParentName(oldSurface),
                                LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
                        }
                    }
                }
                else
                {
                    DrawableType.Restore(memento.OldValueHolder, oldSurface);
                }
            }

            /// Block to destroy the clone object.
            GameObject newObject = GameFinder.FindChild(memento.NewSurface.GetDrawableSurface(),
                memento.NewValueHolder.Id);
            if (newObject.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = newObject.GetComponent<MMNodeValueHolder>();
                if (valueHolder.GetParentBranchLine() != null)
                {
                    new EraseNetAction(memento.NewSurface.ID, memento.NewSurface.ParentID,
                        valueHolder.GetParentBranchLine().name).Execute();
                    Destroyer.Destroy(valueHolder.GetParentBranchLine());

                    valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(newObject);
                    new MindMapRemoveChildNetAction(memento.NewSurface.ID, memento.NewSurface.ParentID,
                        MindMapNodeConf.GetNodeConf(newObject)).Execute();
                }
                foreach (DrawableType type in memento.NewNodesHolder.GetAllDrawableTypes())
                {
                    new EraseNetAction(memento.NewSurface.ID, memento.NewSurface.ParentID, type.Id).Execute();
                    Destroyer.Destroy(GameFinder.FindChild(memento.NewSurface.GetDrawableSurface(), type.Id));
                }
            }
            new EraseNetAction(memento.NewSurface.ID, memento.NewSurface.ParentID, newObject.name).Execute();
            Destroyer.Destroy(newObject);
        }

        /// <summary>
        /// Repeats this action, i.e., creates again the new object and deletes the old one if it was cut.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            /// Block to restore the clone object.
            GameObject newSurface = memento.NewSurface.GetDrawableSurface();

            if (memento.NewValueHolder is MindMapNodeConf)
            {
                GameObject obj = DrawableType.Restore(memento.NewValueHolder, newSurface);
                foreach (DrawableType type in memento.NewNodesHolder.GetAllDrawableTypes())
                {
                    DrawableType.Restore(type, newSurface);
                }
                if (memento.OldBranchLineConfig != null)
                {
                    GameObject newObject = GameFinder.FindChild(newSurface, memento.NewValueHolder.Id);
                    if (newObject.GetComponent<MMNodeValueHolder>().GetParentBranchLine() != null)
                    {
                        GameObject branchLineToParent = newObject.GetComponent<MMNodeValueHolder>().
                            GetParentBranchLine();
                        GameEdit.ChangeLine(branchLineToParent, memento.OldBranchLineConfig);
                        new EditLineNetAction(newSurface.name, GameFinder.GetDrawableSurfaceParentName(newSurface),
                            LineConf.GetLineWithoutRenderPos(branchLineToParent)).Execute();
                    }
                }
            }
            else
            {
                DrawableType.Restore(memento.NewValueHolder, newSurface);
            }

            /// Block to destroy the original object, if cut was selected.
            if (memento.State == CutCopy.Cut)
            {
                GameObject oldObject = GameFinder.FindChild(memento.OldSurface.GetDrawableSurface(),
                    memento.OldValueHolder.Id);
                if (oldObject.CompareTag(Tags.MindMapNode))
                {
                    MMNodeValueHolder valueHolder = oldObject.GetComponent<MMNodeValueHolder>();
                    if (valueHolder.GetParentBranchLine() != null)
                    {
                        new EraseNetAction(memento.OldSurface.ID, memento.OldSurface.ParentID,
                            valueHolder.GetParentBranchLine().name).Execute();
                        Destroyer.Destroy(valueHolder.GetParentBranchLine());

                        valueHolder.GetParent().GetComponent<MMNodeValueHolder>().RemoveChild(oldObject);
                        new MindMapRemoveChildNetAction(memento.OldSurface.ID, memento.OldSurface.ParentID,
                            MindMapNodeConf.GetNodeConf(oldObject)).Execute();
                    }
                    foreach (DrawableType type in memento.OldNodesHolder.GetAllDrawableTypes())
                    {
                        new EraseNetAction(memento.OldSurface.ID, memento.OldSurface.ParentID, type.Id).Execute();
                        Destroyer.Destroy(GameFinder.FindChild(memento.OldSurface.GetDrawableSurface(), type.Id));
                    }
                }
                new EraseNetAction(memento.OldSurface.ID, memento.OldSurface.ParentID, oldObject.name).Execute();
                Destroyer.Destroy(oldObject);
            }
        }

        /// <summary>
        /// A new instance of <see cref="CutCopyPasteActyion"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CutCopyPasteAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new CutCopyPasteAction();
        }

        /// <summary>
        /// A new instance of <see cref="CutCopyPasteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CutCopyPasteAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.CutCopyPaste"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.CutCopyPaste;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object,
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
                {
                    memento.OldValueHolder.Id,
                    memento.NewValueHolder.Id
                };
        }
    }
}
