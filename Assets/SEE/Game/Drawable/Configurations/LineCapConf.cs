using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Represents the configuration of a line cap (decoration at the start or end of a line).
    /// A line cap defines both the type of the cap and its visual appearance.
    /// </summary>
    [Serializable]
    public class LineCapConf : ICloneable, ILineVisualConf
    {
        /// <summary>
        /// The type of the line cap.
        /// Determines the geometric shape of the cap.
        /// </summary>
        public LineCapPointsCalculator.LineCap CapKind;

        /// <summary>
        /// Specifies how the colors of the cap are interpreted.
        /// </summary>
        public ColorKind ColorKind { get; set; }

        /// <summary>
        /// The primary color of the cap.
        /// </summary>
        public Color PrimaryColor { get; set; }

        /// <summary>
        /// The secondary color of the cap.
        /// Used depending on the <see cref="ColorKind"/>.
        /// </summary>
        public Color SecondaryColor { get; set; }

        /// <summary>
        /// The thickness of the cap lines.
        /// </summary>
        public float Thickness { get; set; }

        /// <summary>
        /// The line style used to render the cap.
        /// </summary>
        public LineKind LineKind { get; set; }

        /// <summary>
        /// The tiling of a dashed line. Only used for "Dashed" line kind.
        /// </summary>
        public float Tiling { get; set; }

        /// <summary>
        /// Indicates whether the cap should be filled out.
        /// </summary>
        public bool FillOutStatus { get; set; }

        /// <summary>
        /// The fill-out color of the cap.
        /// </summary>
        public Color FillOutColor { get; set; }

        /// <summary>
        /// Creates a deep copy of this <see cref="LineCapConf"/>.
        /// </summary>
        /// <returns>A cloned instance of this <see cref="LineCapConf"/>.</returns>
        public object Clone()
        {
            LineCapConf clone = new LineCapConf
            {
                CapKind = this.CapKind,
                ColorKind = this.ColorKind,
                PrimaryColor = this.PrimaryColor,
                SecondaryColor = this.SecondaryColor,
                Thickness = this.Thickness,
                LineKind = this.LineKind,
                Tiling = this.Tiling,
                FillOutStatus = this.FillOutStatus,
                FillOutColor = this.FillOutColor
            };

            return clone;
        }

        /// <summary>
        /// Creates a default configuration representing the absence of a line cap.
        /// </summary>
        /// <returns>A line-cap configuration whose <see cref="CapKind"/> is <c>None</c>.</returns>
        public static LineCapConf CreateNone()
        {
            return new LineCapConf
            {
                CapKind = LineCapPointsCalculator.LineCap.None,
                ColorKind = ColorKind.Monochrome,
                PrimaryColor = Color.clear,
                SecondaryColor = Color.clear,
                Thickness = 0f,
                LineKind = LineKind.Solid,
                Tiling = 1f,
                FillOutStatus = false,
                FillOutColor = Color.clear
            };
        }

        /// <summary>
        /// Gets both line-cap configurations of the given line.
        /// </summary>
        /// <param name="line">The line game object.</param>
        /// <returns>
        /// An array containing the start cap configuration at index 0 and the end cap
        /// configuration at index 1; or null if the given object
        /// is not a valid line object.
        /// </returns>
        public static LineCapConf[] GetLineCapConfs(GameObject line)
        {
            if (line == null || !line.CompareTag(Tags.Line))
            {
                return null;
            }
            LineCapConf[] confs = new LineCapConf[2];
            confs[0] = GetLineStartCapConf(line);
            confs[1] = GetLineEndCapConf(line);
            return confs;
        }

        /// <summary>
        /// Gets the configuration of the start cap of the given line.
        /// </summary>
        /// <param name="line">The line game object.</param>
        /// <returns>
        /// The start cap configuration, or null if no valid start cap exists.
        /// </returns>
        public static LineCapConf GetLineStartCapConf(GameObject line)
        {
            return GetLineCapConf(line, true);
        }

        /// <summary>
        /// Gets the configuration of the end cap of the given line.
        /// </summary>
        /// <param name="line">The line game object.</param>
        /// <returns>
        /// The end cap configuration, or "null if no valid end cap exists.
        /// </returns>
        public static LineCapConf GetLineEndCapConf(GameObject line)
        {
            return GetLineCapConf(line, false);
        }

        /// <summary>
        /// Creates a <see cref="LineCapConf"/> for either the start or end cap of the given line.
        /// </summary>
        /// <param name="line">The line game object.</param>
        /// <param name="startCap">
        /// If set to true, the start cap is read; otherwise, the end cap is read.
        /// </param>
        /// <returns>
        /// The created <see cref="LineCapConf"/>, or null if the line
        /// or line cap is invalid.
        /// </returns>
        private static LineCapConf GetLineCapConf(GameObject line, bool startCap)
        {
            if (line == null || !line.CompareTag(Tags.Line))
            {
                return null;
            }

            LineCapValueHolder capValueHolder = line.GetComponent<LineCapValueHolder>();
            LineCapPointsCalculator.LineCap capKind = startCap
                ? capValueHolder.StartCap
                : capValueHolder.EndCap;

            if (capKind == LineCapPointsCalculator.LineCap.None)
            {
                return CreateNone();
            }

            string capName = GetLineCapName(line,
                startCap ? ValueHolder.LineStartCapPrefix : ValueHolder.LineEndCapPrefix);

            GameObject lineCapGO = line.FindDescendant(capName);
            if (lineCapGO == null || !lineCapGO.CompareTag(Tags.LineCap))
            {
                return null;
            }

            LineRenderer renderer = lineCapGO.GetComponent<LineRenderer>();

            LineCapConf conf = new()
            {
                CapKind = startCap ? capValueHolder.StartCap : capValueHolder.EndCap
            };

            LineVisualConfFactory.ApplyVisualProperties(lineCapGO, renderer, conf);

            return conf;
        }

        #region Config I/O
        /// <summary>
        /// Label in the configuration file for the cap kind.
        /// </summary>
        private const string capKindLabel = "CapKind";

        /// <summary>
        /// Label in the configuration file for the primary color.
        /// </summary>
        private const string primaryColorLabel = "PrimaryColor";

        /// <summary>
        /// Label in the configuration file for the secondary color.
        /// </summary>
        private const string secondaryColorLabel = "SecondaryColor";

        /// <summary>
        /// Label in the configuration file for the color kind.
        /// </summary>
        private const string colorKindLabel = "ColorKind";

        /// <summary>
        /// Label in the configuration file for the thickness.
        /// </summary>
        private const string thicknessLabel = "Thickness";

        /// <summary>
        /// Label in the configuration file for the line kind.
        /// </summary>
        private const string lineKindLabel = "LineKind";

        /// <summary>
        /// Label in the configuration file for the tiling.
        /// </summary>
        private const string tilingLabel = "Tiling";

        /// <summary>
        /// Label in the configuration file for the fill-out status.
        /// </summary>
        private const string fillOutStatusLabel = "FillOutStatus";

        /// <summary>
        /// Label in the configuration file for the fill-out color.
        /// </summary>
        private const string fillOutColorLabel = "FillOutColor";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        internal void SaveAttributes(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(CapKind.ToString(), capKindLabel);
            writer.Save(ColorKind.ToString(), colorKindLabel);
            writer.Save(PrimaryColor, primaryColorLabel);
            writer.Save(SecondaryColor, secondaryColorLabel);
            writer.Save(Thickness, thicknessLabel);
            writer.Save(LineKind.ToString(), lineKindLabel);
            writer.Save(Tiling, tilingLabel);
            writer.Save(FillOutStatus, fillOutStatusLabel);
            writer.Save(FillOutColor, fillOutColorLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores this <see cref="LineCapConf"/> from the given attribute dictionary.
        /// </summary>
        /// <param name="attributes">
        /// The attribute dictionary created by a <see cref="ConfigWriter"/>.
        /// </param>
        /// <returns>Whether or not the configuration was restored without errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;

            if (attributes.TryGetValue(capKindLabel, out object capKindObject)
                && Enum.TryParse((string)capKindObject, out LineCapPointsCalculator.LineCap capKind))
            {
                CapKind = capKind;
            }
            else
            {
                CapKind = LineCapPointsCalculator.LineCap.None;
                errors = true;
            }

            if (attributes.TryGetValue(colorKindLabel, out object colorKindObject)
                && Enum.TryParse((string)colorKindObject, out ColorKind colorKind))
            {
                ColorKind = colorKind;
            }
            else
            {
                ColorKind = ColorKind.Monochrome;
                errors = true;
            }

            Color primary = Color.clear;
            if (ConfigIO.Restore(attributes, primaryColorLabel, ref primary))
            {
                PrimaryColor = primary;
            }
            else
            {
                PrimaryColor = Color.clear;
                errors = true;
            }

            Color secondary = Color.clear;
            if (ConfigIO.Restore(attributes, secondaryColorLabel, ref secondary))
            {
                SecondaryColor = secondary;
            }
            else
            {
                SecondaryColor = Color.clear;
                errors = true;
            }

            if (attributes.TryGetValue(thicknessLabel, out object thicknessObject))
            {
                Thickness = (float)thicknessObject;
            }
            else
            {
                Thickness = 0f;
                errors = true;
            }

            if (attributes.TryGetValue(lineKindLabel, out object lineKindObject)
                && Enum.TryParse((string)lineKindObject, out LineKind lineKind))
            {
                LineKind = lineKind;
            }
            else
            {
                LineKind = LineKind.Solid;
                errors = true;
            }

            if (attributes.TryGetValue(tilingLabel, out object tilingObject))
            {
                Tiling = (float)tilingObject;
            }
            else
            {
                Tiling = 1f;
                errors = true;
            }

            if (attributes.TryGetValue(fillOutStatusLabel, out object fillOutStatusObject))
            {
                FillOutStatus = (bool)fillOutStatusObject;
            }
            else
            {
                FillOutStatus = false;
                errors = true;
            }

            Color fillOut = Color.clear;
            if (ConfigIO.Restore(attributes, fillOutColorLabel, ref fillOut))
            {
                FillOutColor = fillOut;
            }
            else
            {
                FillOutColor = Color.clear;
                errors = true;
            }

            return !errors;
        }
        #endregion
    }
}
