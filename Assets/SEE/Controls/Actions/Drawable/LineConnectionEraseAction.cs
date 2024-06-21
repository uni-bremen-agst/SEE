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
    /// This action allows the user to delete a line connector between two points.
    /// Is intended for shapes.
    /// </summary>
    class LineConnectionEraseAction : DrawableAction
    {
        /// <summary>
        /// True if the action is active.
        /// </summary>
        private bool isActive = false;
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to
        /// revert or repeat a <see cref="LineConnectionEraseAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// Is the configuration of line before it was modified.
            /// </summary>
            public readonly LineConf OriginalLine;
            /// <summary>
            /// Is the drawable surface on which the lines are displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The list of lines that resulted from modify the original line.
            /// </summary>
            public readonly List<LineConf> Lines;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before it was modified.</param>
            /// <param name="surface">The drawable surface where the lines are displayed</param>
            /// <param name="lines">The list of lines that resulted from modify the original line</param>
            public Memento(GameObject originalLine, GameObject surface, List<LineConf> lines)
            {
                OriginalLine = LineConf.GetLine(originalLine);
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Lines = lines;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LineConnectorErase"/>.
        /// Specifically: Allows the user to delete a line connection between two points.
        /// One action run allows to delete the line connections of one point.
        /// Is intended for shapes.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block is responsible for deleting the line connectors of the given point.
                /// It searches for the nearest point on the line from the mouse position.
                /// Multiple line points may overlap, so it works with a list of nearest points.
                /// The line is split at the found points, and sublines are created,
                /// with their starting and ending points corresponding to the splitting point.
                if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit)
                    && !isActive)
                {
                    isActive = true;
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (hitObject.CompareTag(Tags.Line))
                    {
                        LineConf originLine = LineConf.GetLine(hitObject);
                        List<LineConf> lines = new();
                        NearestPoints.GetNearestPoints(hitObject, raycastHit.point,
                            out List<Vector3> positionsList, out List<int> matchedIndices);
                        GameLineSplit.EraseLinePointConnection(GameFinder.GetDrawableSurface(hitObject), originLine,
                            matchedIndices, positionsList, lines);

                        memento = new Memento(hitObject, GameFinder.GetDrawableSurface(hitObject), lines);
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
            GameObject surface = memento.Surface.GetDrawable();
            GameDrawer.ReDrawLine(surface, memento.OriginalLine);
            new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.OriginalLine).Execute();

            foreach (LineConf line in memento.Lines)
            {
                GameObject lineObj = GameFinder.FindChild(surface, line.Id);
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, line.Id).Execute();
                Destroyer.Destroy(lineObj);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes the original line and restores the sublines.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject surface = memento.Surface.GetDrawable();
            GameObject originObj = GameFinder.FindChild(surface, memento.OriginalLine.Id);
            new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.OriginalLine.Id).Execute();
            Destroyer.Destroy(originObj);

            foreach (LineConf line in memento.Lines)
            {
                GameDrawer.ReDrawLine(surface, line);
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, line).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="LineConnectionEraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineConnectionEraseAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LineConnectionEraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="LineConnectionEraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineConnectionEraseAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LineConnectorErase"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LineConnectionErase;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>the id of the line that was splitted.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Surface == null)
            {
                return new();
            }
            else
            {
                return new() { memento.OriginalLine.Id };
            }
        }
    }
}
