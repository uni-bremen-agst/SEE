using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Provides rendering and management of line-cap objects.
    /// </summary>
    public static class GameLineCapRenderer
    {
        /// <summary>
        /// Removes all generated line-cap child objects from the given shape.
        /// </summary>
        /// <param name="shape">The shape whose generated line caps should be removed.</param>
        public static void RemoveLineCaps(GameObject shape)
        {
            if (shape == null)
            {
                return;
            }

            List<GameObject> capsToRemove = new();

            foreach (Transform child in shape.transform)
            {
                if (child.gameObject.name.StartsWith(
                        ValueHolder.LineStartCapPrefix,
                        StringComparison.Ordinal)
                    || child.gameObject.name.StartsWith(
                        ValueHolder.LineEndCapPrefix,
                        StringComparison.Ordinal))
                {
                    capsToRemove.Add(child.gameObject);
                }
            }

            foreach (GameObject cap in capsToRemove)
            {
                // Line caps are removed immediately because they may be recreated
                // in the same frame. Delayed destruction would keep the old object
                // alive until frame end and could cause name collisions.
                GameObject.DestroyImmediate(cap);
            }
        }

        /// <summary>
        /// Draws a line cap as a child object of the given line shape using the
        /// visual settings of the parent line.
        /// </summary>
        /// <param name="shape">The parent line shape.</param>
        /// <param name="prefix">The prefix describing the cap position.</param>
        /// <param name="points">The local points of the line cap.</param>
        /// <param name="anchor">The anchor point in the local space of the parent line.</param>
        /// <param name="angleInDegrees">The rotation angle of the cap in degrees.</param>
        /// <param name="line">The parent line configuration.</param>
        /// <param name="capKind">The cap kind.</param>
        /// <returns>The created or updated line-cap object.</returns>
        public static GameObject DrawLineCap(
            GameObject shape,
            string prefix,
            Vector3[] points,
            Vector3 anchor,
            float angleInDegrees,
            LineConf line,
            LineCap capKind)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LineCapConf capConf = GameLineCapConfiguration.CreateLineCapConf(line, null, capKind);

            return DrawLineCap(
                shape,
                prefix,
                points,
                anchor,
                angleInDegrees,
                capConf,
                useOwnVisuals: false);
        }

        /// <summary>
        /// Draws a line cap using the visual settings stored in the given
        /// <see cref="LineCapConf"/>.
        /// </summary>
        /// <param name="shape">The parent line shape.</param>
        /// <param name="name">The name of the line cap.</param>
        /// <param name="points">The local points of the line cap.</param>
        /// <param name="anchor">The anchor point in the local space of the parent line.</param>
        /// <param name="angleInDegrees">The rotation angle of the cap in degrees.</param>
        /// <param name="capConf">The line-cap configuration.</param>
        /// <param name="useOwnVisuals">
        /// Whether the cap uses its own visual settings.
        /// </param>
        /// <returns>The created or updated line-cap object.</returns>
        public static GameObject DrawLineCap(
            GameObject shape,
            string name,
            Vector3[] points,
            Vector3 anchor,
            float angleInDegrees,
            LineCapConf capConf,
            bool useOwnVisuals)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (capConf == null)
            {
                throw new ArgumentNullException(nameof(capConf));
            }

            GameObject drawableSurface = GameFinder.GetDrawableSurface(shape);

            Color? fillOutColor =
                capConf.FillOutStatus && CanApplyFillOut(points)
                    ? capConf.FillOutColor
                    : null;

            GameObject capObject = GameLineDrawer.DrawLine(
                drawableSurface,
                name,
                points,
                capConf.ColorKind,
                capConf.PrimaryColor,
                capConf.SecondaryColor,
                capConf.Thickness,
                false,
                capConf.LineKind,
                capConf.Tiling,
                false,
                fillOutColor,
                false,
                false);

            GameLineGeometry.SetPivotShape(capObject, Vector3.zero);

            capObject.transform.SetParent(shape.transform, false);
            capObject.transform.localEulerAngles =
                new Vector3(0.0f, 0.0f, angleInDegrees);
            capObject.transform.localPosition =
                new Vector3(anchor.x, anchor.y, shape.transform.localPosition.z);
            capObject.tag = Tags.LineCap;

            LineCapValueHolder capValueHolder =
                shape.GetComponent<LineCapValueHolder>();

            if (name.StartsWith(ValueHolder.LineStartCapPrefix))
            {
                capValueHolder.StartCap = capConf.CapKind;
                capValueHolder.StartCapUsesOwnVisuals = useOwnVisuals;
            }
            else
            {
                capValueHolder.EndCap = capConf.CapKind;
                capValueHolder.EndCapUsesOwnVisuals = useOwnVisuals;
            }

            return capObject;
        }

        /// <summary>
        /// Returns all line-cap objects belonging to the given line.
        /// </summary>
        /// <param name="shape">The line whose cap objects should be returned.</param>
        /// <param name="isStartCap">
        /// True for start-cap objects, false for end-cap objects.
        /// </param>
        /// <returns>All matching line-cap objects.</returns>
        internal static List<GameObject> GetLineCapObjects(
            GameObject shape,
            bool isStartCap)
        {
            string capName = GetLineCapName(
                shape,
                isStartCap
                    ? ValueHolder.LineStartCapPrefix
                    : ValueHolder.LineEndCapPrefix);

            return shape.FindAllDescendantWithStartingName(capName);
        }

        /// <summary>
        /// Draws the line-cap object for the given configuration.
        /// </summary>
        internal static void DrawLineCapObject(
            GameObject shape,
            LineConf line,
            LineCapConf conf,
            LineCapPosition position,
            bool useCapConfVisuals)
        {
            if (shape == null || line == null
                || conf == null || conf.CapKind == LineCap.None
                || !CanCalculate(line, position))
            {
                return;
            }

            List<LineCapShape> capShapes = GetShapes(conf, line, position, useCapConfVisuals);

            Vector3 anchor;
            Vector3 direction;
            string prefix;

            if (position == LineCapPosition.Start)
            {
                anchor = line.RendererPositions[0];
                direction = line.RendererPositions[0] - line.RendererPositions[1];
                prefix = ValueHolder.LineStartCapPrefix;
            }
            else
            {
                anchor = line.RendererPositions[line.RendererPositions.Length - 1];
                direction =
                    line.RendererPositions[line.RendererPositions.Length - 1]
                    - line.RendererPositions[line.RendererPositions.Length - 2];
                prefix = ValueHolder.LineEndCapPrefix;
            }

            if (direction == Vector3.zero)
            {
                return;
            }

            float angleInDegrees =
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < capShapes.Count; i++)
            {
                LineCapShape capShape = capShapes[i];
                string name = GetLineCapName(shape, prefix) + "_" + i;

                if (useCapConfVisuals)
                {
                    DrawLineCap(
                        shape,
                        name,
                        capShape.Points,
                        anchor,
                        angleInDegrees,
                        conf,
                        true);
                }
                else
                {
                    DrawLineCap(
                        shape,
                        name,
                        capShape.Points,
                        anchor,
                        angleInDegrees,
                        line,
                        conf.CapKind);
                }
            }
        }

        /// <summary>
        /// Determines whether a fill-out can be applied to the given points.
        /// </summary>
        private static bool CanApplyFillOut(Vector3[] points)
        {
            return points != null && points.Distinct().Count() > 2;
        }

        /// <summary>
        /// Builds the line-cap object name for the given shape and prefix.
        /// </summary>
        private static string GetLineCapName(GameObject shape, string prefix)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            string shapeName = shape.name;

            if (shapeName.StartsWith(
                    ValueHolder.LinePrefix,
                    StringComparison.Ordinal))
            {
                shapeName =
                    shapeName.Substring(ValueHolder.LinePrefix.Length);
            }

            return prefix + shapeName;
        }
    }
}
