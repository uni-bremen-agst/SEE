using Assets.SEE.Game.Drawable.ActionHelpers;
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
            /// Is the drawable surface on which the lines are displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The list of lines that resulted from point remove of the original line.
            /// </summary>
            public List<LineConf> Lines;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before a point was removed.</param>
            /// <param name="surface">The drawable surface where the lines are displayed</param>
            /// <param name="lines">The list of lines that resulted from remove a point of the original line</param>
            public Memento(GameObject originalLine, GameObject surface, List<LineConf> lines)
            {
                OriginalLine = LineConf.GetLine(originalLine);
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
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
                if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;
                    isActive = true;

                    if (hitObject.CompareTag(Tags.Line))
                    {
                        LineConf originLine = LineConf.GetLine(hitObject);
                        List<LineConf> lines = new();
                        NearestPoints.GetNearestPoints(hitObject, raycastHit.point,
                            out List<Vector3> positionsList, out List<int> matchedIndices);

                        GameLineSplit.Split(GameFinder.GetDrawableSurface(hitObject), originLine,
                            matchedIndices, positionsList, lines, true);

                        memento = new Memento(hitObject, GameFinder.GetDrawableSurface(hitObject), lines);
                        mementoList.Add(memento);
                        new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID,
                            memento.OriginalLine.Id).Execute();
                        Destroyer.Destroy(hitObject);
                    }
                }
                /// This block completes the action.
                if (Queries.MouseUp(MouseButton.Left) && isActive)
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
                    GameObject lineObj = GameFinder.FindChild(surface, line.Id);
                    new EraseNetAction(mem.Surface.ID, mem.Surface.ParentID, line.Id).Execute();
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
                GameObject originObj = GameFinder.FindChild(surface, mem.OriginalLine.Id);
                new EraseNetAction(mem.Surface.ID, mem.Surface.ParentID, mem.OriginalLine.Id).Execute();
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
            if (memento == null || memento.Surface == null)
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
