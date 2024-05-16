using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action allows the user to split a <see cref="LineConf"/>.
    /// </summary>
    public class LineSplitAction : DrawableAction
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
        /// revert or repeat a <see cref="LineSplitAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// Is the configuration of line before it was splitted.
            /// </summary>
            public readonly LineConf OriginalLine;
            /// <summary>
            /// Is the drawable on that the lines are displayed.
            /// </summary>
            public readonly DrawableConfig Drawable;
            /// <summary>
            /// The list of lines that resulted from splitting the original line.
            /// </summary>
            public readonly List<LineConf> Lines;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="originalLine">Is the configuration of line before it was splitted.</param>
            /// <param name="drawable">The drawable where the lines are displayed</param>
            /// <param name="lines">The list of lines that resulted from splitting the original line</param>
            public Memento(GameObject originalLine, GameObject drawable, List<LineConf> lines)
            {
                OriginalLine = LineConf.GetLine(originalLine);
                Drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                Lines = lines;
            }
        }

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
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !isActive &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameFinder.HasDrawable(raycastHit.collider.gameObject))
                {
                    isActive = true;
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (hitObject.CompareTag(Tags.Line))
                    {
                        LineConf originLine = LineConf.GetLine(hitObject);
                        List<LineConf> lines = new();
                        NearestPoints.GetNearestPoints(hitObject, raycastHit.point,
                            out List<Vector3> positionsList, out List<int> matchedIndices);
                        GameLineSplit.Split(GameFinder.GetDrawable(hitObject), originLine,
                            matchedIndices, positionsList, lines, false);

                        /// Showes a notification if the split was successfully.
                        if (lines.Count > 1)
                        {
                            ShowNotification.Info("Line split",
                                "The original line was successfully split in " + lines.Count + " lines");
                            /// Marks the split position for a specific time.
                            MarkSplitPosition(hitObject, positionsList[matchedIndices[0]]);
                        }
                        memento = new Memento(hitObject, GameFinder.GetDrawable(hitObject), lines);
                        new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID,
                            memento.OriginalLine.id).Execute();
                        Destroyer.Destroy(hitObject);
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
        /// Marks the split point with a polygon with a radius of
        /// <see cref="ValueHolder.lineSplitMarkerRadius"/> and vertices count of <see cref="ValueHolder.lineSplitMarkerVertices"/>
        /// for <see cref="ValueHolder.lineSplitTimer"/> seconds.
        /// </summary>
        /// <param name="hitObject">The object that has been split.</param>
        /// <param name="splitPos">The first split position.</param>
        private void MarkSplitPosition(GameObject hitObject, Vector3 splitPos)
        {
            /// Calculates the pivot point for the marker.
            Vector3 position = hitObject.transform.TransformPoint(splitPos);
            GameObject drawable = GameFinder.GetDrawable(hitObject);
            position = GameDrawer.GetConvertedPosition(drawable, position);

            /// Calculates the negativ color for the marker.
            Color color = GetColor(hitObject);

            Color.RGBToHSV(color, out float H, out float S, out float V);
            /// Calculate the complementary color.
            float negativH = (H + 0.5f) % 1f;
            Color negativColor = Color.HSVToRGB(negativH, S, V);

            /// If the color does not have a complementary color, take the default.
            if (color == negativColor)
            {
                negativColor = ValueHolder.lineSplitDefaultMarkerColor;
            }

            /// Calculates the positions of the marker polygon.
            Vector3[] positions = ShapePointsCalculator.Polygon(position,
                ValueHolder.lineSplitMarkerRadius, ValueHolder.lineSplitMarkerVertices);
            /// Creates the marker polygon.
            GameObject point = GameDrawer.DrawLine(drawable, DrawableHolder.GetRandomString(10), positions,
                GameDrawer.ColorKind.Monochrome,
                negativColor, negativColor, 0.01f,
                false, GameDrawer.LineKind.Solid, 1f, false);
            /// Sets the pivot point of the marker.
            GameDrawer.SetPivotShape(point, position);
            /// Adds the point on all clients.
            new DrawNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), LineConf.GetLine(point)).Execute();
            /// Adds a blink effect.
            point.AddComponent<BlinkEffect>();
            /// Adds the blink effect to the point on all clients.
            new AddBlinkEffectNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), point.name).Execute();
            /// Destroys the marker after the chosen time.
            Object.Destroy(point, ValueHolder.lineSplitTimer);
            new EraseAfterTimeNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), point.name, ValueHolder.lineSplitTimer).Execute();
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
            switch (holder.GetColorKind())
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
            GameObject drawable = memento.Drawable.GetDrawable();
            GameDrawer.ReDrawLine(drawable, memento.OriginalLine);
            new DrawNetAction(memento.Drawable.ID, memento.Drawable.ParentID, memento.OriginalLine).Execute();

            foreach (LineConf line in memento.Lines)
            {
                GameObject lineObj = GameFinder.FindChild(drawable, line.id);
                new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID, line.id).Execute();
                Destroyer.Destroy(lineObj);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes the original line and restores the sublines.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject drawable = memento.Drawable.GetDrawable();
            GameObject originObj = GameFinder.FindChild(drawable, memento.OriginalLine.id);
            new EraseNetAction(memento.Drawable.ID, memento.Drawable.ParentID, memento.OriginalLine.id).Execute();
            Destroyer.Destroy(originObj);

            foreach (LineConf line in memento.Lines)
            {
                GameDrawer.ReDrawLine(drawable, line);
                new DrawNetAction(memento.Drawable.ID, memento.Drawable.ParentID, line).Execute();
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
        /// <returns>the id of the line that was split.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Drawable == null)
            {
                return new();
            }
            else
            {
                return new() { memento.OriginalLine.id };
            }
        }
    }
}
