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
        /// Applies the given start and end line caps to the specified line
        /// and adjusts the visible geometry to accommodate them.
        /// Both caps use the same choice of visual configuration.
        /// </summary>
        /// <param name="shape">The line to which the caps are applied.</param>
        /// <param name="startConf">The start-cap configuration.</param>
        /// <param name="endConf">The end-cap configuration.</param>
        /// <param name="fillOutColor">
        /// The optional fill-out color used when redrawing the line.
        /// </param>
        /// <param name="useCapConfVisuals">
        /// Whether both caps use their own visual configurations
        /// instead of inheriting the main line's visual properties.
        /// </param>
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
        /// Applies the given start and end line caps to the specified line
        /// and adjusts its visible geometry.
        /// When a closing segment is too short for its end cap, the preceding
        /// segment may be used for capped loops or for open lines explicitly
        /// retaining the end cap of a previously split loop.
        /// Start caps always use the original first segment.
        /// The original logical positions are preserved independently of
        /// the rendered geometry.
        /// </summary>
        /// <param name="shape">The line to which the caps are applied.</param>
        /// <param name="startConf">The start-cap configuration.</param>
        /// <param name="endConf">The end-cap configuration.</param>
        /// <param name="fillOutColor">
        /// The optional fill-out color used when redrawing the line.
        /// </param>
        /// <param name="useStartCapConfVisuals">
        /// Whether the start cap uses its own visual configuration.
        /// </param>
        /// <param name="useEndCapConfVisuals">
        /// Whether the end cap uses its own visual configuration.
        /// </param>
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

            LineCapValueHolder capValueHolder =
                shape.GetComponent<LineCapValueHolder>();

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

            Vector3[] originalPositions =
                GameLineGeometry.GetOriginalLinePositions(shape);

            if (originalPositions == null
                || originalPositions.Length < 2)
            {
                return;
            }

            bool hasStartCap =
                startConf != null
                && startConf.CapKind != LineCap.None;

            bool hasEndCap =
                endConf != null
                && endConf.CapKind != LineCap.None;

            bool useExplicitLoopCapGeometry =
                line.Loop && (hasStartCap || hasEndCap);

            LineAnchorValueHolder anchorHolder =
                shape.GetComponent<LineAnchorValueHolder>();

            if (anchorHolder == null)
            {
                anchorHolder =
                    shape.AddComponent<LineAnchorValueHolder>();
            }

            Vector3[] shortenedPositions;

            if (useExplicitLoopCapGeometry)
            {
                anchorHolder.UsesExplicitLoopCapGeometry = true;

                anchorHolder.OriginalRendererPositions =
                    new Vector3[originalPositions.Length];

                Array.Copy(
                    originalPositions,
                    anchorHolder.OriginalRendererPositions,
                    originalPositions.Length);

                shortenedPositions =
                    new Vector3[originalPositions.Length + 1];

                Array.Copy(
                    originalPositions,
                    shortenedPositions,
                    originalPositions.Length);

                shortenedPositions[shortenedPositions.Length - 1] =
                    originalPositions[0];

                shape.GetComponent<LineRenderer>().loop = false;
            }
            else
            {
                shortenedPositions =
                    new Vector3[originalPositions.Length];

                Array.Copy(
                    originalPositions,
                    shortenedPositions,
                    originalPositions.Length);

                anchorHolder.UsesExplicitLoopCapGeometry = false;
                anchorHolder.OriginalRendererPositions = null;

                shape.GetComponent<LineRenderer>().loop = line.Loop;
            }

            bool useClosingSegmentFallback = false;
            Vector3[] capCalculationPositions = null;

            bool mayPreserveEndCapDirection =
                useExplicitLoopCapGeometry
                || (!line.Loop && line.UsePreviousSegmentForEndCap);

            if (mayPreserveEndCapDirection
                && hasEndCap
                && originalPositions.Length >= 3)
            {
                int lastIndex = originalPositions.Length - 1;

                Vector3 anchor = line.Loop
                    ? originalPositions[0]
                    : originalPositions[lastIndex];

                Vector3 nearPoint = line.Loop
                    ? originalPositions[lastIndex]
                    : originalPositions[lastIndex - 1];

                Vector3 previousPoint = line.Loop
                    ? originalPositions[lastIndex - 1]
                    : originalPositions[lastIndex - 2];

                float closingLength =
                    Vector3.Distance(nearPoint, anchor);

                float incomingLength =
                    Vector3.Distance(previousPoint, anchor);

                if (incomingLength > 0.0001f
                    && closingLength < incomingLength * 0.1f)
                {
                    capCalculationPositions =
                        new Vector3[originalPositions.Length];

                    Array.Copy(
                        originalPositions,
                        capCalculationPositions,
                        originalPositions.Length);

                    if (line.Loop)
                    {
                        capCalculationPositions[lastIndex] =
                            previousPoint;
                    }
                    else
                    {
                        capCalculationPositions[lastIndex - 1] =
                            previousPoint;
                    }

                    line.RendererPositions = capCalculationPositions;

                    List<LineCapShape> referenceShapes =
                        GetShapes(
                            endConf,
                            line,
                            LineCapPosition.End,
                            useEndCapConfVisuals);

                    float capExtent = 0.0f;

                    foreach (LineCapShape capShape in referenceShapes)
                    {
                        capExtent = Mathf.Max(
                            capExtent,
                            Mathf.Abs(capShape.ConnectionPoint.x));

                        foreach (Vector3 capPoint in capShape.Points)
                        {
                            capExtent = Mathf.Max(
                                capExtent,
                                Mathf.Abs(capPoint.x));
                        }
                    }

                    useClosingSegmentFallback =
                        closingLength < capExtent;

                    if (useClosingSegmentFallback
                        && !useExplicitLoopCapGeometry)
                    {
                        anchorHolder.OriginalRendererPositions =
                            new Vector3[originalPositions.Length];

                        Array.Copy(
                            originalPositions,
                            anchorHolder.OriginalRendererPositions,
                            originalPositions.Length);
                    }
                }
            }

            Vector3[] endCapCalculationPositions =
                useClosingSegmentFallback
                    ? capCalculationPositions
                    : originalPositions;

            // The start cap must always use the original first segment.
            // On a three-point open line, the end-cap fallback can
            // otherwise replace the second point with the first point.
            line.RendererPositions = originalPositions;

            ApplyLineCapToPositions(
                shape,
                line,
                shortenedPositions,
                startConf,
                LineCapPosition.Start,
                useStartCapConfVisuals);

            // Only the end cap may use the corrected closing direction.
            line.RendererPositions = endCapCalculationPositions;

            ApplyLineCapToPositions(
                shape,
                line,
                shortenedPositions,
                endConf,
                LineCapPosition.End,
                useEndCapConfVisuals);

            if (useClosingSegmentFallback)
            {
                int lastRendererIndex =
                    shortenedPositions.Length - 1;

                Vector3 previousPoint = line.Loop
                    ? originalPositions[originalPositions.Length - 2]
                    : originalPositions[originalPositions.Length - 3];

                shortenedPositions[lastRendererIndex - 1] =
                    Vector3.Lerp(
                        previousPoint,
                        shortenedPositions[lastRendererIndex],
                        0.5f);
            }

            GameLineDrawer.Drawing(
                shape,
                shortenedPositions,
                fillOutColor,
                preserveFillOutColliderState: true);

            // The start-cap object must use the same original segment
            // that was used to shorten the visible line.
            line.RendererPositions = originalPositions;

            GameLineCapRenderer.DrawLineCapObject(
                shape,
                line,
                startConf,
                LineCapPosition.Start,
                useStartCapConfVisuals);

            // Reapply the optional fallback for the end-cap object.
            line.RendererPositions = endCapCalculationPositions;

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
                    line.RendererPositions[0]
                    - line.RendererPositions[1];
            }
            else if (line.Loop)
            {
                anchor = line.RendererPositions[0];

                direction =
                    line.RendererPositions[0]
                    - line.RendererPositions[line.RendererPositions.Length - 1];
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
