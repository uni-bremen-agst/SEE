using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.Drawable;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;
using static SEE.UI.Menu.Drawable.MindMapMenu;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the operations for a mind map.
    /// </summary>
    public class MindMapAction : DrawableAction
    {
        /// <summary>
        /// The selected operation for the mind map.
        /// </summary>
        private ProgressState progress = ProgressState.SelectPosition;

        /// <summary>
        /// The different progress states for the mind map.
        /// </summary>
        private enum ProgressState
        {
            SelectPosition,
            WaitForText,
            Add,
            SelectParent,
            Finish
        }

        /// <summary>
        /// The chosen operation from the mind map menu.
        /// </summary>
        private Operation chosenOperation = Operation.None;

        /// <summary>
        /// The chosen position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The description for the mind map node.
        /// </summary>
        private string writtenText;

        /// <summary>
        /// The created mind map node;
        /// </summary>
        private GameObject node;

        /// <summary>
        /// The branch line to the parent.
        /// </summary>
        private GameObject branchLine;

        /// <summary>
        /// Line renderer of the branch line.
        /// </summary>
        private LineRenderer branchLineRenderer;

        /// <summary>
        /// The write text dialog for getting the node description.
        /// </summary>
        private WriteEditTextDialog writeTextDialog;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MindMapAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// The executed operation.
            /// </summary>
            public readonly Operation Operation;
            /// <summary>
            /// The drawable surface on which the node should be displayed.
            /// </summary>
            public readonly DrawableConfig Surface;

            /// <summary>
            /// The node configuration.
            /// </summary>
            public readonly MindMapNodeConf Conf;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="surface">The drawable surface on which the node should be displayed.</param>
            /// <param name="conf">The node configuration</param>
            /// <param name="operation">The executed operation.</param>
            public Memento(GameObject surface, MindMapNodeConf conf, Operation operation)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Conf = conf;
                Operation = operation;
            }
        }

        /// <summary>
        /// Enables the mind map menu.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            MindMapMenu.Instance.Enable();
        }

        /// <summary>
        /// Disables the mind map menu.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            MindMapMenu.Instance.Destroy();
            MindMapParentSelectionMenu.Instance.Destroy();

            if (progress != ProgressState.Finish && node != null)
            {
                Destroyer.Destroy(node);
                if (branchLine != null)
                {
                    Destroyer.Destroy(branchLine);
                }
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MindMap"/>.
        /// It allows the user to set a mind map node.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if (MindMapMenu.TryGetOperation(out Operation operation))
                {
                    chosenOperation = operation;
                }

                switch (chosenOperation)
                {
                    case Operation.None:
                        if (SEEInput.LeftMouseDown())
                        {
                            ShowNotification.Info("Select an operation",
                                "First you need to select an operation from the menu.");
                        }
                        break;
                    case Operation.Theme or Operation.Subtheme or Operation.Leaf:
                        return AdditionProcess();
                }
            }
            else
            {
                ShowChangesFromMenu();
            }
            return false;
        }

        /// <summary>
        /// This method redraws the branch line when a change is made in the select parent menu.
        /// </summary>
        private void ShowChangesFromMenu()
        {
            if ((chosenOperation == Operation.Subtheme || chosenOperation == Operation.Leaf)
                && progress == ProgressState.SelectParent)
            {
                if (MindMapParentSelectionMenu.GetChosenParent() != null)
                {
                    Vector3[] positions = new Vector3[2];
                    positions[0] = node.GetRootParent().transform.
                        InverseTransformPoint(NearestPoints.GetNearestPoint(node,
                            MindMapParentSelectionMenu.GetChosenParent().transform.position));
                    positions[1] = node.GetRootParent().transform.
                        InverseTransformPoint(NearestPoints.GetNearestPoint(
                            MindMapParentSelectionMenu.GetChosenParent(),
                            node.transform.position));
                    branchLineRenderer.positionCount = 2;
                    branchLineRenderer.SetPositions(positions);
                }

                if (MindMapParentSelectionMenu.TryGetParent(out GameObject parent))
                {
                    MindMapParentSelectionMenu.Instance.Destroy();
                    Destroyer.Destroy(branchLine);
                    branchLine = GameMindMap.CreateBranchLine(node, parent);
                    progress = ProgressState.Finish;
                }
            }
        }

        /// <summary>
        /// The process of adding a mind map node.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        private bool AdditionProcess()
        {
            switch (progress)
            {
                case ProgressState.SelectPosition:
                    if (!SelectPosition())
                    {
                        return false;
                    }
                    break;
                case ProgressState.WaitForText:
                    WaitForText();
                    break;
                case ProgressState.Add:
                    AddNode();
                    break;
                case ProgressState.SelectParent:
                    SelectParent();
                    break;
                case ProgressState.Finish:
                    return FinishAdd();
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Handles the position selection.
        /// Checks using <see cref="CheckValid"/> whether it is a valid state.
        /// If so, the position is adopted, and it waits for the description for the node.
        /// Otherwise, the action is aborted and reset.
        /// </summary>
        /// <returns>the success of the selection.</returns>
        private bool SelectPosition()
        {
            if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit))
            {
                MindMapMenu.Instance.Destroy();
                Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                if (CheckValid(GameFinder.GetAttachedObjectsObject(Surface)))
                {
                    position = raycastHit.point;
                    progress = ProgressState.WaitForText;
                    writeTextDialog = new WriteEditTextDialog();
                    writeTextDialog.Open();
                    return true;
                }
                else
                {
                    Surface = null;
                    chosenOperation = Operation.None;
                    MindMapMenu.Instance.Enable();
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles the node description input of the user.
        /// </summary>
        private void WaitForText()
        {
            if (writeTextDialog.TryGetUserInput(out string textOut))
            {
                if (string.IsNullOrEmpty(textOut))
                {
                    ShowNotification.Error("No text entered", "You need to enter a description for the node.");
                    return;
                }
                writtenText = textOut;
                progress = ProgressState.Add;
            }
            if (writeTextDialog.WasCanceled())
            {
                chosenOperation = Operation.None;
                MindMapMenu.Instance.Enable();
                progress = ProgressState.SelectPosition;
            }
        }

        /// <summary>
        /// This method handles the creation of the mind map node.
        /// It creates the node, and if it is not a theme, the <see cref="SelectParent"/>
        /// section is initiated.
        /// </summary>
        private void AddNode()
        {
            node = GameMindMap.Create(Surface, GetPrefix(), writtenText, position);
            if (chosenOperation == Operation.Theme)
            {
                progress = ProgressState.Finish;
            }
            else
            {
                progress = ProgressState.SelectParent;
                ShowNotification.Info("Select a Parent Node", "Now select a parent node.\n" +
                    "To select, click on the specific parent node or choose it from the menu.", 3);
                /// The following block is for a branch line preview.
                branchLine = GameDrawer.StartDrawing(Surface, new Vector3[] { position },
                    GameDrawer.ColorKind.Monochrome, Color.black, Color.clear,
                    ValueHolder.StandardLineThickness, GameDrawer.LineKind.Solid,
                    ValueHolder.StandardLineTiling);
                branchLine.transform.SetParent(node.transform);
                branchLineRenderer = branchLine.GetComponent<LineRenderer>();
                MindMapParentSelectionMenu.Enable(GameFinder.GetAttachedObjectsObject(Surface), node);
            }
        }

        /// <summary>
        /// This method handles the selection of a parent.
        /// </summary>
        private void SelectParent()
        {
            /// This block enables the canceling of the action.
            Cancel();

            /// This block is for the branch line preview.
            /// It draws the line from the origin of the node to the position of the mouse cursor.
            if (!SEEInput.MouseHold(MouseButton.Left)
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && node != null)
            {
                Vector3[] positions = new Vector3[2];
                positions[0] = node.GetRootParent().transform
                    .InverseTransformPoint(NearestPoints.GetNearestPoint(node, raycastHit.point));
                positions[1] = node.GetRootParent().transform
                    .InverseTransformPoint(raycastHit.point);
                branchLineRenderer.positionCount = 2;
                branchLineRenderer.SetPositions(positions);
            }

            /// This block is for the selection of a parent node.
            /// It will executed if the left mouse button will be clicked.
            if (SEEInput.LeftMouseDown() && node != null)
            {
                /// A node can only be chosen as a parent node if it is a Theme or Subtheme Node.
                /// Additionally, the node must not choose itself.
                if (Raycasting.RaycastAnything(out RaycastHit hit)
                    && hit.collider.gameObject.CompareTag(Tags.MindMapNode)
                    && (hit.collider.gameObject.name.StartsWith(ValueHolder.MindMapThemePrefix) ||
                        hit.collider.gameObject.name.StartsWith(ValueHolder.MindMapSubthemePrefix))
                    && hit.collider.gameObject != node
                    && GameFinder.GetDrawableSurface(hit.collider.gameObject).Equals(GameFinder.GetDrawableSurface(node)))
                {
                    Destroyer.Destroy(branchLine);
                    branchLine = GameMindMap.CreateBranchLine(node, hit.collider.gameObject);
                    progress = ProgressState.Finish;
                }
                else
                {
                    ShowNotification.Warn("Wrong selection.",
                                          "You need to select a theme or a subtheme node of the currently selected drawable.");
                }
            }
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if (SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                MindMapParentSelectionMenu.Instance.Destroy();

                if (progress != ProgressState.Finish && node != null)
                {
                    Destroyer.Destroy(node);
                    if (branchLine != null)
                    {
                        Destroyer.Destroy(branchLine);
                    }
                }

                progress = ProgressState.SelectPosition;
                chosenOperation = Operation.None;
                node = null;
                branchLine = null;
                MindMapMenu.Instance.Enable();
            }
        }

        /// <summary>
        /// Finishes the action.
        /// </summary>
        /// <returns>true</returns>
        private bool FinishAdd()
        {
            memento = new(Surface, MindMapNodeConf.GetNodeConf(node), chosenOperation);
            new MindMapCreateNodeNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Conf).Execute();
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// This method checks for validity by verifying
        /// if at least one theme exists on the drawable.
        /// If not, it provides an appropriate prompt
        /// when attempting to add a node that is not a theme.
        /// </summary>
        /// <param name="attachedObjects">The attached objects of the drawable</param>
        /// <returns>the result of the validation</returns>
        private bool CheckValid(GameObject attachedObjects)
        {
            if (chosenOperation == Operation.Theme)
            {
                return true;
            }

            if (attachedObjects != null && GameFinder.FindAllChildrenWithTag(attachedObjects,
                Tags.MindMapNode).Count > 0)
            {
                return true;
            }
            else
            {
                ShowNotification.Warn("Cannot add", "First you need to add a theme.");
            }

            return false;
        }

        /// <summary>
        /// Gets the prefix of the chosen <see cref="MindMapMenu.Operation"/>
        /// </summary>
        /// <returns>The prefix</returns>
        private string GetPrefix()
        {
            string prefix;
            switch (chosenOperation)
            {
                case Operation.Theme:
                    prefix = ValueHolder.MindMapThemePrefix;
                    break;
                case Operation.Subtheme:
                    prefix = ValueHolder.MindMapSubthemePrefix;
                    break;
                case Operation.Leaf:
                    prefix = ValueHolder.MindMapLeafPrefix;
                    break;
                default:
                    prefix = "";
                    break;
            }
            return prefix;
        }

        /// <summary>
        /// Reverts this action, i.e., it deletes the created mind map node
        /// and deletes it from the list of child nodes in the parent node,
        /// as well as the branch line to the parent node.
        /// </summary>
        public override void Undo()
        {
            GameObject attached = GameFinder.GetAttachedObjectsObject(memento.Surface.GetDrawableSurface());
            GameObject node = GameFinder.FindChild(attached, memento.Conf.Id);
            if (memento.Operation != Operation.Theme)
            {
                GameObject parent = GameFinder.FindChild(attached, memento.Conf.ParentNode);
                if (parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().RemoveChild(node);
                }
                new MindMapRemoveChildNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.Conf).Execute();
                GameObject branchToParent = GameFinder.FindChild(attached, memento.Conf.BranchLineToParent);
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    branchToParent.name).Execute();
                Destroyer.Destroy(branchToParent);
            }
            new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID,
                memento.Conf.Id).Execute();
            Destroyer.Destroy(node);
        }

        /// <summary>
        /// Repeats this action, i.e., it re-adds the mind map node.
        /// </summary>
        public override void Redo()
        {
            GameMindMap.ReCreate(memento.Surface.GetDrawableSurface(), memento.Conf);
            new MindMapCreateNodeNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Conf).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="MindMapAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MindMapAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MindMapAction();
        }

        /// <summary>
        /// A new instance of <see cref="MindMapAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MindMapAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MindMap"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MindMap;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object,
        /// an empty set is always returned.
        /// </summary>
        /// <returns>empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Surface != null)
            {
                HashSet<string> changedObjects = new()
                {
                    memento.Surface.ID,
                    memento.Conf.Id
                };
                if (memento.Operation.Equals(Operation.Subtheme)
                    || memento.Operation.Equals(Operation.Leaf))
                {
                    changedObjects.Add(memento.Conf.BranchLineToParent);
                }
                return changedObjects;
            }
            return new();
        }
    }
}
