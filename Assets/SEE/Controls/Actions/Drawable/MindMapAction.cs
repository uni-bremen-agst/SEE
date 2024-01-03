using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Game.UI.Menu.Drawable;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.UI.Menu.Drawable.MindMapMenu;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the operations for a mind map.
    /// </summary>
    public class MindMapAction : AbstractPlayerAction
    {
        /// <summary>
        /// The selected operation for the mind map
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
        /// The chosen operation from mind map menu.
        /// </summary>
        private Operation chosenOperation = Operation.None;

        /// <summary>
        /// The drawable of the chosen position.
        /// </summary>
        private GameObject drawable;

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
        /// The branch line to parent.
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
        private struct Memento
        {
            /// <summary>
            /// The executed operation.
            /// </summary>
            public readonly Operation operation;
            /// <summary>
            /// The drawable on that the node should be displayed.
            /// </summary>
            public readonly DrawableConfig drawable;

            /// <summary>
            /// The node configuration.
            /// </summary>
            public readonly MindMapNodeConf conf;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the node should be displayed.</param>
            /// <param name="conf">The node configuration</param>
            /// <param name="operation">The executed operation.</param>
            public Memento(GameObject drawable, MindMapNodeConf conf, Operation operation)
            {
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.conf = conf;
                this.operation = operation;
            }
        }

        /// <summary>
        /// Enables the mind map menu.
        /// </summary>
        public override void Awake()
        {
            MindMapMenu.Enable();
        }

        /// <summary>
        /// Disables the mind map menu.
        /// </summary>
        public override void Stop()
        {
            MindMapMenu.Disable();
            MindMapParentSelectionMenu.Disable();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MindMap"/>.
        /// It allows the user to set a mind map node.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
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
                        if (Input.GetMouseButtonDown(0))
                        {
                            ShowNotification.Info("Select an operation", 
                                "First you need to select an operation from the menu.");
                        }
                        break;
                    case Operation.Theme:
                    case Operation.Subtheme:
                    case Operation.Leaf:
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
                    positions[0] = GameFinder.GetHighestParent(node).transform.
                        InverseTransformPoint(NearestPoints.GetNearestPoint(node,
                            MindMapParentSelectionMenu.GetChosenParent().transform.position));
                    positions[1] = GameFinder.GetHighestParent(node).transform.
                        InverseTransformPoint(NearestPoints.GetNearestPoint(
                            MindMapParentSelectionMenu.GetChosenParent(),
                            node.transform.position));
                    branchLineRenderer.positionCount = 2;
                    branchLineRenderer.SetPositions(positions);
                }

                if (MindMapParentSelectionMenu.TryGetParent(out GameObject parent))
                {
                    Destroyer.Destroy(branchLine);
                    branchLine = GameMindMap.CreateBranchLine(node, parent);
                    progress = ProgressState.Finish;
                }
            }
        }

        /// <summary>
        /// The process of addition of a mind map node.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
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
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) 
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) 
                && (GameFinder.hasDrawable(raycastHit.collider.gameObject)
                    || raycastHit.collider.gameObject.CompareTag(Tags.Drawable)))
            {
                MindMapMenu.Disable();
                drawable = GameFinder.GetDrawable(raycastHit.collider.gameObject);
                bool validState = CheckValid(GameFinder.GetAttachedObjectsObject(drawable));
                if (validState)
                {
                    position = raycastHit.point;
                    progress = ProgressState.WaitForText;
                    writeTextDialog = new WriteEditTextDialog();
                    writeTextDialog.Open();
                    return true;
                }
                else
                {
                    drawable = null;
                    chosenOperation = Operation.None;
                    MindMapMenu.Enable();
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
            if (writeTextDialog.GetUserInput(out string textOut))
            {
                writtenText = textOut;
                progress = ProgressState.Add;
            }
            if (writeTextDialog.WasCanceled())
            {
                chosenOperation = Operation.None;
                MindMapMenu.Enable();
                progress = ProgressState.SelectPosition;
            }
        }

        /// <summary>
        /// This method handles the creation of the mind map node.
        /// It creates the node, and if it is not a theme, the SelectParent section is initiated.
        /// </summary>
        private void AddNode()
        {
            string prefix = GetPrefix();
            node = GameMindMap.Create(drawable, prefix, writtenText, position);
            if (chosenOperation == Operation.Theme)
            {
                progress = ProgressState.Finish;
            }
            else
            {
                progress = ProgressState.SelectParent;
                /// The following block is for a branch line preview.
                branchLine = GameDrawer.StartDrawing(drawable, new Vector3[] { position },
                    GameDrawer.ColorKind.Monochrome, Color.black, Color.clear,
                    ValueHolder.standardLineThickness, GameDrawer.LineKind.Solid,
                    ValueHolder.standardLineTiling);
                branchLine.transform.SetParent(node.transform);
                branchLineRenderer = branchLine.GetComponent<LineRenderer>();
                MindMapParentSelectionMenu.Enable(GameFinder.GetAttachedObjectsObject(drawable), node);
            }
        }

        /// <summary>
        /// This Method handles the selection of a parent.
        /// </summary>
        private void SelectParent()
        {
            /// This block is for the branch line preview.
            /// It draws the line from the origin of the node to the position of the mouse cursor.
            if (!Input.GetMouseButton(0) && Raycasting.RaycastAnything(out RaycastHit raycast) &&
               (raycast.collider.gameObject.CompareTag(Tags.Drawable)
                  || GameFinder.hasDrawable(raycast.collider.gameObject)))
            {
                Vector3[] positions = new Vector3[2];
                positions[0] = GameFinder.GetHighestParent(node).transform
                    .InverseTransformPoint(NearestPoints.GetNearestPoint(node, raycast.point));
                positions[1] = GameFinder.GetHighestParent(node).transform
                    .InverseTransformPoint(raycast.point);
                branchLineRenderer.positionCount = 2;
                branchLineRenderer.SetPositions(positions);
            }

            /// This block is for the selection of a parent node.
            /// It will executed if the left mouse button will be clicked.
            if (Input.GetMouseButtonDown(0))
            {
                /// A node can only be chosen as a parent node if it is a Theme or Subtheme Node. 
                /// Additionally, the node must not choose itself.
                if (Raycasting.RaycastAnything(out RaycastHit hit) &&
                   hit.collider.gameObject.CompareTag(Tags.MindMapNode) &&
                    (hit.collider.gameObject.name.StartsWith(ValueHolder.MindMapThemePrefix) ||
                    hit.collider.gameObject.name.StartsWith(ValueHolder.MindMapSubthemePrefix)) &&
                    hit.collider.gameObject != node)
                {
                    Destroyer.Destroy(branchLine);
                    branchLine = GameMindMap.CreateBranchLine(node, hit.collider.gameObject);
                    progress = ProgressState.Finish;
                }
                else
                {
                    ShowNotification.Warn("Wrong selection.", "You need to select a theme or a subtheme node.");
                }
            }
        }

        /// <summary>
        /// Finish the action.
        /// </summary>
        /// <returns>true</returns>
        private bool FinishAdd()
        {
            memento = new(drawable, MindMapNodeConf.GetNodeConf(node), chosenOperation);
            new MindMapCreateNodeNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.conf).Execute();
            currentState = ReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// This method checks for validity by verifying 
        /// if at least one theme exists on the drawable. 
        /// If not, it provides an appropriate prompt 
        /// when attempting to add a node that is not a theme.
        /// </summary>
        /// <param name="attachedObjects">The attached objects object of the drawable</param>
        /// <returns>the result of the validation.</returns>
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
                ShowNotification.Warn("Can't add", "First you need to add a theme.");
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
            GameObject attached = GameFinder.GetAttachedObjectsObject(memento.drawable.GetDrawable());
            GameObject node = GameFinder.FindChild(attached, memento.conf.id);
            if (memento.operation != Operation.Theme)
            {
                GameObject parent = GameFinder.FindChild(attached, memento.conf.parentNode);
                if (parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().RemoveChild(node);
                }
                new MindMapRemoveChildNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    memento.conf).Execute();
                GameObject branchToParent = GameFinder.FindChild(attached, memento.conf.branchLineToParent);
                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                    branchToParent.name).Execute();
                Destroyer.Destroy(branchToParent);
            }
            new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                memento.conf.id).Execute();
            Destroyer.Destroy(node);
        }

        /// <summary>
        /// Repeats this action, i.e., it re-adds the mind map node.
        /// </summary>
        public override void Redo()
        {
            GameMindMap.ReCreate(memento.drawable.GetDrawable(), memento.conf);
            new MindMapCreateNodeNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.conf).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="MindMapAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MindMapAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MindMapAction();
        }

        /// <summary>
        /// A new instance of <see cref="MindMapAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MindMapAction"/></returns>
        public override ReversibleAction NewInstance()
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
            return new HashSet<string>();
        }
    }
}