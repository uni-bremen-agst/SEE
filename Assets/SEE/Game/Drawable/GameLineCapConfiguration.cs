using SEE.Game.Drawable.Configurations;
using System;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides creation and normalization of line-cap configurations.
    /// </summary>
    public static class GameLineCapConfiguration
    {
        /// <summary>
        /// Creates a normalized line-cap configuration for the given cap kind based on
        /// an existing line-cap configuration.
        /// Existing visual settings are preserved if an active cap exists.
        /// Cap-kind-specific defaults are applied only when the cap kind changes.
        /// </summary>
        /// <param name="line">The parent line configuration.</param>
        /// <param name="existingConf">The existing line-cap configuration to preserve.</param>
        /// <param name="newCapKind">The newly selected line-cap kind.</param>
        /// <returns>The updated line-cap configuration.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        internal static LineCapConf CreateLineCapConf(
            LineConf line,
            LineCapConf existingConf,
            LineCap newCapKind)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            bool hasExistingCap =
                existingConf != null
                && existingConf.CapKind != LineCap.None;

            bool capKindChanged =
                existingConf == null
                || existingConf.CapKind != newCapKind;

            LineCapConf capConf = new();

            if (hasExistingCap)
            {
                capConf.ColorKind = existingConf.ColorKind;
                capConf.PrimaryColor = existingConf.PrimaryColor;
                capConf.SecondaryColor = existingConf.SecondaryColor;
                capConf.Thickness = existingConf.Thickness;
                capConf.LineKind = existingConf.LineKind;
                capConf.Tiling = existingConf.Tiling;
                capConf.FillOutStatus = existingConf.FillOutStatus;
                capConf.FillOutColor = existingConf.FillOutColor;
                capConf.UseOwnVisuals = existingConf.UseOwnVisuals;
            }
            else
            {
                capConf.ColorKind = line.ColorKind;
                capConf.PrimaryColor = line.PrimaryColor;
                capConf.SecondaryColor = line.SecondaryColor;
                capConf.Thickness = line.Thickness;
                capConf.LineKind = line.LineKind;
                capConf.Tiling = line.Tiling;
                capConf.FillOutStatus = false;
                capConf.FillOutColor = Color.clear;
                capConf.UseOwnVisuals = false;
            }

            capConf.CapKind = newCapKind;

            if (capKindChanged || newCapKind == LineCap.None)
            {
                ApplyCapKindDefaults(line, capConf);
            }

            return capConf;
        }

        /// <summary>
        /// Applies cap-kind-specific defaults to the given line-cap configuration.
        /// Existing values are preserved unless the selected cap kind requires a
        /// specific override.
        /// </summary>
        /// <param name="line">The parent line configuration.</param>
        /// <param name="capConf">The line-cap configuration to normalize.</param>
        /// <remarks>
        /// If a line cap defines its own fill-out defaults here, it should also be
        /// added to <see cref="ActionHelpers.LineCapPointsCalculator.HasOwnFillOutDefault"/>
        /// so the edit-mode restoration logic behaves correctly.
        /// </remarks>
        internal static void ApplyCapKindDefaults(LineConf line, LineCapConf capConf)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (capConf == null)
            {
                throw new ArgumentNullException(nameof(capConf));
            }

            switch (capConf.CapKind)
            {
                case LineCap.Composition:
                    capConf.FillOutStatus = true;

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor = capConf.PrimaryColor;
                    }
                    break;

                case LineCap.None:
                    capConf.FillOutStatus = false;
                    capConf.FillOutColor = Color.clear;
                    break;
            }
        }
    }
}
