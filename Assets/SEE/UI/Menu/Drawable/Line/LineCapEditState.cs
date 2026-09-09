using SEE.Game.Drawable.Configurations;
using System.Collections.Generic;
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
        /// Stores the last fill-out configuration of start caps that define
        /// their own fill-out default.
        /// </summary>
        private readonly Dictionary<LineCap, LineCapConf> rememberedStartOwnFillOutDefaultConfs = new();

        /// <summary>
        /// Stores the last fill-out configuration of end caps that define
        /// their own fill-out default.
        /// </summary>
        private readonly Dictionary<LineCap, LineCapConf> rememberedEndOwnFillOutDefaultConfs = new();

        /// <summary>
        /// Whether the fill-out state of the start cap was explicitly changed
        /// during the current cap selection.
        /// </summary>
        private bool startCapFillOutChangedByUser;

        /// <summary>
        /// Whether the fill-out state of the end cap was explicitly changed
        /// during the current cap selection.
        /// </summary>
        private bool endCapFillOutChangedByUser;

        /// <summary>
        /// The ID of the line for which the temporary state is currently stored.
        /// </summary>
        private string editedLineId;

        /// <summary>
        /// Initializes the temporary state for editing the given line.
        /// Cap-specific fill-out states are retained when the same line menu
        /// is initialized again during the same drawing operation.
        /// </summary>
        /// <param name="line">The line configuration being edited.</param>
        internal void Initialize(LineConf line)
        {
            if (line == null)
            {
                editedLineId = null;
                rememberedStartCapConf = null;
                rememberedEndCapConf = null;
                rememberedStartOwnFillOutDefaultConfs.Clear();
                rememberedEndOwnFillOutDefaultConfs.Clear();
                startCapFillOutChangedByUser = false;
                endCapFillOutChangedByUser = false;
                return;
            }

            bool isSameLine = !string.IsNullOrEmpty(editedLineId)
                              && editedLineId == line.ID;

            if (!isSameLine)
            {
                rememberedStartOwnFillOutDefaultConfs.Clear();
                rememberedEndOwnFillOutDefaultConfs.Clear();
            }

            editedLineId = line.ID;

            if (!isSameLine)
            {
                rememberedStartCapConf = null;
                rememberedEndCapConf = null;
                rememberedStartOwnFillOutDefaultConfs.Clear();
                rememberedEndOwnFillOutDefaultConfs.Clear();
            }

            if (line.LineCapStart != null
                && line.LineCapStart.CapKind != LineCap.None
                && !HasOwnFillOutDefault(line.LineCapStart.CapKind))
            {
                rememberedStartCapConf = line.LineCapStart.Clone();
            }

            if (line.LineCapEnd != null
                && line.LineCapEnd.CapKind != LineCap.None
                && !HasOwnFillOutDefault(line.LineCapEnd.CapKind))
            {
                rememberedEndCapConf = line.LineCapEnd.Clone();
            }

            editedLineId = line.ID;

            RememberOwnFillOutDefaultCapConf(line.LineCapStart, true);
            RememberOwnFillOutDefaultCapConf(line.LineCapEnd, false);

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
                cap.UseOwnVisuals = rememberedCap.UseOwnVisuals;
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
                cap.UseOwnVisuals = false;
            }
        }

        /// <summary>
        /// Remembers the current non-none line-cap configuration.
        /// Caps with their own fill-out default are stored separately and do not
        /// replace the last remembered normal cap configuration.
        /// </summary>
        /// <param name="cap">The currently active line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        internal void RememberPreviousCapConf(LineCapConf cap, bool isStartCap)
        {
            if (cap == null || cap.CapKind == LineCap.None)
            {
                return;
            }

            if (HasOwnFillOutDefault(cap.CapKind))
            {
                RememberOwnFillOutDefaultCapConf(cap, isStartCap);
            }
            else if (isStartCap)
            {
                rememberedStartCapConf = cap.Clone();
            }
            else
            {
                rememberedEndCapConf = cap.Clone();
            }

            if (isStartCap)
            {
                startCapFillOutChangedByUser = false;
            }
            else
            {
                endCapFillOutChangedByUser = false;
            }
        }

        /// <summary>
        /// Marks the fill-out configuration of the selected cap as explicitly
        /// changed by the user.
        /// </summary>
        /// <param name="cap">The currently edited line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        internal void UpdateFillOutChangedByUser(LineCapConf cap, bool isStartCap)
        {
            if (cap == null)
            {
                return;
            }

            if (isStartCap)
            {
                startCapFillOutChangedByUser = true;
            }
            else
            {
                endCapFillOutChangedByUser = true;
            }
        }

        /// <summary>
        /// Restores the appropriate fill-out state after changing the cap kind.
        /// Normal caps use the most recently remembered normal cap state.
        /// Caps with their own fill-out default restore their cap-specific state
        /// if such a state has previously been remembered.
        /// </summary>
        /// <param name="cap">The currently selected line-cap configuration.</param>
        /// <param name="isStartCap">Whether the start cap is being edited.</param>
        /// <returns>True if a remembered fill-out state was applied.</returns>
        internal bool RestoreRememberedFillOutIfNotChangedByUser(LineCapConf cap, bool isStartCap)
        {
            if (cap == null || cap.CapKind == LineCap.None)
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

            if (HasOwnFillOutDefault(cap.CapKind))
            {
                LineCapConf rememberedOwnDefaultCap =
                    GetRememberedOwnFillOutDefaultCapConf(cap.CapKind, isStartCap);

                if (rememberedOwnDefaultCap == null)
                {
                    return false;
                }

                cap.FillOutStatus = rememberedOwnDefaultCap.FillOutStatus;
                cap.FillOutColor = rememberedOwnDefaultCap.FillOutColor;
                return true;
            }

            LineCapConf rememberedCap = GetRememberedCapConf(isStartCap);

            if (rememberedCap == null)
            {
                cap.FillOutStatus = false;
                return true;
            }

            cap.FillOutStatus = rememberedCap.FillOutStatus;
            cap.FillOutColor = rememberedCap.FillOutColor;

            return true;
        }

        /// <summary>
        /// Remembers the configuration of a cap that defines its own fill-out default.
        /// </summary>
        /// <param name="cap">The cap configuration to remember.</param>
        /// <param name="isStartCap">Whether the configuration belongs to the start cap.</param>
        private void RememberOwnFillOutDefaultCapConf(LineCapConf cap, bool isStartCap)
        {
            if (cap == null || !HasOwnFillOutDefault(cap.CapKind))
            {
                return;
            }

            Dictionary<LineCap, LineCapConf> rememberedConfs = isStartCap
                ? rememberedStartOwnFillOutDefaultConfs
                : rememberedEndOwnFillOutDefaultConfs;

            rememberedConfs[cap.CapKind] = cap.Clone();
        }

        /// <summary>
        /// Gets the remembered configuration for a cap with its own fill-out default.
        /// </summary>
        /// <param name="capKind">The cap kind whose configuration should be returned.</param>
        /// <param name="isStartCap">Whether the start cap is requested.</param>
        /// <returns>The remembered configuration or null if none exists.</returns>
        private LineCapConf GetRememberedOwnFillOutDefaultCapConf(LineCap capKind, bool isStartCap)
        {
            Dictionary<LineCap, LineCapConf> rememberedConfs = isStartCap
                ? rememberedStartOwnFillOutDefaultConfs
                : rememberedEndOwnFillOutDefaultConfs;

            return rememberedConfs.TryGetValue(capKind, out LineCapConf rememberedCap)
                ? rememberedCap
                : null;
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
