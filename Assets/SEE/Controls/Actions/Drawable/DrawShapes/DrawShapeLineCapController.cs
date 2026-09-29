using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable.Shapes;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Controls.Actions.Drawable.DrawShapes
{
    /// <summary>
    /// Manages line-cap configuration and preview state while drawing shapes.
    /// </summary>
    internal sealed class DrawShapeLineCapController
    {
        /// <summary>
        /// The previously applied start line cap of the preview.
        /// </summary>
        private LineCap lastPreviewStartCap = LineCap.None;

        /// <summary>
        /// The previously applied end line cap of the preview.
        /// </summary>
        private LineCap lastPreviewEndCap = LineCap.None;

        /// <summary>
        /// The previously applied line kind of the preview.
        /// </summary>
        private LineKind lastPreviewLineKind;

        /// <summary>
        /// Refreshes the preview line caps if their configuration or the effective
        /// line kind changed.
        /// </summary>
        /// <param name="shape">The current preview shape.</param>
        /// <param name="surface">The drawable surface containing the shape.</param>
        /// <param name="previewPositions">The positions currently shown by the preview.</param>
        /// <param name="fillOutColor">The current fill-out color.</param>
        /// <param name="previewActive">Whether a shape preview is currently active.</param>
        public void RefreshPreviewIfMenuChanged(GameObject shape, GameObject surface,
            Vector3[] previewPositions, Color? fillOutColor, bool previewActive)
        {
            if (shape == null || previewPositions == null || !previewActive)
            {
                return;
            }

            LineCap startCap = ShapeMenu.GetLineStartCap();
            LineCap endCap = ShapeMenu.GetLineEndCap();

            bool hasReference =
                startCap == LineCap.Reference
                || endCap == LineCap.Reference;

            bool hadReference =
                lastPreviewStartCap == LineCap.Reference
                || lastPreviewEndCap == LineCap.Reference;

            LineConf currentShape = LineConf.GetLine(shape);
            if (currentShape == null)
            {
                return;
            }

            LineKind previewLineKind = ResolvePreviewLineKind(
                hasReference,
                hadReference,
                currentShape.LineKind,
                ValueHolder.CurrentLineKind);

            if (startCap == lastPreviewStartCap
                && endCap == lastPreviewEndCap
                && previewLineKind == lastPreviewLineKind)
            {
                return;
            }

            ApplyPreview(shape, previewPositions, fillOutColor);

            new DrawNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                LineConf.GetLine(shape)).Execute();
        }

        /// <summary>
        /// Applies the currently selected line caps to the finished line.
        /// </summary>
        /// <param name="shape">The finished line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="shapeConf">The configuration of the finished line.</param>
        /// <returns>The updated line configuration.</returns>
        public LineConf ApplyFinal(GameObject shape, GameObject surface, LineConf shapeConf)
        {
            if (shape == null || shapeConf == null)
            {
                return shapeConf;
            }

            (LineCapConf startConf, LineCapConf endConf, bool hasReference)
                = CreateSelectedLineCapConfs(shape, surface, shapeConf, true);

            GameLineCapApplicator.ApplyLineCaps(
                shape,
                startConf,
                endConf,
                LineConf.GetFillOutColor(shapeConf),
                hasReference || startConf.UseOwnVisuals,
                hasReference || endConf.UseOwnVisuals);

            return LineConf.GetLine(shape);
        }

        /// <summary>
        /// Applies the currently selected line caps to the preview line.
        /// </summary>
        /// <param name="shape">The preview line.</param>
        /// <param name="previewPositions">The current positions of the preview line.</param>
        /// <param name="fillOutColor">The current fill-out color.</param>
        public void ApplyPreview(GameObject shape, Vector3[] previewPositions, Color? fillOutColor)
        {
            if (shape == null || previewPositions == null || previewPositions.Length < 2)
            {
                return;
            }

            GameLineGeometry.UpdateOriginalAnchors(shape, previewPositions);

            LineConf currentShape = LineConf.GetLine(shape);
            if (currentShape == null)
            {
                return;
            }

            (LineCapConf startConf, LineCapConf endConf, bool hasReference)
                = CreateSelectedLineCapConfs(shape, null, currentShape, false);

            GameLineCapApplicator.ApplyLineCaps(
                shape,
                startConf,
                endConf,
                fillOutColor,
                hasReference || startConf.UseOwnVisuals,
                hasReference || endConf.UseOwnVisuals);

            lastPreviewStartCap = ShapeMenu.GetLineStartCap();
            lastPreviewEndCap = ShapeMenu.GetLineEndCap();

            LineConf refreshedShape = LineConf.GetLine(shape);

            lastPreviewLineKind = hasReference
                ? LineKind.Dashed25
                : refreshedShape?.LineKind ?? currentShape.LineKind;
        }

        /// <summary>
        /// Resets the cached preview state.
        /// </summary>
        public void Reset()
        {
            lastPreviewStartCap = LineCap.None;
            lastPreviewEndCap = LineCap.None;
            lastPreviewLineKind = ValueHolder.CurrentLineKind;
        }

        /// <summary>
        /// Creates the currently selected start and end line-cap configurations.
        /// </summary>
        /// <param name="shape">The affected line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="shapeConf">The current line configuration.</param>
        /// <param name="sendLineKindChange">
        /// Whether a line-kind change caused by a reference cap should be synchronized separately.
        /// </param>
        /// <returns>
        /// The created start and end line-cap configurations and whether a reference cap is used.
        /// </returns>
        private (LineCapConf StartConf, LineCapConf EndConf, bool HasReference)
            CreateSelectedLineCapConfs(GameObject shape, GameObject surface, LineConf shapeConf,
                bool sendLineKindChange)
        {
            LineCapConf startConf = ShapeMenu.GetLineStartCapConf();
            LineCapConf endConf = ShapeMenu.GetLineEndCapConf();

            LineCap startCap = startConf.CapKind;
            LineCap endCap = endConf.CapKind;

            bool hasReference =
                startCap == LineCap.Reference
                || endCap == LineCap.Reference;

            bool hadReference =
                lastPreviewStartCap == LineCap.Reference
                || lastPreviewEndCap == LineCap.Reference;

            LineKind lineKind = ResolvePreviewLineKind(
                hasReference,
                hadReference,
                shapeConf.LineKind,
                ValueHolder.CurrentLineKind);

            GameLineAppearance.ChangeLineKind(shape, lineKind, shapeConf.Tiling);
            shapeConf.LineKind = lineKind;

            if (hasReference && sendLineKindChange)
            {
                new ChangeLineKindNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    shape.name,
                    LineKind.Dashed25,
                    shapeConf.Tiling).Execute();
            }

            LineCap actualStartCap =
                startCap == LineCap.Reference
                    ? LineCap.Arrow
                    : startCap;

            LineCap actualEndCap =
                endCap == LineCap.Reference
                    ? LineCap.Arrow
                    : endCap;

            startConf = CreateSelectedLineCapConf(
                shapeConf,
                startConf,
                actualStartCap,
                startCap);

            endConf = CreateSelectedLineCapConf(
                shapeConf,
                endConf,
                actualEndCap,
                endCap);

            return (startConf, endConf, hasReference);
        }

        /// <summary>
        /// Creates the selected line-cap configuration for the preview or final line.
        /// </summary>
        /// <param name="shapeConf">The parent line configuration.</param>
        /// <param name="existingCapConf">The existing cap configuration.</param>
        /// <param name="actualCap">The actual cap kind to draw.</param>
        /// <param name="selectedCap">The cap kind selected in the shape menu.</param>
        /// <returns>The normalized line-cap configuration.</returns>
        private static LineCapConf CreateSelectedLineCapConf(LineConf shapeConf,
            LineCapConf existingCapConf, LineCap actualCap, LineCap selectedCap)
        {
            LineCapConf reusableCapConf = existingCapConf != null
                && existingCapConf.CapKind == actualCap
                && existingCapConf.UseOwnVisuals
                    ? existingCapConf
                    : null;

            LineCapConf capConf =
                GameLineCapConfiguration.CreateLineCapConf(shapeConf, reusableCapConf, actualCap);

            ConfigureReferenceLineCap(selectedCap, capConf);

            return capConf;
        }

        /// <summary>
        /// Configures a line cap as the visible cap of a reference line.
        /// </summary>
        /// <param name="selectedCap">The cap selected in the shape menu.</param>
        /// <param name="capConf">The cap configuration to adjust.</param>
        private static void ConfigureReferenceLineCap(LineCap selectedCap, LineCapConf capConf)
        {
            if (selectedCap != LineCap.Reference)
            {
                return;
            }

            capConf.LineKind = LineKind.Solid;
            capConf.Tiling = ValueHolder.StandardLineTiling;
            capConf.FillOutStatus = false;
            capConf.FillOutColor = Color.clear;
        }

        /// <summary>
        /// Determines the effective line kind of a preview while line caps are applied.
        /// </summary>
        /// <param name="hasReference">Whether the current selection contains a reference cap.</param>
        /// <param name="hadReference">Whether the previous selection contained a reference cap.</param>
        /// <param name="currentLineKind">The current line kind of the preview.</param>
        /// <param name="drawingLineKind">The regular line kind configured for drawing.</param>
        /// <returns>The line kind to apply.</returns>
        internal static LineKind ResolvePreviewLineKind(bool hasReference, bool hadReference,
            LineKind currentLineKind, LineKind drawingLineKind)
        {
            if (hasReference)
            {
                return LineKind.Dashed25;
            }

            if (hadReference)
            {
                return drawingLineKind;
            }

            return currentLineKind;
        }
    }
}
