using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Provides geometry-related functionality for drawable lines, including
    /// pivot placement, collider updates, original anchors, and position handling.
    /// </summary>
    public static class GameLineGeometry
    {
        /// <summary>
        /// Sets the pivot of the line to the center of the line.
        /// For an odd number of positions, the pivot is placed precisely at the midpoint.
        /// For an even number, the midpoint is calculated by adding the two middle points and
        /// dividing by two, obtaining the center of the two middle points.
        ///
        /// After determining the midpoint, the line positions are converted to world space,
        /// and the line is shifted to the midpoint.
        /// Subsequently, the world space coordinates are converted back to local,
        /// ensuring that the visual representation of the line remains unchanged while
        /// the pivot is shifted.
        /// </summary>
        /// <param name="line">The line in which the pivot should be set.</param>
        /// <param name="fillOutColor">
        /// The color for filling out the line, or null if the line should not be filled out.
        /// </param>
        /// <returns>The line with the pivot in the middle.</returns>
        public static GameObject SetPivot(GameObject line, Color? fillOutColor = null)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();
                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);

                Vector3 middlePos;

                if (positions.Length % 2 == 1)
                {
                    middlePos = positions[(int)Mathf.Round(positions.Length / 2)];
                }
                else
                {
                    Vector3 left = positions[positions.Length / 2 - 1];
                    Vector3 right = positions[positions.Length / 2];
                    middlePos = (left + right) / 2;
                }

                middlePos.z = line.transform.localPosition.z;

                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(
                    sourceArray: positions,
                    destinationArray: convertedPositions,
                    length: positions.Length);

                line.transform.TransformPoints(convertedPositions);

                line.transform.localPosition = middlePos;

                line.transform.InverseTransformPoints(convertedPositions);

                GameLineDrawer.Drawing(line, convertedPositions);

                GameLineDrawer.FinishDrawing(
                    line,
                    renderer.loop,
                    fillOutColor);

                UpdateOriginalAnchors(line, convertedPositions);
            }

            return line;
        }

        /// <summary>
        /// Changes the pivot point of a line.
        /// Will be needed for <see cref="GameLineSplit"/>.
        /// </summary>
        /// <param name="line">The line whose pivot point should be changed.</param>
        /// <returns><paramref name="line"/> with the new pivot point.</returns>
        public static GameObject ChangePivot(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();
                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);

                Vector3 middlePos;

                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(
                    sourceArray: positions,
                    destinationArray: convertedPositions,
                    length: positions.Length);

                line.transform.TransformPoints(convertedPositions);

                if (convertedPositions.Length % 2 == 1)
                {
                    middlePos =
                        convertedPositions[
                            (int)Mathf.Round(convertedPositions.Length / 2)];
                }
                else
                {
                    Vector3 left =
                        convertedPositions[convertedPositions.Length / 2 - 1];

                    Vector3 right =
                        convertedPositions[convertedPositions.Length / 2];

                    middlePos = (left + right) / 2;
                }

                line.transform.position = middlePos;

                line.transform.InverseTransformPoints(convertedPositions);

                GameLineDrawer.Drawing(line, convertedPositions);
                GameLineDrawer.FinishDrawing(line, renderer.loop);
            }

            return line;
        }

        /// <summary>
        /// Sets the pivot point for shapes.
        /// In this case, the pivot point is placed at the original hit point of creation,
        /// corresponding to the center of the shape.
        /// </summary>
        /// <param name="line">The shape for which the pivot point should be set.</param>
        /// <param name="middlePos">The center position for the shape.</param>
        /// <param name="fillOutColor">
        /// The color for filling out the line, or null if the line should not be filled out.
        /// </param>
        /// <param name="updateOriginalAnchors">
        /// Whether the original anchors of the main line should be updated after the pivot change.
        /// This must be false for generated line-cap objects.
        /// </param>
        /// <returns>The modified <paramref name="line"/> with the new pivot applied.</returns>
        public static GameObject SetPivotShape(
            GameObject line,
            Vector3 middlePos,
            Color? fillOutColor = null,
            bool updateOriginalAnchors = false)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();

                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);

                middlePos.z = line.transform.localPosition.z;

                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(
                    sourceArray: positions,
                    destinationArray: convertedPositions,
                    length: positions.Length);

                line.transform.TransformPoints(convertedPositions);

                line.transform.localPosition = middlePos;

                line.transform.InverseTransformPoints(convertedPositions);

                GameLineDrawer.Drawing(line, convertedPositions);

                GameLineDrawer.FinishDrawing(
                    line,
                    renderer.loop,
                    fillOutColor);

                if (updateOriginalAnchors)
                {
                    UpdateOriginalAnchors(line, convertedPositions);
                }
            }

            return line;
        }

        /// <summary>
        /// Refreshes the mesh collider of the line.
        /// The mesh for the mesh collider is recalculated.
        /// </summary>
        /// <param name="line">The line whose mesh collider should be refreshed.</param>
        public static void RefreshCollider(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                MeshCollider collider = line.GetComponent<MeshCollider>();

                Mesh mesh = new();
                lineRenderer.BakeMesh(mesh);

                if (mesh.vertices.Distinct().Count() >= 3)
                {
                    collider.sharedMesh = mesh;
                }
            }
        }

        /// <summary>
        /// Gets the original unshortened line positions for the given line.
        /// The first and last positions are restored from the stored original anchors
        /// if available.
        /// </summary>
        /// <param name="shape">The line GameObject.</param>
        /// <returns>
        /// A copy of the original line positions if available, otherwise a copy of the
        /// current renderer positions.
        /// </returns>
        internal static Vector3[] GetOriginalLinePositions(GameObject shape)
        {
            LineConf line = LineConf.GetLine(shape);

            if (line == null
                || line.RendererPositions == null
                || line.RendererPositions.Length < 2)
            {
                return null;
            }

            Vector3[] originalPositions =
                new Vector3[line.RendererPositions.Length];

            Array.Copy(
                line.RendererPositions,
                originalPositions,
                line.RendererPositions.Length);

            originalPositions[0] = line.OriginalStartAnchor;
            originalPositions[originalPositions.Length - 1] =
                line.OriginalEndAnchor;

            return originalPositions;
        }

        /// <summary>
        /// Moves the specified points of the given line to a new position.
        /// </summary>
        /// <param name="line">The line whose points should be moved.</param>
        /// <param name="indices">
        /// The indices of the points to move. All specified points receive the same position.
        /// </param>
        /// <param name="point">The new point position.</param>
        public static void MovePoint(
            GameObject line,
            List<int> indices,
            Vector3 point)
        {
            Vector3[] originalPositions =
                GetOriginalLinePositions(line);

            if (originalPositions == null)
            {
                return;
            }

            foreach (int index in indices)
            {
                if (index < 0 || index >= originalPositions.Length)
                {
                    continue;
                }

                originalPositions[index] =
                    new Vector3(
                        point.x,
                        point.y,
                        originalPositions[index].z);
            }

            ApplyOriginalLinePositions(
                line,
                originalPositions);
        }

        /// <summary>
        /// Updates the stored original anchors of the given line.
        /// Existing values are overwritten.
        /// </summary>
        /// <param name="line">The line whose anchors should be updated.</param>
        /// <param name="positions">The new original positions.</param>
        public static void UpdateOriginalAnchors(
            GameObject line,
            Vector3[] positions)
        {
            if (line == null
                || positions == null
                || positions.Length < 2)
            {
                return;
            }

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            if (anchorHolder == null)
            {
                anchorHolder =
                    line.AddComponent<LineAnchorValueHolder>();
            }

            anchorHolder.OriginalStartAnchor =
                new Vector3(
                    positions[0].x,
                    positions[0].y,
                    0.0f);

            anchorHolder.OriginalEndAnchor =
                new Vector3(
                    positions[positions.Length - 1].x,
                    positions[positions.Length - 1].y,
                    0.0f);

            anchorHolder.HasOriginalAnchors = true;
        }

        /// <summary>
        /// Applies original, unshortened line positions to the given line.
        /// This is required for lines with line caps because their renderer positions
        /// may be visually shortened.
        /// </summary>
        /// <param name="line">The line whose positions should be updated.</param>
        /// <param name="positions">The original, unshortened line positions.</param>
        public static void ApplyOriginalLinePositions(
            GameObject line,
            Vector3[] positions)
        {
            if (line == null
                || positions == null
                || positions.Length < 2)
            {
                return;
            }

            UpdateOriginalAnchors(line, positions);

            LineConf lineConf = LineConf.GetLine(line);
            if (lineConf == null)
            {
                return;
            }

            Color? fillOutColor =
                LineConf.GetFillOutColor(lineConf);

            GameLineDrawer.Drawing(
                line,
                positions,
                fillOutColor,
                preserveFillOutColliderState: true);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                lineConf.LineCapStart,
                lineConf.LineCapEnd,
                fillOutColor,
                useCapConfVisuals: true);

            RefreshCollider(line);
        }

        /// <summary>
        /// Applies the stored original anchors from the given line configuration
        /// to the line object.
        /// </summary>
        /// <param name="line">
        /// The line object whose anchor holder should be updated.
        /// </param>
        /// <param name="lineConf">
        /// The line configuration containing the stored original anchors.
        /// </param>
        internal static void ApplyStoredOriginalAnchors(
            GameObject line,
            LineConf lineConf)
        {
            if (line == null || lineConf == null)
            {
                return;
            }

            LineAnchorValueHolder anchorHolder =
                line.GetComponent<LineAnchorValueHolder>();

            if (anchorHolder == null)
            {
                anchorHolder =
                    line.AddComponent<LineAnchorValueHolder>();
            }

            anchorHolder.OriginalStartAnchor =
                lineConf.OriginalStartAnchor;

            anchorHolder.OriginalEndAnchor =
                lineConf.OriginalEndAnchor;

            anchorHolder.HasOriginalAnchors = true;
        }

        /// <summary>
        /// Sets the z positions of the given <paramref name="positions"/> to zero.
        /// This is necessary because a line renderer may change z values in case of
        /// an overlap, which causes problems when changing the order in layer.
        /// </summary>
        /// <param name="positions">The positions of the line renderer.</param>
        internal static void UpdateZPositions(ref Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].z = 0;
            }
        }

        /// <summary>
        /// Counts the different positions of a <see cref="Vector3"/> array.
        /// </summary>
        /// <param name="positions">The positions to be examined.</param>
        /// <returns>The count of different positions.</returns>
        public static int DifferentPositionCounter(Vector3[] positions)
        {
            return new List<Vector3>(positions)
                .Distinct()
                .ToList()
                .Count;
        }

        /// <summary>
        /// Counts the different positions of a shape GameObject.
        /// </summary>
        /// <param name="shape">The shape GameObject.</param>
        /// <returns>The count of different positions.</returns>
        public static int DifferentPositionCounter(GameObject shape)
        {
            if (shape.CompareTag(Tags.Line)
                || shape.CompareTag(Tags.LineCap))
            {
                LineRenderer renderer =
                    shape.GetComponent<LineRenderer>();

                Vector3[] positions =
                    new Vector3[renderer.positionCount];

                renderer.GetPositions(positions);

                return DifferentPositionCounter(positions);
            }

            return 0;
        }

        /// <summary>
        /// Calculates the number of different vertices of a mesh generated from
        /// the line points of the line renderer.
        /// </summary>
        /// <param name="line">The line holding the line renderer.</param>
        /// <returns>The number of different vertices.</returns>
        public static int DifferentMeshVerticesCounter(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer =
                    line.GetComponent<LineRenderer>();

                Mesh mesh = new();
                renderer.BakeMesh(mesh);

                return mesh.vertices
                    .Distinct()
                    .ToList()
                    .Count;
            }

            return 0;
        }

        /// <summary>
        /// Converts a world-space position into the local coordinate system used
        /// by drawable lines on the given surface.
        /// </summary>
        /// <param name="surface">The targeted drawable surface.</param>
        /// <param name="position">The world-space position to convert.</param>
        /// <returns>The converted local position.</returns>
        public static Vector3 GetConvertedPosition(
            GameObject surface,
            Vector3 position)
        {
            DrawableSetupManager.Setup(
                surface,
                out GameObject _,
                out GameObject attachedObjects);

            return attachedObjects.transform.InverseTransformPoint(position)
                   - ValueHolder.DistanceToDrawable;
        }
    }
}
