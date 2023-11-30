using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using SEE.Game.UI.Notification;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows to split a line.
    /// </summary>
    class LineSplitAction : AbstractPlayerAction
    {
        /// <summary>
        /// Represents that the action is active.
        /// </summary>
        private bool isActive = false;
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LineSplitAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// Is the configuration of line before it was splitted.
            /// </summary>
            public readonly LineConf originalLine;
            /// <summary>
            /// Is the drawable on that the lines are displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// The list of lines that resulted from splitting the original line.
            /// </summary>
            public readonly List<LineConf> lines;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before it was splitted.</param>
            /// <param name="drawable">The drawable where the lines are displayed</param>
            /// <param name="lines">The list of lines that resulted from splitting the original line</param>
            public Memento(GameObject originalLine, GameObject drawable, List<LineConf> lines)
            {
                this.originalLine = LineConf.GetLine(originalLine);
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.lines = lines;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LineSplit"/>.
        /// Specifically: Allows the user to split a line. One action run allows to split the line one time.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block is responsible for splitting the line.
                /// It searches for the nearest point on the line from the mouse position.
                /// Multiple line points may overlap, so it works with a list of nearest points.
                /// The line is split at the found points, and sublines are created, 
                /// with their starting and ending points corresponding to the splitting point.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isActive &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameFinder.hasDrawable(raycastHit.collider.gameObject))
                {
                    isActive = true;
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (hittedObject.CompareTag(Tags.Line))
                    {
                        LineRenderer lineRenderer = hittedObject.GetComponent<LineRenderer>();
                        Vector3[] positions = new Vector3[lineRenderer.positionCount];
                        lineRenderer.GetPositions(positions);
                        List<Vector3> positionsList = positions.ToList();
                        LineConf originLine = LineConf.GetLine(hittedObject);

                        Vector3[] transformedPositions = new Vector3[positions.Length];
                        Array.Copy(sourceArray: positions, destinationArray: transformedPositions, length: positions.Length);
                        hittedObject.transform.TransformPoints(transformedPositions);
                        List<LineConf> lines = new();
                        List<int> matchedIndices = NearestPoints.GetNearestIndices(transformedPositions, raycastHit.point);
                        GameLineSplit.Split(GameFinder.GetDrawable(hittedObject), originLine, matchedIndices, positionsList, lines, false);
                        if (lines.Count > 1)
                        {
                            ShowNotification.Info("Line splitted", "The original line was successfully splitted in " + lines.Count + " lines");
                        }

                        memento = new Memento(hittedObject, GameFinder.GetDrawable(hittedObject), lines);
                        new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.originalLine.id).Execute();
                        Destroyer.Destroy(hittedObject);
                    }
                }
                /// This block completes the action.
                if (Input.GetMouseButtonUp(0) && isActive)
                {
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., it deletes the sublines and restores the original line.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject drawable = memento.drawable.GetDrawable();
            GameDrawer.ReDrawLine(drawable, memento.originalLine);
            new DrawOnNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.originalLine).Execute();

            foreach (LineConf line in memento.lines)
            {
                GameObject lineObj = GameFinder.FindChild(drawable, line.id);
                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, line.id).Execute();
                Destroyer.Destroy(lineObj);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes the original line and restores the sublines.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject drawable = memento.drawable.GetDrawable();
            GameObject originObj = GameFinder.FindChild(drawable, memento.originalLine.id);
            new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.originalLine.id).Execute();
            Destroyer.Destroy(originObj);

            foreach (LineConf line in memento.lines)
            {
                GameDrawer.ReDrawLine(drawable, line);
                new DrawOnNetAction(memento.drawable.ID, memento.drawable.ParentID, line).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="LineSplitAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineSplitAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LineSplitAction();
        }

        /// <summary>
        /// A new instance of <see cref="LineSplitAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineSplitAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.LineSplit"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LineSplit;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the id of the line that was splitted.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string> { memento.originalLine.id };
            }
        }
    }
}
