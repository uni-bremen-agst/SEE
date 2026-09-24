using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.Editing
{
    /// <summary>
    /// Provides editing operations for drawable lines and line caps.
    /// </summary>
    internal static class GameLineEdit
    {
        /// <summary>
        /// Changes the thickness of a line or line cap.
        /// </summary>
        /// <param name="shape">The shape whose thickness should be changed.</param>
        /// <param name="thickness">The new thickness.</param>
        internal static void ChangeThickness(GameObject shape, float thickness)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineRenderer renderer = shape.GetComponent<LineRenderer>();
                renderer.startWidth = thickness;
                renderer.endWidth = thickness;
                GameLineGeometry.RefreshCollider(shape);
            }
        }

        /// <summary>
        /// Changes the loop state of a line.
        /// </summary>
        /// <param name="line">The line whose loop should be changed.</param>
        /// <param name="loop">The new loop state.</param>
        internal static void ChangeLoop(GameObject line, bool loop)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();
                renderer.loop = loop;
                GameLineGeometry.RefreshCollider(line);
            }
        }

        /// <summary>
        /// Changes the line caps of a line.
        /// </summary>
        /// <param name="line">The line whose line caps should be changed.</param>
        /// <param name="currentConf">The current line configuration.</param>
        /// <param name="start">The starting line cap.</param>
        /// <param name="end">The ending line cap.</param>
        internal static void ChangeLineCaps(
            GameObject line,
            LineConf currentConf,
            LineCap start,
            LineCap end)
        {
            if (line == null || !line.CompareTag(Tags.Line) || currentConf == null)
            {
                return;
            }

            currentConf.LineCapStart =
                GameLineCapConfiguration.CreateLineCapConf(
                    currentConf,
                    currentConf.LineCapStart,
                    start);

            currentConf.LineCapEnd =
                GameLineCapConfiguration.CreateLineCapConf(
                    currentConf,
                    currentConf.LineCapEnd,
                    end);

            GameLineCapApplicator.ApplyLineCaps(
                line,
                currentConf.LineCapStart,
                currentConf.LineCapEnd,
                LineConf.GetFillOutColor(currentConf),
                currentConf.LineCapStart.UseOwnVisuals,
                currentConf.LineCapEnd.UseOwnVisuals);
        }

        /// <summary>
        /// Changes the visual style of one line cap of the given line.
        /// If the thickness changes, the line caps are recreated so that the geometric
        /// size and the connection point of the cap are updated accordingly.
        /// </summary>
        /// <param name="line">The line whose line cap style should be changed.</param>
        /// <param name="isStartCap">
        /// True if the start cap should be changed, false if the end cap should be changed.
        /// </param>
        /// <param name="capConf">The new visual configuration of the line cap.</param>
        internal static void ChangeLineCapStyle(
            GameObject line,
            bool isStartCap,
            LineCapConf capConf)
        {
            if (line == null || !line.CompareTag(Tags.Line) || capConf == null)
            {
                return;
            }

            LineCapValueHolder holder = line.GetComponent<LineCapValueHolder>();
            if (holder == null)
            {
                return;
            }

            LineCapConf currentCapConf = isStartCap
                ? LineCapConf.GetLineStartCapConf(line)
                : LineCapConf.GetLineEndCapConf(line);

            bool thicknessChanged = currentCapConf != null
                && !Mathf.Approximately(currentCapConf.Thickness, capConf.Thickness);

            capConf.UseOwnVisuals = true;

            if (isStartCap)
            {
                holder.StartCapUsesOwnVisuals = true;
            }
            else
            {
                holder.EndCapUsesOwnVisuals = true;
            }

            if (thicknessChanged)
            {
                LineConf currentLine = LineConf.GetLine(line);
                if (currentLine == null)
                {
                    return;
                }

                if (isStartCap)
                {
                    currentLine.LineCapStart = capConf.Clone();
                }
                else
                {
                    currentLine.LineCapEnd = capConf.Clone();
                }

                bool useStartCapVisuals = isStartCap
                    || holder.StartCapUsesOwnVisuals;

                bool useEndCapVisuals = !isStartCap
                    || holder.EndCapUsesOwnVisuals;

                GameLineCapApplicator.ApplyLineCaps(
                    line,
                    currentLine.LineCapStart,
                    currentLine.LineCapEnd,
                    LineConf.GetFillOutColor(currentLine),
                    useStartCapVisuals,
                    useEndCapVisuals);

                return;
            }

            List<GameObject> caps =
                GameLineCapRenderer.GetLineCapObjects(line, isStartCap);

            foreach (GameObject capGO in caps)
            {
                ChangeThickness(capGO, capConf.Thickness);
                GameLineAppearance.ChangeColorKind(
                    capGO, capConf.ColorKind, capConf);
                GameLineAppearance.ChangeLineKind(
                    capGO, capConf.LineKind, capConf.Tiling);
                GameLineAppearance.ChangePrimaryColor(
                    capGO, capConf.PrimaryColor);
                GameLineAppearance.ChangeSecondaryColor(
                    capGO, capConf.SecondaryColor);
                GameLineFillOut.ChangeFillOut(
                    capGO, capConf.FillOutStatus, capConf.FillOutColor);
            }
        }

        /// <summary>
        /// Changes all editable values of a line.
        /// </summary>
        /// <param name="lineObj">The line whose values should be changed.</param>
        /// <param name="line">The new line configuration.</param>
        internal static void ChangeLine(GameObject lineObj, LineConf line)
        {
            if (lineObj.CompareTag(Tags.Line))
            {
                ChangeThickness(lineObj, line.Thickness);
                GameEdit.ChangeLayer(lineObj, line.OrderInLayer);
                GameLineAppearance.ChangeColorKind(
                    lineObj, line.ColorKind, line);
                GameLineAppearance.ChangePrimaryColor(
                    lineObj, line.PrimaryColor);
                GameLineAppearance.ChangeSecondaryColor(
                    lineObj, line.SecondaryColor);
                GameLineFillOut.ChangeFillOut(
                    lineObj, line.FillOutStatus, line.FillOutColor);
                ChangeLoop(lineObj, line.Loop);
                GameLineAppearance.ChangeLineKind(
                    lineObj, line.LineKind, line.Tiling);
                ChangeLineCaps(
                    lineObj,
                    line,
                    line.LineCapStart.CapKind,
                    line.LineCapEnd.CapKind);
                ChangeLineCapStyle(lineObj, true, line.LineCapStart);
                ChangeLineCapStyle(lineObj, false, line.LineCapEnd);
            }
        }
    }
}
