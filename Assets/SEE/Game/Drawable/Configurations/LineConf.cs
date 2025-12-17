using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for a drawable line.
    /// </summary>
    [Serializable]
    public class LineConf : DrawableType, ICloneable
    {
        /// <summary>
        /// The renderer positions of the drawn points.
        /// </summary>
        public Vector3[] RendererPositions;

        /// <summary>
        /// The configurations of the drawn points.
        /// Will be needed for correct saving / loading.
        /// </summary>
        private readonly List<Vector3Config> rendererPositionConfigs = new();

        /// <summary>
        /// If true, the line should loop.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// The primary color of the line.
        /// </summary>
        public Color PrimaryColor;

        /// <summary>
        /// The secondary color of the line.
        /// </summary>
        public Color SecondaryColor;

        /// <summary>
        /// The color kind of the line (Monochrome/Gradient/Two-color dashed).
        /// </summary>
        public GameDrawer.ColorKind ColorKind;

        /// <summary>
        /// The thickness of the line.
        /// </summary>
        public float Thickness;

        /// <summary>
        /// The line kind of the line (Solid/Dashed/Dashed25/Dashed50/Dashed75/Dashed100)
        /// </summary>
        public GameDrawer.LineKind LineKind;

        /// <summary>
        /// The tiling of a dashed line. Only used for "Dashed" line kind.
        /// </summary>
        public float Tiling;

        /// <summary>
        /// Whether the fill out is active or not.
        /// </summary>
        public bool FillOutStatus;

        /// <summary>
        /// The fill out color; null if the line has no fill out.
        /// </summary>
        public Color FillOutColor;

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
                GameObject fillout = GameFinder.FindChild(lineGameObject, ValueHolder.FillOut);
                line = new()
                {
                    ID = lineGameObject.name,
                    AssociatedPage = lineGameObject.GetComponent<AssociatedPageHolder>().AssociatedPage,
                    Position = lineGameObject.transform.localPosition,
                    Scale = lineGameObject.transform.localScale,
                    OrderInLayer = lineGameObject.GetComponent<OrderInLayerValueHolder>().OrderInLayer,
                    Thickness = renderer.startWidth,
                    Tiling = renderer.textureScale.x,
                    LineKind = lineGameObject.GetComponent<LineValueHolder>().LineKind,
                    Loop = renderer.loop,
                    EulerAngles = lineGameObject.transform.localEulerAngles,
                    ColorKind = lineGameObject.GetComponent<LineValueHolder>().ColorKind,
                    RendererPositions = new Vector3[renderer.positionCount],
                    FillOutStatus = fillout != null,
                    FillOutColor = fillout != null ? GameFinder.FindChild(lineGameObject, ValueHolder.FillOut).GetColor() : Color.clear
                };
                renderer.GetPositions(line.RendererPositions);
                switch (line.ColorKind)
                {
                    case GameDrawer.ColorKind.Monochrome:
                        line.PrimaryColor = renderer.material.color;
                        line.SecondaryColor = Color.clear;
                        break;
                    case GameDrawer.ColorKind.Gradient:
                        line.PrimaryColor = renderer.startColor;
                        line.SecondaryColor = renderer.endColor;
                        break;
                    case GameDrawer.ColorKind.TwoDashed:
                        line.PrimaryColor = renderer.materials[0].color;
                        line.SecondaryColor = renderer.materials[1].color;
                        break;
                }
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
                RendererPositions = this.RendererPositions,
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
            };
        }

        #region Config I/O

        /// <summary>
        /// Label in the configuration file for the positions of the line renderer for a line.
        /// </summary>
        private const string rendererPositionsLabel = "RendererPositions";

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
            foreach(Vector3 pos in RendererPositions)
            {
                rendererPositionConfigs.Add(new Vector3Config() { Value = pos });
            }
            writer.Save(rendererPositionConfigs, rendererPositionsLabel);
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

            return !errors;
        }

        #endregion
    }
}