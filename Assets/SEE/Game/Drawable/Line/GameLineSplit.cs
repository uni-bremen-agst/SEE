using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// This class provides functionality for splitting lines.
    /// The splitting point can either be retained (split) or deleted (pointerase).
    /// </summary>
    public static class GameLineSplit
    {
        /// <summary>
        /// Splits a line.
        /// The splitting point can either be retained (split) or deleted (pointerase)
        /// </summary>
        /// <param name="surface">The drawable surface on which the lines should be redrawn.</param>
        /// <param name="originLine">Configuration of the original line.</param>
        /// <param name="matchedIndices">The indices of the points to be split.</param>
        /// <param name="positions">The positions of the line.</param>
        /// <param name="lines">List that holds the newly created line configurations.</param>
        /// <param name="removeMatchedIndex">Specifies whether the split points should be deleted.</param>
        public static void Split(GameObject surface, LineConf originLine, List<int> matchedIndices,
            List<Vector3> positions, List<LineConf> lines, bool removeMatchedIndex)
        {
            if (originLine != null && originLine.Loop)
            {
                ReDrawLoopSegments(
                    surface,
                    originLine,
                    matchedIndices,
                    positions,
                    lines,
                    removeMatchedIndex
                        ? LoopEditOperation.ErasePoint
                        : LoopEditOperation.SplitPoint);

                return;
            }

            int removeCounter = removeMatchedIndex ? 1 : 0;

            /// Block for the case where multiple indices were found that need to be split.
            if (matchedIndices.Count > 1)
            {
                /// Creates the individual segments that remain when the line is split at the indices.
                /// Depending on whether the split point should be deleted, it is either removed immediately or retained.
                List<List<Vector3>> parts = new();
                for (int i = 0; i < matchedIndices.Count; i++)
                {
                    int newI = removeMatchedIndex ? matchedIndices[i] : matchedIndices[i] + 1;
                    newI = newI < 1 ? 1 : newI;
                    if (i > 0)
                    {
                        int startIndex = removeMatchedIndex ? matchedIndices[i - 1] + 1 : matchedIndices[i - 1];
                        int include = removeMatchedIndex ? 0 : 1;
                        newI = matchedIndices[i] + include - startIndex;
                        parts.Add(positions.GetRange(startIndex, newI));
                    }
                    else
                    {
                        parts.Add(positions.GetRange(0, newI));
                    }
                }

                /// Add the last part of the splitting.
                int endIndex = removeMatchedIndex ? matchedIndices.Last() + 1 : matchedIndices.Last();
                int endI = positions.Count - endIndex;
                parts.Add(positions.GetRange(endIndex, endI));

                /// Trys to re-draw every segment.
                foreach (List<Vector3> list in parts)
                {
                    TryReDraw(surface, originLine, list.ToArray(), lines);
                }
                /// Block for the case where an attempt was made to split at the start or end point.
                if (lines.Count == 1 && !removeMatchedIndex)
                {
                    ShowNotification.Warn("Can't split", "The line can't split on start/end point." +
                        "\nThe line was redrawn nonetheless.");
                }
            }
            else
            {
                /// Block for the case where only one index was found to split the line.
                if (matchedIndices.Count == 1)
                {
                    Vector3[] begin = positions.GetRange(0, matchedIndices[0] + 1 - removeCounter).ToArray();

                    /// Block for the case where an attempt was made to split at the start point.
                    if (begin.Length == 1 && !removeMatchedIndex)
                    {
                        ShowNotification.Warn("Line Split Problem: ", "You can't split the line on the start point." +
                            "\nThe line was redrawn nonetheless.");
                    }
                    else if (begin.Length == positions.Count && !removeMatchedIndex)
                    {/// Block for the case where an attempt was made to split at the end point.
                        ShowNotification.Warn("Line Split Problem: ", "You can't split the line on the end point." +
                            "\nThe line was redrawn nonetheless.");
                    }
                    int lastIndex = positions.Count - removeCounter - matchedIndices[0];
                    Vector3[] end = positions.GetRange(matchedIndices[0] + removeCounter, lastIndex).ToArray();

                    /// Trys to re-draw the first and second segment.
                    TryReDraw(surface, originLine, begin, lines);
                    TryReDraw(surface, originLine, end, lines);
                }
            }
        }

        /// <summary>
        /// Erases a connection line between two points.
        /// To do this, select the starting point of this line.
        /// Is intended for shapes.
        /// </summary>
        /// <param name="surface">The drawable surface on which the lines should be redrawn.</param>
        /// <param name="originLine">Configuration of the original line.</param>
        /// <param name="matchedIndices">The indices of the points which connection lines should
        /// be erased.</param>
        /// <param name="positions">The positions of the line.</param>
        /// <param name="lines">List that holds the new created line configurations.</param>
        public static void EraseLinePointConnection(GameObject surface, LineConf originLine, List<int> matchedIndices,
            List<Vector3> positions, List<LineConf> lines)
        {
            if (originLine != null && originLine.Loop)
            {
                ReDrawLoopSegments(
                    surface,
                    originLine,
                    matchedIndices,
                    positions,
                    lines,
                    LoopEditOperation.EraseConnection);

                return;
            }

            /// Block for the case where multiple indices were found that need to be split.
            if (matchedIndices.Count > 1)
            {
                /// Creates the individual segments that remain when the line is split
                /// at the indices. Depending on whether the split point should be deleted,
                /// it is either removed immediately or retained.
                List<List<Vector3>> parts = new();
                for (int i = 0; i < matchedIndices.Count; i++)
                {
                    int newI = matchedIndices[i] + 1;
                    newI = newI < 1 ? 1 : newI;
                    if (i > 0)
                    {
                        int startIndex = matchedIndices[i - 1] + 1;
                        newI = matchedIndices[i] + 1 - startIndex;
                        parts.Add(positions.GetRange(startIndex, newI));
                    }
                    else
                    {
                        parts.Add(positions.GetRange(0, newI));
                    }
                }

                /// Add the last part of the splitting.
                int endIndex = matchedIndices.Last() + 1;
                int endI = positions.Count - endIndex;
                parts.Add(positions.GetRange(endIndex, endI));

                /// Tries to re-draw every segment.
                foreach (List<Vector3> list in parts)
                {
                    TryReDraw(surface, originLine, list.ToArray(), lines);
                }

                /// Block for the case where an attempt was made to split at the start or end point.
                if (lines.Count == 1
                    && lines[0].RendererPositions.Length == positions.Count)
                {
                    ShowNotification.Warn("Line Connector Erase Problem:",
                        "You can't erase a line connector on the endpoint. " +
                        "Because the endpoint has none." +
                        "\nThe line was redrawn nonetheless.");
                }
            }
            else
            {
                /// Block for the case where only one index was found to split the line.
                if (matchedIndices.Count == 1)
                {
                    Vector3[] begin = positions.GetRange(0, matchedIndices[0] + 1).ToArray();

                    if (begin.Length == positions.Count)
                    {
                        /// Block for the case where an attempt was made to split at the end point.
                        ShowNotification.Warn("Line Connector Erase Problem: ",
                            "You can't erase a line connector on the endpoint. " +
                            "Because the endpoint has none." +
                            "\nThe line was redrawn nonetheless.");
                    }
                    int lastIndex = positions.Count - 1 - matchedIndices[0];
                    Vector3[] end = positions.GetRange(matchedIndices[0] + 1, lastIndex).ToArray();

                    /// Trys to re-draw the first and second segment.
                    TryReDraw(surface, originLine, begin, lines);
                    TryReDraw(surface, originLine, end, lines);
                }
            }
        }


        /// <summary>
        /// Specifies how a closed line is split into open segments.
        /// </summary>
        internal enum LoopEditOperation
        {
            /// <summary>
            /// Splits the line at the selected points while retaining them.
            /// </summary>
            SplitPoint,

            /// <summary>
            /// Removes the selected points and their adjacent connections.
            /// </summary>
            ErasePoint,

            /// <summary>
            /// Removes the connections starting at the selected points.
            /// </summary>
            EraseConnection
        }

        /// <summary>
        /// Describes an open segment resulting from an operation on a loop,
        /// including whether it retains either original line cap.
        /// </summary>
        internal readonly struct LoopSegment
        {
            /// <summary>
            /// The positions of the resulting open segment.
            /// </summary>
            public Vector3[] Positions { get; }

            /// <summary>
            /// Whether the segment retains the original start cap.
            /// </summary>
            public bool RetainsStartCap { get; }

            /// <summary>
            /// Whether the segment retains the original end cap.
            /// </summary>
            public bool RetainsEndCap { get; }

            /// <summary>
            /// Creates a segment with its cap ownership information.
            /// </summary>
            /// <param name="positions">The positions of the segment.</param>
            /// <param name="retainsStartCap">
            /// Whether the original start cap is retained.
            /// </param>
            /// <param name="retainsEndCap">
            /// Whether the original end cap is retained.
            /// </param>
            public LoopSegment(
                Vector3[] positions,
                bool retainsStartCap,
                bool retainsEndCap)
            {
                Positions = positions;
                RetainsStartCap = retainsStartCap;
                RetainsEndCap = retainsEndCap;
            }
        }

        /// <summary>
        /// Calculates the open segments resulting from editing a closed line.
        /// The closing connection is represented explicitly by appending
        /// the first logical point to the traversal.
        /// </summary>
        /// <param name="logicalPositions">
        /// The original line positions without the synthetic renderer point.
        /// </param>
        /// <param name="selectedIndices">
        /// The selected logical point indices.
        /// </param>
        /// <param name="operation">The requested editing operation.</param>
        /// <returns>
        /// The resulting open segments and their original cap ownership.
        /// Segments containing fewer than two points are omitted.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if an input collection is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if an index or operation is invalid.
        /// </exception>
        internal static List<LoopSegment> GetLoopSegments(
            IReadOnlyList<Vector3> logicalPositions,
            IEnumerable<int> selectedIndices,
            LoopEditOperation operation)
        {
            if (logicalPositions == null)
            {
                throw new ArgumentNullException(nameof(logicalPositions));
            }

            if (selectedIndices == null)
            {
                throw new ArgumentNullException(nameof(selectedIndices));
            }


            List<LoopSegment> segments = new();
            int originalPointCount = logicalPositions.Count;

            // Some closed shapes already contain their first point
            // again as their final logical position. Such a point
            // must not be appended a second time.
            bool hasExplicitClosingPoint =
                originalPointCount > 2
                && logicalPositions[0].Equals(
                    logicalPositions[originalPointCount - 1]);

            int pointCount = hasExplicitClosingPoint
                ? originalPointCount - 1
                : originalPointCount;

            if (pointCount < 2)
            {
                return segments;
            }

            HashSet<int> selected = new();

            foreach (int index in selectedIndices)
            {
                if (index < 0 || index >= originalPointCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(selectedIndices),
                        index,
                        "The selected index is outside the logical positions.");
                }

                int normalizedIndex = index;

                if (hasExplicitClosingPoint && index == pointCount)
                {
                    // For point operations, the repeated closing
                    // position represents the first logical point.
                    // For connection erasure, it identifies the
                    // incoming closing connection instead.
                    normalizedIndex =
                        operation == LoopEditOperation.EraseConnection
                            ? pointCount - 1
                            : 0;
                }

                selected.Add(normalizedIndex);
            }

            if (selected.Count == 0)
            {
                return segments;
            }

            List<Vector3> traversal = new();

            for (int index = 0; index < pointCount; index++)
            {
                traversal.Add(logicalPositions[index]);
            }

            // Append exactly one explicit closing position.
            traversal.Add(logicalPositions[0]);

            switch (operation)
            {
                case LoopEditOperation.SplitPoint:
                    {
                        int segmentStart = 0;

                        // Point zero already forms the beginning and end of
                        // the explicit traversal. Other selected points split
                        // that traversal into separate open segments.
                        for (int index = 1; index < pointCount; index++)
                        {
                            if (selected.Contains(index))
                            {
                                AddLoopSegment(
                                    segments,
                                    traversal,
                                    segmentStart,
                                    index);

                                segmentStart = index;
                            }
                        }

                        AddLoopSegment(
                            segments,
                            traversal,
                            segmentStart,
                            pointCount);

                        break;
                    }

                case LoopEditOperation.ErasePoint:
                    {
                        int segmentStart = -1;

                        for (int index = 0; index <= pointCount; index++)
                        {
                            // The additional closing position represents
                            // logical point zero and must be removed together
                            // with that point.
                            bool removePoint =
                                selected.Contains(index % pointCount);

                            if (removePoint)
                            {
                                if (segmentStart >= 0)
                                {
                                    AddLoopSegment(
                                        segments,
                                        traversal,
                                        segmentStart,
                                        index - 1);

                                    segmentStart = -1;
                                }
                            }
                            else if (segmentStart < 0)
                            {
                                segmentStart = index;
                            }
                        }

                        if (segmentStart >= 0)
                        {
                            AddLoopSegment(
                                segments,
                                traversal,
                                segmentStart,
                                pointCount);
                        }

                        break;
                    }

                case LoopEditOperation.EraseConnection:
                    {
                        int segmentStart = 0;

                        for (int index = 0; index < pointCount; index++)
                        {
                            // Selecting point i removes the outgoing
                            // connection from i to the following point.
                            if (selected.Contains(index))
                            {
                                AddLoopSegment(
                                    segments,
                                    traversal,
                                    segmentStart,
                                    index);

                                segmentStart = index + 1;
                            }
                        }

                        AddLoopSegment(
                            segments,
                            traversal,
                            segmentStart,
                            pointCount);

                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(operation),
                        operation,
                        "Unsupported loop editing operation.");
            }

            return segments;
        }

        /// <summary>
        /// Adds a non-degenerate open segment from the indexed traversal.
        /// Cap ownership follows the original traversal boundaries rather
        /// than comparing potentially overlapping point coordinates.
        /// </summary>
        /// <param name="segments">The result collection.</param>
        /// <param name="traversal">The explicitly closed traversal.</param>
        /// <param name="firstIndex">The inclusive first position.</param>
        /// <param name="lastIndex">The inclusive last position.</param>
        private static void AddLoopSegment(
            List<LoopSegment> segments,
            IReadOnlyList<Vector3> traversal,
            int firstIndex,
            int lastIndex)
        {
            if (lastIndex - firstIndex < 1)
            {
                return;
            }

            Vector3[] positions =
                new Vector3[lastIndex - firstIndex + 1];

            for (int index = firstIndex; index <= lastIndex; index++)
            {
                positions[index - firstIndex] = traversal[index];
            }

            segments.Add(
                new LoopSegment(
                    positions,
                    firstIndex == 0,
                    lastIndex == traversal.Count - 1));
        }

        /// <summary>
        /// Reconstructs the open segments of an edited loop with their
        /// corresponding original start and end line caps.
        /// </summary>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="originLine">The original closed line.</param>
        /// <param name="matchedIndices">The selected logical indices.</param>
        /// <param name="positions">The original logical positions.</param>
        /// <param name="lines">The resulting line configurations.</param>
        /// <param name="operation">The loop editing operation.</param>
        private static void ReDrawLoopSegments(
            GameObject surface,
            LineConf originLine,
            List<int> matchedIndices,
            List<Vector3> positions,
            List<LineConf> lines,
            LoopEditOperation operation)
        {
            List<LoopSegment> segments = GetLoopSegments(
                positions,
                matchedIndices,
                operation);

            foreach (LoopSegment segment in segments)
            {
                TryReDraw(
                    surface,
                    originLine,
                    segment.Positions,
                    lines,
                    segment.RetainsStartCap,
                    segment.RetainsEndCap);
            }
        }

        /// <summary>
        /// Recreates a line segment if it contains at least two positions.
        /// For segments originating from loops, the original cap ownership
        /// is passed explicitly.
        /// </summary>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="originLine">The original line configuration.</param>
        /// <param name="positions">The positions of the resulting segment.</param>
        /// <param name="lines">The collection of resulting lines.</param>
        /// <param name="retainLoopStartCap">
        /// Whether the original start cap is retained for a loop segment.
        /// </param>
        /// <param name="retainLoopEndCap">
        /// Whether the original end cap is retained for a loop segment.
        /// </param>
        private static void TryReDraw(
            GameObject surface,
            LineConf originLine,
            Vector3[] positions,
            List<LineConf> lines,
            bool retainLoopStartCap = false,
            bool retainLoopEndCap = false)
        {
            if (positions.Length > 1)
            {
                lines.Add(
                    ReDraw(
                        surface,
                        originLine,
                        positions,
                        retainLoopStartCap,
                        retainLoopEndCap));
            }
        }

        /// <summary>
        /// Redraws the line with the new sub positions.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be redrawn.</param>
        /// <param name="originLine">Configuration of the old line.</param>
        /// <param name="positions">The new sub positions of the new line.</param>
        /// <param name="retainLoopStartCap">
        /// Whether the segment retains the original start cap of a loop.
        /// </param>
        /// <param name="retainLoopEndCap">
        /// Whether the segment retains the original end cap of a loop.
        /// </param>
        /// <returns>Configuration of the new sub line.</returns>
        private static LineConf ReDraw(
            GameObject surface,
            LineConf originLine,
            Vector3[] positions,
            bool retainLoopStartCap = false,
            bool retainLoopEndCap = false)
        {
            LineConf lineToCreate = originLine.Clone();
            lineToCreate.ID = "";
            lineToCreate.RendererPositions = positions;
            lineToCreate.OriginalStartAnchor = positions[0];
            lineToCreate.OriginalEndAnchor = positions[^1];

            // Every resulting segment is an open line, even if the
            // original line was a loop.
            lineToCreate.Loop = false;

            AdjustLineCapsForSegment(
                originLine,
                lineToCreate,
                positions);

            if (originLine.Loop)
            {
                // On a loop, both original caps can be anchored at
                // the first logical point. Coordinate comparisons are
                // therefore insufficient to determine cap ownership.
                lineToCreate.LineCapStart =
                    retainLoopStartCap && originLine.LineCapStart != null
                        ? originLine.LineCapStart.Clone()
                        : LineCapConf.CreateNone();

                lineToCreate.LineCapEnd =
                    retainLoopEndCap && originLine.LineCapEnd != null
                        ? originLine.LineCapEnd.Clone()
                        : LineCapConf.CreateNone();
            }

            LineCapConf startCap = lineToCreate.LineCapStart != null
                ? lineToCreate.LineCapStart.Clone()
                : LineCapConf.CreateNone();

            LineCapConf endCap = lineToCreate.LineCapEnd != null
                ? lineToCreate.LineCapEnd.Clone()
                : LineCapConf.CreateNone();

            lineToCreate.LineCapStart = LineCapConf.CreateNone();
            lineToCreate.LineCapEnd = LineCapConf.CreateNone();

            GameObject newLine = GameLineDrawer.ReDrawLine(surface, lineToCreate);

            GameLineGeometry.ChangePivot(newLine);

            LineConf pivotAdjustedLine = LineConf.GetLine(newLine);
            GameLineGeometry.UpdateOriginalAnchors(newLine, pivotAdjustedLine.RendererPositions);
            pivotAdjustedLine = LineConf.GetLine(newLine);

            pivotAdjustedLine.LineCapStart = startCap;
            pivotAdjustedLine.LineCapEnd = endCap;

            GameLineCapApplicator.ApplyLineCaps(newLine, pivotAdjustedLine.LineCapStart,
                pivotAdjustedLine.LineCapEnd, LineConf.GetFillOutColor(pivotAdjustedLine), true);

            LineConf result = LineConf.GetLine(newLine);

            new DrawNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), result).Execute();

            return result;
        }

        /// <summary>
        /// Adjusts the line caps of the given segment so that only caps at the original
        /// start or end of the line are preserved.
        /// </summary>
        /// <param name="originLine">The original line.</param>
        /// <param name="segmentLine">The segment line to update.</param>
        /// <param name="positions">The positions of the segment line.</param>
        private static void AdjustLineCapsForSegment(LineConf originLine, LineConf segmentLine, Vector3[] positions)
        {
            if (originLine == null || segmentLine == null || positions == null || positions.Length < 2)
            {
                return;
            }

            bool keepsOriginalStart = positions[0] == originLine.OriginalStartAnchor;
            bool keepsOriginalEnd = positions[^1] == originLine.OriginalEndAnchor;

            segmentLine.LineCapStart = keepsOriginalStart
                ? originLine.LineCapStart?.Clone()
                : LineCapConf.CreateNone();

            segmentLine.LineCapEnd = keepsOriginalEnd
                ? originLine.LineCapEnd?.Clone()
                : LineCapConf.CreateNone();
        }
    }
}
