using SEE.Game.Drawable.ValueHolders;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable line.
    /// </summary>
    /// <remarks>
    /// TODO (#964): Replace <see cref="ICloneable"/> with a strongly typed cloning
    /// mechanism as part of a dedicated refactoring. This class currently follows the
    /// cloning approach used throughout the drawable configuration hierarchy.
    /// </remarks>
    [Serializable]
    public class LineConf : DrawableType, ICloneable, ILineVisualConf
    {
        /// <summary>
        /// The renderer positions of the drawn points.
        /// </summary>
        public Vector3[] RendererPositions;

        /// <summary>
        /// The original start anchor of the line before line caps are applied.
        /// </summary>
        public Vector3 OriginalStartAnchor;

        /// <summary>
        /// The original end anchor of the line before line caps are applied.
        /// </summary>
        public Vector3 OriginalEndAnchor;

        /// <summary>
        /// If true, the line should loop.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// Serialized backing field for <see cref="PrimaryColor"/>.
        /// </summary>
        [SerializeField]
        private Color primaryColor;

        /// <summary>
        /// The primary color of the line.
        /// </summary>
        public Color PrimaryColor
        {
            get => primaryColor;
            set => primaryColor = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="SecondaryColor"/>.
        /// </summary>
        [SerializeField]
        private Color secondaryColor;

        /// <summary>
        /// The secondary color of the line.
        /// </summary>
        public Color SecondaryColor
        {
            get => secondaryColor;
            set => secondaryColor = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="ColorKind"/>.
        /// </summary>
        [SerializeField]
        private GameDrawer.ColorKind colorKind;

        /// <summary>
        /// The color kind of the line (Monochrome/Gradient/Two-color dashed).
        /// </summary>
        public GameDrawer.ColorKind ColorKind
        {
            get => colorKind;
            set => colorKind = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="Thickness"/>.
        /// </summary>
        [SerializeField]
        private float thickness;

        /// <summary>
        /// The thickness of the line.
        /// </summary>
        public float Thickness
        {
            get => thickness;
            set => thickness = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="LineKind"/>.
        /// </summary>
        [SerializeField]
        private GameDrawer.LineKind lineKind;

        /// <summary>
        /// The line kind of the line (Solid/Dashed/Dashed25/Dashed50/Dashed75/Dashed100)
        /// </summary>
        public GameDrawer.LineKind LineKind
        {
            get => lineKind;
            set => lineKind = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="Tiling"/>.
        /// </summary>
        [SerializeField]
        private float tiling;

        /// <summary>
        /// The tiling of a dashed line. Only used for "Dashed" line kind.
        /// </summary>
        public float Tiling
        {
            get => tiling;
            set => tiling = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="FillOutStatus"/>.
        /// </summary>
        [SerializeField]
        private bool fillOutStatus;

        /// <summary>
        /// Whether the fill out is active or not.
        /// </summary>
        public bool FillOutStatus
        {
            get => fillOutStatus;
            set => fillOutStatus = value;
        }

        /// <summary>
        /// Serialized backing field for <see cref="FillOutColor"/>.
        /// </summary>
        [SerializeField]
        private Color fillOutColor;

        /// <summary>
        /// The fill out color; null if the line has no fill out.
        /// </summary>
        public Color FillOutColor
        {
            get => fillOutColor;
            set => fillOutColor = value;
        }

        /// <summary>
        /// The configuration of the start cap of the line.
        /// </summary>
        public LineCapConf LineCapStart;

        /// <summary>
        /// The configuration of the end cap of the line.
        /// </summary>
        public LineCapConf LineCapEnd;

        /// <summary>
        /// Whether this line was created by freehand drawing.
        /// Freehand lines do not support line caps because their first and last
        /// segments can be too short or unstable for reliable cap calculation.
        /// </summary>
        public bool FreehandLine;

        /// <summary>
        /// Creates a <see cref="LineConf"/> for the given game object.
        /// </summary>
        /// <param name="lineGameObject">The game object with the <see cref="LineRenderer"/> component.</param>
        /// <returns>The created <see cref="LineConf"/> object.</returns>
        public static LineConf GetLine(GameObject lineGameObject)
        {
            LineConf line = null;
            if (lineGameObject != null && lineGameObject.CompareTag(Tags.Line))
            {
                LineRenderer renderer = lineGameObject.GetComponent<LineRenderer>();
                LineAnchorValueHolder anchorHolder = lineGameObject.GetComponent<LineAnchorValueHolder>();

                line = new()
                {
                    ID = lineGameObject.name,
                    AssociatedPage = lineGameObject.GetComponent<AssociatedPageHolder>().AssociatedPage,
                    Position = lineGameObject.transform.localPosition,
                    Scale = lineGameObject.transform.localScale,
                    OrderInLayer = lineGameObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer,
                    Loop = renderer.loop,
                    EulerAngles = lineGameObject.transform.localEulerAngles,
                    RendererPositions = new Vector3[renderer.positionCount],
                    LineCapStart = LineCapConf.GetLineStartCapConf(lineGameObject),
                    LineCapEnd = LineCapConf.GetLineEndCapConf(lineGameObject),
                    FreehandLine = lineGameObject.TryGetComponent(out LineValueHolder holder)
                                    && holder.FreehandLine,
                };

                renderer.GetPositions(line.RendererPositions);

                if (anchorHolder != null && anchorHolder.HasOriginalAnchors)
                {
                    line.OriginalStartAnchor = anchorHolder.OriginalStartAnchor;
                    line.OriginalEndAnchor = anchorHolder.OriginalEndAnchor;
                }
                else if (line.RendererPositions.Length > 0)
                {
                    line.OriginalStartAnchor = line.RendererPositions[0];
                    line.OriginalEndAnchor = line.RendererPositions[line.RendererPositions.Length - 1];
                }

                LineVisualConfFactory.ApplyVisualProperties(lineGameObject, renderer, line);
            }
            return line;
        }

        /// <summary>
        /// Gets a modified <see cref="LineConf"/> for the given object.
        /// The render positions are empty.
        /// It's needed to reduce the network resources.
        /// </summary>
        /// <param name="lineGameObject">The game object with the <see cref="LineRenderer"/> component.</param>
        /// <returns>The created <see cref="LineConf"/> without the renderer positions.</returns>
        public static LineConf GetLineWithoutRenderPos(GameObject lineGameObject)
        {
            LineConf conf = GetLine(lineGameObject);
            conf.RendererPositions = new Vector3[0];
            return conf;
        }

        /// <summary>
        /// Gets the fill out of the <see cref="LineConf"/>
        /// </summary>
        /// <param name="conf">The line conf.</param>
        /// <returns>The fill out color or null.</returns>
        public static Color? GetFillOutColor(LineConf conf)
        {
            return conf.FillOutStatus ? conf.FillOutColor : null;
        }

        /// <summary>
        /// Clones this line object.
        /// </summary>
        /// <returns>A copy of this line object.</returns>
        public object Clone()
        {
            return new LineConf
            {
                ID = this.ID,
                AssociatedPage = this.AssociatedPage,
                Position = this.Position,
                RendererPositions = this.RendererPositions?.ToArray(),
                Loop = this.Loop,
                PrimaryColor = this.PrimaryColor,
                SecondaryColor = this.SecondaryColor,
                ColorKind = this.ColorKind,
                OrderInLayer = this.OrderInLayer,
                Thickness = this.Thickness,
                EulerAngles = this.EulerAngles,
                Scale = this.Scale,
                LineKind = this.LineKind,
                Tiling = this.Tiling,
                FillOutStatus = this.FillOutStatus,
                FillOutColor = this.FillOutColor,
                LineCapStart = this.LineCapStart?.Clone() as LineCapConf,
                LineCapEnd = this.LineCapEnd?.Clone() as LineCapConf,
                OriginalStartAnchor = this.OriginalStartAnchor,
                OriginalEndAnchor = this.OriginalEndAnchor,
                FreehandLine = this.FreehandLine,
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the positions of the line renderer for a line.
        /// </summary>
        private const string rendererPositionsLabel = "RendererPositions";

        /// <summary>
        /// Label in the configuration file for the original start anchor of a line.
        /// </summary>
        private const string originalStartAnchorLabel = "OriginalStartAnchor";

        /// <summary>
        /// Label in the configuration file for the original end anchor of a line.
        /// </summary>
        private const string originalEndAnchorLabel = "OriginalEndAnchor";

        /// <summary>
        /// Label in the configuration file for the loop option of a line.
        /// </summary>
        private const string loopLabel = "LoopLabel";

        /// <summary>
        /// Label in the configuration file for the primary color of a line.
        /// </summary>
        private const string primaryColorLabel = "PrimaryColorLabel";

        /// <summary>
        /// Label in the configuration file for the secondary color of a line.
        /// </summary>
        private const string secondaryColorLabel = "SecondaryColorLabel";

        /// <summary>
        /// Label in the configuration file for the color kind of a line.
        /// </summary>
        private const string colorKindLabel = "ColorKindLabel";

        /// <summary>
        /// Label in the configuration file for the thickness of a line.
        /// </summary>
        private const string thicknessLabel = "ThicknessLabel";

        /// <summary>
        /// Label in the configuration file for the line kind of a line.
        /// </summary>
        private const string lineKindLabel = "LineKindLabel";

        /// <summary>
        /// Label in the configuration file for the tiling of a line with a "Dashed" line kind.
        /// </summary>
        private const string tilingLabel = "TilingLabel";

        /// <summary>
        /// Label in the configuration file for the fill out status of a line.
        /// </summary>
        private const string fillOutStatusLabel = "FillOutStatus";

        /// <summary>
        /// Label in the configuration file for the fill out color of a line.
        /// </summary>
        private const string fillOutColorLabel = "FillOutColorLabel";

        /// <summary>
        /// Label in the configuration file for the start cap configuration of a line.
        /// </summary>
        private const string lineCapStartLabel = "LineCapStart";

        /// <summary>
        /// Label in the configuration file for the end cap configuration of a line.
        /// </summary>
        private const string lineCapEndLabel = "LineCapEnd";

        /// <summary>
        /// Label in the configuration file for whether a line was created by freehand drawing.
        /// </summary>
        private const string freehandLineLabel = "FreehandLine";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(ColorKind.ToString(), colorKindLabel);
            writer.Save(PrimaryColor, primaryColorLabel);
            writer.Save(SecondaryColor, secondaryColorLabel);
            writer.Save(FillOutStatus, fillOutStatusLabel);
            writer.Save(FillOutColor, fillOutColorLabel);
            writer.Save(Thickness, thicknessLabel);
            writer.Save(Loop, loopLabel);
            writer.Save(LineKind.ToString(), lineKindLabel);
            writer.Save(Tiling, tilingLabel);
            writer.Save(OriginalStartAnchor, originalStartAnchorLabel);
            writer.Save(OriginalEndAnchor, originalEndAnchorLabel);
            writer.Save(FreehandLine, freehandLineLabel);

            List<Vector3Config> rendererPositionConfigs = RendererPositions
                .Select(pos => new Vector3Config { Value = pos })
                .ToList();

            writer.Save(rendererPositionConfigs, rendererPositionsLabel);

            LineCapStart.SaveAttributes(writer, lineCapStartLabel);
            LineCapEnd.SaveAttributes(writer, lineCapEndLabel);
        }

        /// <summary>
        /// Given the representation of a <see cref="LineConf"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="LineConf"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="LineConf"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="LineConf"/> was loaded without errors.</returns>
        new internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = base.Restore(attributes);

            /// Try to restore the color kind
            if (attributes.TryGetValue(colorKindLabel, out object cKind)
                && Enum.TryParse((string)cKind, out GameDrawer.ColorKind colorResult))
            {
                ColorKind = colorResult;
            }
            else
            {
                ColorKind = GameDrawer.ColorKind.Monochrome;
                errors = true;
            }

            /// Try to restore the primary color.
            Color loadedColor = Color.black;
            if (ConfigIO.Restore(attributes, primaryColorLabel, ref loadedColor))
            {
                PrimaryColor = loadedColor;
            }
            else
            {
                PrimaryColor = Color.black;
                errors = true;
            }

            /// Try to restore the secondary color.
            Color loadedSecColor = Color.black;
            if (ConfigIO.Restore(attributes, secondaryColorLabel, ref loadedSecColor))
            {
                SecondaryColor = loadedSecColor;
            }
            else
            {
                SecondaryColor = Color.black;
                errors = true;
            }

            /// Try to restore the fill out status.
            if (attributes.TryGetValue(fillOutStatusLabel, out object loadedFillOutStatus))
            {
                FillOutStatus = (bool)loadedFillOutStatus;
            }
            else
            {
                FillOutStatus = false;
                errors = true;
            }

            /// Try to restore the fill out color.
            Color loadedFOColor = Color.black;
            if (ConfigIO.Restore(attributes, fillOutColorLabel, ref loadedFOColor))
            {
                FillOutColor = loadedFOColor;
            }
            else
            {
                FillOutColor = Color.black;
                errors = true;
            }

            /// Try to restore the thickness.
            if (attributes.TryGetValue(thicknessLabel, out object thick))
            {
                Thickness = (float)thick;
            }
            else
            {
                Thickness = ValueHolder.CurrentThickness;
                errors = true;
            }

            /// Try to restore the loop.
            if (attributes.TryGetValue(loopLabel, out object loadedLoop))
            {
                Loop = (bool)loadedLoop;
            }
            else
            {
                Loop = false;
                errors = true;
            }

            List<Vector3> listRendererPositions = new();
            /// Try to restore the line position points.
            if (attributes.TryGetValue(rendererPositionsLabel, out object positionList))
            {
                foreach (object item in (List<object>) positionList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    Vector3Config config = new();
                    config.Restore(dict);
                    listRendererPositions.Add(config.Value);
                }
                RendererPositions = listRendererPositions.ToArray();
            }

            Vector3 loadedOriginalStartAnchor = Vector3.zero;
            if (ConfigIO.Restore(attributes, originalStartAnchorLabel, ref loadedOriginalStartAnchor))
            {
                OriginalStartAnchor = loadedOriginalStartAnchor;
            }
            else
            {
                OriginalStartAnchor = RendererPositions != null && RendererPositions.Length > 0
                    ? RendererPositions[0]
                    : Vector3.zero;
                errors = true;
            }

            Vector3 loadedOriginalEndAnchor = Vector3.zero;
            if (ConfigIO.Restore(attributes, originalEndAnchorLabel, ref loadedOriginalEndAnchor))
            {
                OriginalEndAnchor = loadedOriginalEndAnchor;
            }
            else
            {
                OriginalEndAnchor = RendererPositions != null && RendererPositions.Length > 0
                    ? RendererPositions[RendererPositions.Length - 1]
                    : Vector3.zero;
                errors = true;
            }

            /// Try to restore the tiling.
            if (attributes.TryGetValue(tilingLabel, out object til))
            {
                Tiling = (float)til;
            }
            else
            {
                Tiling = 1;
                errors = true;
            }

            /// Try to restore the line kind.
            if (attributes.TryGetValue(lineKindLabel, out object kind)
                && Enum.TryParse<GameDrawer.LineKind>((string)kind, out GameDrawer.LineKind result))
            {
                LineKind = result;
            }
            else
            {
                LineKind = GameDrawer.LineKind.Solid;
                errors = true;
            }

            /// Try to restore whether this line was created by freehand drawing.
            if (attributes.TryGetValue(freehandLineLabel, out object loadedFreehandLine))
            {
                FreehandLine = (bool)loadedFreehandLine;
            }
            else
            {
                FreehandLine = false;
            }

            if (attributes.TryGetValue(lineCapStartLabel, out object startCapObject))
            {
                Dictionary<string, object> startCapDict = (Dictionary<string, object>)startCapObject;
                LineCapConf startCap = new();
                if (startCap.Restore(startCapDict))
                {
                    LineCapStart = startCap;
                }
                else
                {
                    LineCapStart = LineCapConf.CreateNone();
                    errors = true;
                }
            }
            else
            {
                LineCapStart = LineCapConf.CreateNone();
                errors = true;
            }

            if (attributes.TryGetValue(lineCapEndLabel, out object endCapObject))
            {
                Dictionary<string, object> endCapDict = (Dictionary<string, object>)endCapObject;
                LineCapConf endCap = new();
                if (endCap.Restore(endCapDict))
                {
                    LineCapEnd = endCap;
                }
                else
                {
                    LineCapEnd = LineCapConf.CreateNone();
                    errors = true;
                }
            }
            else
            {
                LineCapEnd = LineCapConf.CreateNone();
                errors = true;
            }

            return !errors;
        }

        #endregion
    }
}