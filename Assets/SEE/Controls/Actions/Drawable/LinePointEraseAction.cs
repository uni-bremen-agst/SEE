using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to erase only some points of a <see cref="LineConf"/>.
    /// </summary>
    class LinePointEraseAction : LineAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private readonly List<Memento> mementoList = new();

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LinePointErase"/>.
        /// Specifically: Allows the user to remove one or more points of a line.
        ///               In one action run it can remove multiple points of different lines.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block is responsible for remove a point of the line/lines.
                /// It searches for the nearest point on the line from the mouse position.
                /// Multiple line points may overlap, so it works with a list of nearest points.
                /// The line is split at the found points and the points will be removed. Sublines will be created,
                /// with their starting and ending points corresponding to the nearest point of the removed point.
                if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (hitObject.CompareTag(Tags.Line) || hitObject.CompareTag(Tags.LineCap))
                    {
                        isActive = true;

                        GameObject selectedLine = hitObject.CompareTag(Tags.LineCap)
                            ? hitObject.transform.parent.gameObject
                            : hitObject;

                        LineConf originLine = LineConf.GetLine(selectedLine);
                        List<LineConf> lines = new();

                        List<int> matchedIndices;
                        if (hitObject.CompareTag(Tags.LineCap))
                        {
                            bool startCapSelected = hitObject.name.StartsWith(ValueHolder.LineStartCapPrefix);
                            int lastIndex = selectedLine.GetComponent<LineRenderer>().positionCount - 1;

                            matchedIndices = new List<int>
                            {
                                startCapSelected ? 0 : lastIndex
                            };
                        }
                        else
                        {
                            NearestPoints.GetNearestPoints(selectedLine, raycastHit.point,
                                out List<Vector3> _, out matchedIndices);
                        }

                        GameLineSplit.Split(GameFinder.GetDrawableSurface(selectedLine), originLine,
                            matchedIndices, GameDrawer.GetOriginalLinePositions(selectedLine).ToList(), lines, true);

                        memento = new Memento(selectedLine, GameFinder.GetDrawableSurface(selectedLine), lines);
                        mementoList.Add(memento);
                        new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID,
                            memento.OriginalLine.ID).Execute();
                        Destroyer.Destroy(selectedLine);
                    }
                }
                /// This block completes the action.
                if (SEEInput.MouseUp(MouseButton.Left) && isActive)
                {
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., it deletes the sublines and restores the original line.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            List<Memento> reverseList = new(mementoList);
            reverseList.Reverse();
            foreach (Memento mem in reverseList)
            {
                GameObject surface = mem.Surface.GetDrawableSurface();
                GameDrawer.ReDrawLine(surface, mem.OriginalLine);
                new DrawNetAction(mem.Surface.ID, mem.Surface.ParentID, mem.OriginalLine).Execute();

                foreach (LineConf line in mem.Lines)
                {
                    GameObject lineObj = GameFinder.FindAttachedOrLocalDescendant(surface, line.ID);
                    new EraseNetAction(mem.Surface.ID, mem.Surface.ParentID, line.ID).Execute();
                    Destroyer.Destroy(lineObj);
                }
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes the original line and restores the sublines.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (Memento mem in mementoList)
            {
                GameObject surface = mem.Surface.GetDrawableSurface();
                GameObject originObj = GameFinder.FindAttachedOrLocalDescendant(surface, mem.OriginalLine.ID);
                new EraseNetAction(mem.Surface.ID, mem.Surface.ParentID, mem.OriginalLine.ID).Execute();
                Destroyer.Destroy(originObj);

                foreach (LineConf line in mem.Lines)
                {
                    GameDrawer.ReDrawLine(surface, line);
                    new DrawNetAction(mem.Surface.ID, mem.Surface.ParentID, line).Execute();
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="LinePointEraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LinePointEraseAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LinePointEraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="LinePointEraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LinePointEraseAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LinePointErase"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LinePointErase;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>A set of line IDs.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento == null || memento.Surface == null)
            {
                return new();
            }
            else
            {
                HashSet<string> changedObjects = new();
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.OriginalLine.ID);
                }
                return changedObjects;
            }
        }
    }
}
