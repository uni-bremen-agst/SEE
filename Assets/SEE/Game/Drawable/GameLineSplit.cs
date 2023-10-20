using SEE.Net.Actions.Drawable;
using RTG;
using SEE.Game;
using SEE.Game.UI.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game.Drawable.Configurations;

namespace Assets.SEE.Game.Drawable
{
    public static class GameLineSplit
    {
        public static void Split(GameObject drawable, LineConf originLine, List<int> matchedIndexes, List<Vector3> positions, List<LineConf> lines, bool removeMatchedIndex)
        {
            int removeCounter = removeMatchedIndex ? 1 : 0;

            if (matchedIndexes.Count > 1)
            {
                Vector3[] firstPart = positions.GetRange(0, matchedIndexes[0] + 1 - removeCounter).ToArray();
                int lastArrayIndex = matchedIndexes[matchedIndexes.Count - 1];
                int lastListIndex = positions.Count - removeCounter - lastArrayIndex;
                Vector3[] lastPart = positions.GetRange(lastArrayIndex + removeCounter, lastListIndex).ToArray();
                TryReDraw(drawable, originLine, firstPart, lines);
                TryReDraw(drawable, originLine, lastPart, lines);
                positions.RemoveRange(0, (matchedIndexes[0] - 1) + removeCounter);
                int removedCount = (matchedIndexes[0] - 1) + removeCounter;
                bool firstRun = true;

                for (int i = 0; i < matchedIndexes.Count; i++)
                {
                    matchedIndexes[i] -= removedCount;
                }
                for (int i = 1; i < matchedIndexes.Count - 1; i++)
                {
                    int j = i + 1;
                    if (matchedIndexes[i] + 1 != matchedIndexes[j] || firstRun)
                    {
                        firstRun = false;
                        Vector3[] middlePart = positions.GetRange(0, matchedIndexes[i] + 1 - removeCounter).ToArray();
                        positions.RemoveRange(0, matchedIndexes[i]);
                        removedCount = (matchedIndexes[i] - 1) + removeCounter;
                        for (int k = i; k < matchedIndexes.Count; k++)
                        {
                            matchedIndexes[k] -= removedCount;
                        }
                        TryReDraw(drawable, originLine, middlePart, lines);
                    }
                }
            }
            else
            {
                if (matchedIndexes.Count == 1)
                {
                    Vector3[] begin = positions.GetRange(0, matchedIndexes[0] + 1 - removeCounter).ToArray();
                    if (begin.Length == 1)
                    {
                        ShowNotification.Warn("Line Split Problem: ", "You can't split the line on the start point.\nThe line was redrawn nonetheless.");
                    } else if (begin.Length == positions.Count)
                    {
                        ShowNotification.Warn("Line Split Problem: ", "You can't split the line on the end point.\nThe line was redrawn nonetheless.");
                    }
                    int lastIndex = positions.Count - removeCounter - matchedIndexes[0];
                    Vector3[] end = positions.GetRange(matchedIndexes[0] + removeCounter, lastIndex).ToArray();

                    TryReDraw(drawable, originLine, begin, lines);
                    TryReDraw(drawable, originLine, end, lines);
                }
            }
        }

        private static void TryReDraw(GameObject drawable, LineConf originLine, Vector3[] positions, List<LineConf> lines)
        {
            if (positions.Length > 1)
            {
                lines.Add(ReDraw(drawable, originLine, positions));
            }
        }
        private static LineConf ReDraw(GameObject drawable, LineConf originLine, Vector3[] positions)
        {
            LineConf lineToCreate = (LineConf)originLine.Clone();
            lineToCreate.id = "";
            lineToCreate.rendererPositions = positions;

            GameObject newLine = GameDrawer.ReDrawLine(drawable, lineToCreate);
            new DrawOnNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable), LineConf.GetLine(newLine)).Execute();

            return LineConf.GetLine(newLine);
        }
    }
}