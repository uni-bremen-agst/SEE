using SEE.Game.Drawable.Configurations;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Holds temporary state used while editing the visual configuration of line caps.
    /// </summary>
    internal sealed class LineCapEditState
    {
        /// <summary>
        /// The last non-none configuration of the start cap.
        /// </summary>
        private LineCapConf rememberedStartCapConf;

        /// <summary>
        /// The last non-none configuration of the end cap.
        /// </summary>
        private LineCapConf rememberedEndCapConf;

        /// <summary>
        /// Whether the fill-out state of the start cap was explicitly changed
        /// during the current edit operation.
        /// </summary>
        private bool startCapFillOutChangedByUser;

        /// <summary>
        /// Whether the fill-out state of the end cap was explicitly changed
        /// during the current edit operation.
        /// </summary>
        private bool endCapFillOutChangedByUser;

        /// <summary>
        /// Initializes the temporary state for editing the given line.
        /// </summary>
        /// <param name="line">The line configuration being edited.</param>
        internal void Initialize(LineConf line)
        {
            rememberedStartCapConf = line.LineCapStart != null
                                     && line.LineCapStart.CapKind != LineCap.None
                ? line.LineCapStart.Clone()
                : null;

            rememberedEndCapConf = line.LineCapEnd != null
                                   && line.LineCapEnd.CapKind != LineCap.None
                ? line.LineCapEnd.Clone()
                : null;

            startCapFillOutChangedByUser = false;
            endCapFillOutChangedByUser = false;
        }

        /// <summary>
        /// Initializes the given line-cap configuration with either the last remembered
        /// cap-specific visual settings or the visual settings of the main line.
        /// </summary>
        /// <param name="line">The line configuration used as fallback.</param>
        /// <param name="cap">The line-cap configuration to initialize.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        internal void InitializeCapConf(LineConf line, LineCapConf cap, bool isStartCap)
        {
            if (line == null || cap == null)
            {
                return;
            }

            LineCapConf rememberedCap = GetRememberedCapConf(isStartCap);

            if (rememberedCap != null)
            {
                cap.Thickness = rememberedCap.Thickness;
                cap.LineKind = rememberedCap.LineKind;
                cap.Tiling = rememberedCap.Tiling;
                cap.ColorKind = rememberedCap.ColorKind;
                cap.PrimaryColor = rememberedCap.PrimaryColor;
                cap.SecondaryColor = rememberedCap.SecondaryColor;
                cap.FillOutStatus = rememberedCap.FillOutStatus;
                cap.FillOutColor = rememberedCap.FillOutColor;
            }
            else
            {
                cap.Thickness = line.Thickness;
                cap.LineKind = line.LineKind;
                cap.Tiling = line.Tiling;
                cap.ColorKind = line.ColorKind;
                cap.PrimaryColor = line.PrimaryColor;
                cap.SecondaryColor = line.SecondaryColor;
                cap.FillOutStatus = line.FillOutStatus;
                cap.FillOutColor = line.FillOutColor;
            }
        }

        /// <summary>
        /// Remembers the current non-none line-cap configuration.
        /// </summary>
        /// <param name="cap">The currently active line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        internal void RememberPreviousCapConf(LineCapConf cap, bool isStartCap)
        {
            if (cap == null || cap.CapKind == LineCap.None)
            {
                return;
            }

            if (isStartCap)
            {
                rememberedStartCapConf = cap.Clone();
            }
            else
            {
                rememberedEndCapConf = cap.Clone();
            }
        }

        /// <summary>
        /// Updates whether the fill-out state of the selected cap differs from
        /// the state remembered at the beginning of the current edit operation.
        /// </summary>
        /// <param name="cap">The currently edited line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        internal void UpdateFillOutChangedByUser(LineCapConf cap, bool isStartCap)
        {
            if (cap == null)
            {
                return;
            }

            LineCapConf rememberedCap = GetRememberedCapConf(isStartCap);

            bool changed = rememberedCap == null
                           || cap.FillOutStatus != rememberedCap.FillOutStatus
                           || cap.FillOutColor != rememberedCap.FillOutColor;

            if (isStartCap)
            {
                startCapFillOutChangedByUser = changed;
            }
            else
            {
                endCapFillOutChangedByUser = changed;
            }
        }

        /// <summary>
        /// Restores the remembered fill-out state of a line cap after changing
        /// its kind unless the user explicitly changed that state.
        /// </summary>
        /// <param name="cap">The currently selected line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        /// <returns>True if the remembered fill-out state was restored.</returns>
        internal bool RestoreRememberedFillOutIfNotChangedByUser(LineCapConf cap, bool isStartCap)
        {
            if (cap == null || HasOwnFillOutDefault(cap.CapKind))
            {
                return false;
            }

            bool changedByUser = isStartCap
                ? startCapFillOutChangedByUser
                : endCapFillOutChangedByUser;

            if (changedByUser)
            {
                return false;
            }

            LineCapConf rememberedCap = GetRememberedCapConf(isStartCap);

            cap.FillOutStatus = rememberedCap != null && rememberedCap.FillOutStatus;
            cap.FillOutColor = rememberedCap != null
                ? rememberedCap.FillOutColor
                : Color.clear;

            return true;
        }

        /// <summary>
        /// Gets the remembered non-none configuration of the selected cap side.
        /// </summary>
        /// <param name="isStartCap">Whether the start cap is requested.</param>
        /// <returns>The remembered configuration or null if none exists.</returns>
        private LineCapConf GetRememberedCapConf(bool isStartCap)
        {
            return isStartCap
                ? rememberedStartCapConf
                : rememberedEndCapConf;
        }
    }
}
