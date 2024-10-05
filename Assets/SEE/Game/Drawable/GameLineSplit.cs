using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
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
        /// Checks if the line can be redrawn.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be drawn.</param>
        /// <param name="originLine">The configuration of the original line.</param>
        /// <param name="positions">The positions for the new line.</param>
        /// <param name="lines">List that holds the new line configurations.</param>
        private static void TryReDraw(GameObject surface, LineConf originLine,
            Vector3[] positions, List<LineConf> lines)
        {
            if (positions.Length > 1)
            {
                lines.Add(ReDraw(surface, originLine, positions));
            }
        }

        /// <summary>
        /// Redraws the line with the new sub positions.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be redrawn.</param>
        /// <param name="originLine">Configuration of the old line.</param>
        /// <param name="positions">The new sub positions of the new line.</param>
        /// <returns>Configuration of the new sub line.</returns>
        private static LineConf ReDraw(GameObject surface, LineConf originLine, Vector3[] positions)
        {
            LineConf lineToCreate = (LineConf)originLine.Clone();
            lineToCreate.Id = "";
            lineToCreate.RendererPositions = positions;

            GameObject newLine = GameDrawer.ReDrawLine(surface, lineToCreate);
            GameDrawer.ChangePivot(newLine);
            new DrawNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                LineConf.GetLine(newLine)).Execute();

            return LineConf.GetLine(newLine);
        }
    }
}