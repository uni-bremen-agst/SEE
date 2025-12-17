using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows the user to split a <see cref="LineConf"/>.
    /// </summary>
    public class LineSplitAction : LineAction
    {
        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LineSplit"/>.
        /// Specifically: Allows the user to split a line. One action run allows to split the line one time.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block is responsible for splitting the line.
                /// It searches for the nearest point on the line from the mouse position.
                /// Multiple line points may overlap, so it works with a list of nearest points.
                /// The line is split at the found points, and sublines are created,
                /// with their starting and ending points corresponding to the splitting point.
                if (Selector.SelectQueryHasDrawableSurface(out RaycastHit raycastHit)
                    && !isActive)
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (hitObject.CompareTag(Tags.Line))
                    {
                        isActive = true;
                        LineConf originLine = LineConf.GetLine(hitObject);
                        List<LineConf> lines = new();
                        NearestPoints.GetNearestPoints(hitObject, raycastHit.point,
                            out List<Vector3> positionsList, out List<int> matchedIndices);
                        GameLineSplit.Split(GameFinder.GetDrawableSurface(hitObject), originLine,
                            matchedIndices, positionsList, lines, false);

                        /// Showes a notification if the split was successfully.
                        if (lines.Count > 1)
                        {
                            ShowNotification.Info("Line split",
                                "The original line was successfully split in " + lines.Count + " lines");
                            /// Marks the split position for a specific time.
                            MarkSplitPosition(hitObject, positionsList[matchedIndices[0]]);
                        }
                        memento = new Memento(hitObject, GameFinder.GetDrawableSurface(hitObject), lines);
                        new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID,
                            memento.OriginalLine.ID).Execute();
                        Destroyer.Destroy(hitObject);
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
        /// Marks the split point with a polygon with a radius of
        /// <see cref="ValueHolder.LineSplitMarkerRadius"/> and vertices count of <see cref="ValueHolder.LineSplitMarkerVertices"/>
        /// for <see cref="ValueHolder.LineSplitTimer"/> seconds.
        /// </summary>
        /// <param name="hitObject">The object that has been split.</param>
        /// <param name="splitPos">The first split position.</param>
        private void MarkSplitPosition(GameObject hitObject, Vector3 splitPos)
        {
            /// Calculates the pivot point for the marker.
            Vector3 position = hitObject.transform.TransformPoint(splitPos);
            GameObject surface = GameFinder.GetDrawableSurface(hitObject);
            position = GameDrawer.GetConvertedPosition(surface, position);

            /// Calculates the negativ color for the marker.
            Color color = GetColor(hitObject);
            Color negativColor = ColorConverter.Complementary(color);

            /// Calculates the positions of the marker polygon.
            Vector3[] positions = ShapePointsCalculator.Polygon(position,
                ValueHolder.LineSplitMarkerRadius, ValueHolder.LineSplitMarkerVertices);
            /// Creates the marker polygon.
            GameObject point = GameDrawer.DrawLine(surface, RandomStrings.GetRandomString(10), positions,
                GameDrawer.ColorKind.Monochrome,
                negativColor, negativColor, 0.01f,
                false, GameDrawer.LineKind.Solid, 1f, increaseCurrentOrder: false);
            /// Sets the pivot point of the marker.
            GameDrawer.SetPivotShape(point, position);
            /// Adds the point on all clients.
            new DrawNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), LineConf.GetLine(point)).Execute();
            /// Adds a blink effect.
            point.AddComponent<BlinkEffect>();
            /// Adds the blink effect to the point on all clients.
            new AddBlinkEffectNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), point.name).Execute();
            /// Destroys the marker after the chosen time.
            Object.Destroy(point, ValueHolder.LineSplitTimer);
            new EraseAfterTimeNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), point.name, ValueHolder.LineSplitTimer).Execute();
        }

        /// <summary>
        /// Delivers the color for the complementary color calculation.
        /// For <see cref="GameDrawer.ColorKind.Monochrome"/>, it is the normal line color.
        /// For <see cref="GameDrawer.ColorKind.Gradient"/>, it is a mix of the start and the end color.
        /// For <see cref="GameDrawer.ColorKind.TwoDashed"/>, it is a mix of the two material colors.
        /// </summary>
        /// <param name="line">The split line.</param>
        /// <returns>The color for the complementary color calculation.</returns>
        private Color GetColor(GameObject line)
        {
            Color color = Color.magenta;
            LineValueHolder holder = line.GetComponent<LineValueHolder>();
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            switch (holder.ColorKind)
            {
                case GameDrawer.ColorKind.Monochrome:
                    color = line.GetColor();
                    break;
                case GameDrawer.ColorKind.Gradient:
                    color = (renderer.startColor + renderer.endColor) / 2;
                    break;
                case GameDrawer.ColorKind.TwoDashed:
                    color = (renderer.materials[0].color + renderer.materials[1].color) / 2;
                    break;
            }
            return color;
        }

        /// <summary>
        /// Reverts this action, i.e., it deletes the sublines and restores the original line.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject surface = memento.Surface.GetDrawableSurface();
            GameDrawer.ReDrawLine(surface, memento.OriginalLine);
            new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.OriginalLine).Execute();

            foreach (LineConf line in memento.Lines)
            {
                GameObject lineObj = GameFinder.FindChild(surface, line.ID);
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, line.ID).Execute();
                Destroyer.Destroy(lineObj);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes the original line and restores the sublines.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject surface = memento.Surface.GetDrawableSurface();
            GameObject originObj = GameFinder.FindChild(surface, memento.OriginalLine.ID);
            new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.OriginalLine.ID).Execute();
            Destroyer.Destroy(originObj);

            foreach (LineConf line in memento.Lines)
            {
                GameDrawer.ReDrawLine(surface, line);
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, line).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="LineSplitAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineSplitAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LineSplitAction();
        }

        /// <summary>
        /// A new instance of <see cref="LineSplitAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LineSplitAction"/></returns>
        public override IReversibleAction NewInstance()
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
        /// </summary>
        /// <returns>the ID of the line that was split.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Surface == null)
            {
                return new();
            }
            else
            {
                return new() { memento.OriginalLine.ID };
            }
        }
    }
}
