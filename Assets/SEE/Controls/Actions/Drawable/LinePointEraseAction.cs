using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to erase only some points of a <see cref="LineConf"/>.
    /// </summary>
    class LinePointEraseAction : DrawableAction
    {
        /// <summary>
        /// True if the action is active.
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private readonly List<Memento> mementoList = new();

        /// <summary>
        /// Saves the information for one line point erase.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to
        /// revert or repeat a <see cref="LinePointEraseAction"/>.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// Is the configuration of line before a point was removed.
            /// </summary>
            public LineConf OriginalLine;
            /// <summary>
            /// Is the drawable on which the lines are displayed.
            /// </summary>
            public readonly DrawableConfig Drawable;
            /// <summary>
            /// The list of lines that resulted from point remove of the original line.
            /// </summary>
            public List<LineConf> Lines;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before a point was removed.</param>
            /// <param name="drawable">The drawable where the lines are displayed</param>
            /// <param name="lines">The list of lines that resulted from remove a point of the original line</param>
            public Memento(GameObject originalLine, GameObject drawable, List<LineConf> lines)
            {
                OriginalLine = LineConf.GetLine(originalLine);
                Drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                Lines = lines;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LinePointErase"/>.
        /// Specifically: Allows the user to remove one or more points of a line.
        ///               In one action run it can remove multiple points of different lines.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block is responsible for remove a point of the line/lines.
                /// It searches for the nearest point on the line from the mouse position.
                /// Multiple line points may overlap, so it works with a list of nearest points.
                /// The line is split at the found points and the points will be removed. Sublines will be created,
                /// with their starting and ending points corresponding to the nearest point of the removed point.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameFinder.HasDrawable(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    isActive = true;

                    if (hittedObject.CompareTag(Tags.Line))
                    {
                        LineConf originLine = LineConf.GetLine(hittedObject);
                        List<LineConf> lines = new();
                        NearestPoints.GetNearestPoints(hittedObject, raycastHit.point,
                            out List<Vector3> positionsList, out List<int> matchedIndices);

                        GameLineSplit.Split(GameFinder.GetDrawable(hittedObject), originLine,
                            matchedIndices, positionsList, lines, true);

                        memento = new Memento(hittedObject, GameFinder.GetDrawable(hittedObject), lines);
                        mementoList.Add(memento);
                        new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                            memento.OriginalLine.Id).Execute();
                        Destroyer.Destroy(hittedObject);
                    }
                }
                /// This block completes the action.
                if (Input.GetMouseButtonUp(0) && isActive)
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
                GameObject drawable = mem.Drawable.GetDrawable();
                GameDrawer.ReDrawLine(drawable, mem.OriginalLine);
                new DrawNetAction(mem.Drawable.ID, mem.Drawable.ParentID, mem.OriginalLine).Execute();

                foreach (LineConf line in mem.Lines)
                {
                    GameObject lineObj = GameFinder.FindChild(drawable, line.Id);
                    new EraseNetAction(mem.Drawable.ID, mem.Drawable.ParentID, line.Id).Execute();
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
                GameObject drawable = mem.Drawable.GetDrawable();
                GameObject originObj = GameFinder.FindChild(drawable, mem.OriginalLine.Id);
                new EraseNetAction(mem.Drawable.ID, mem.Drawable.ParentID, mem.OriginalLine.Id).Execute();
                Destroyer.Destroy(originObj);

                foreach (LineConf line in mem.Lines)
                {
                    GameDrawer.ReDrawLine(drawable, line);
                    new DrawNetAction(mem.Drawable.ID, mem.Drawable.ParentID, line).Execute();
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="LinePointEraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LinePointEraseAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LinePointEraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="LinePointEraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LinePointEraseAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LinePointErase"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LinePointErase;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>a set of line ids</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento == null || memento.Drawable == null)
            {
                return new();
            }
            else
            {
                HashSet<string> changedObjects = new();
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.OriginalLine.Id);
                }
                return changedObjects;
            }
        }
    }
}
