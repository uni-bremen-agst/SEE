using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Applies line-cap configurations to lines and adjusts their geometry.
    /// </summary>
    public static class GameLineCapApplicator
    {
        /// <summary>
        /// Applies the given start and end line caps to the specified line.
        /// </summary>
        public static void ApplyLineCaps(
            GameObject shape,
            LineCapConf startConf,
            LineCapConf endConf,
            Color? fillOutColor = null,
            bool useCapConfVisuals = false)
        {
            ApplyLineCaps(
                shape,
                startConf,
                endConf,
                fillOutColor,
                useCapConfVisuals,
                useCapConfVisuals);
        }

        /// <summary>
        /// Applies the given start and end line caps and allows deciding
        /// separately whether each cap uses its own visual configuration.
        /// </summary>
        public static void ApplyLineCaps(
            GameObject shape,
            LineCapConf startConf,
            LineCapConf endConf,
            Color? fillOutColor,
            bool useStartCapConfVisuals,
            bool useEndCapConfVisuals)
        {
            if (shape == null)
            {
                return;
            }

            GameLineCapRenderer.RemoveLineCaps(shape);

            LineConf line = LineConf.GetLine(shape);
            if (line == null)
            {
                return;
            }

            LineCapValueHolder capValueHolder = shape.GetComponent<LineCapValueHolder>();

            if (capValueHolder != null)
            {
                capValueHolder.StartCap =
                    startConf?.CapKind ?? LineCap.None;

                capValueHolder.EndCap =
                    endConf?.CapKind ?? LineCap.None;

                capValueHolder.StartCapUsesOwnVisuals =
                    startConf != null
                    && startConf.CapKind != LineCap.None
                    && useStartCapConfVisuals;

                capValueHolder.EndCapUsesOwnVisuals =
                    endConf != null
                    && endConf.CapKind != LineCap.None
                    && useEndCapConfVisuals;
            }

            Vector3[] originalPositions = GameLineGeometry.GetOriginalLinePositions(shape);

            if (originalPositions == null || originalPositions.Length < 2)
            {
                return;
            }

            Vector3[] shortenedPositions = new Vector3[originalPositions.Length];

            Array.Copy(
                originalPositions,
                shortenedPositions,
                originalPositions.Length);

            line.RendererPositions = originalPositions;

            ApplyLineCapToPositions(
                shape,
                line,
                shortenedPositions,
                startConf,
                LineCapPosition.Start,
                useStartCapConfVisuals);

            ApplyLineCapToPositions(
                shape,
                line,
                shortenedPositions,
                endConf,
                LineCapPosition.End,
                useEndCapConfVisuals);

            GameLineDrawer.Drawing(
                shape,
                shortenedPositions,
                fillOutColor,
                preserveFillOutColliderState: true);

            GameLineCapRenderer.DrawLineCapObject(
                shape,
                line,
                startConf,
                LineCapPosition.Start,
                useStartCapConfVisuals);

            GameLineCapRenderer.DrawLineCapObject(
                shape,
                line,
                endConf,
                LineCapPosition.End,
                useEndCapConfVisuals);
        }

        /// <summary>
        /// Applies the shortening required for the given line cap.
        /// </summary>
        private static void ApplyLineCapToPositions(
            GameObject shape,
            LineConf line,
            Vector3[] positions,
            LineCapConf conf,
            LineCapPosition position,
            bool useCapConfVisuals)
        {
            if (shape == null || line == null || positions == null
                || conf == null || conf.CapKind == LineCap.None
                || !CanCalculate(line, position))
            {
                return;
            }

            List<LineCapShape> capShapes =
                GetShapes(conf, line, position, useCapConfVisuals);

            LineCapShape capShape = capShapes[0];

            Vector3 anchor;
            Vector3 direction;

            if (position == LineCapPosition.Start)
            {
                anchor = line.RendererPositions[0];
                direction =
                    line.RendererPositions[0] - line.RendererPositions[1];
            }
            else
            {
                anchor =
                    line.RendererPositions[line.RendererPositions.Length - 1];

                direction =
                    line.RendererPositions[line.RendererPositions.Length - 1]
                    - line.RendererPositions[line.RendererPositions.Length - 2];
            }

            if (direction == Vector3.zero)
            {
                return;
            }

            float angleInDegrees =
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Vector3 rotatedConnectionPoint =
                RotatePoint(capShape.ConnectionPoint, angleInDegrees);

            if (position == LineCapPosition.Start)
            {
                positions[0] = anchor + rotatedConnectionPoint;
            }
            else
            {
                positions[positions.Length - 1] =
                    anchor + rotatedConnectionPoint;
            }
        }

        /// <summary>
        /// Rotates a local point around the origin by the given angle.
        /// </summary>
        private static Vector3 RotatePoint(
            Vector3 point,
            float angleInDegrees)
        {
            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleInRadians);
            float sin = Mathf.Sin(angleInRadians);

            float x = (point.x * cos) - (point.y * sin);
            float y = (point.x * sin) + (point.y * cos);

            return new Vector3(x, y, point.z);
        }
    }
}
