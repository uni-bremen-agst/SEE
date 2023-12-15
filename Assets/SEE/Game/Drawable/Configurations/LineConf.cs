using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
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
        /// The position of the line.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The scale of the line.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// The renderer positions of the drawed points.
        /// </summary>
        public Vector3[] rendererPositions;

        /// <summary>
        /// The configurations of the drawed points.
        /// Will be needed for correct saving / loading.
        /// </summary>
        private List<Vector3Config> rendererPositionConfigs = new();

        /// <summary>
        /// Is the option, if the line should loop.
        /// </summary>
        public bool loop;

        /// <summary>
        /// The primary color of the line.
        /// </summary>
        public Color primaryColor;

        /// <summary>
        /// The secondary color of the line.
        /// </summary>
        public Color secondaryColor;

        /// <summary>
        /// The color kind of the line (Monochrome/Gradient/Two-color dashed)
        /// </summary>
        public GameDrawer.ColorKind colorKind;

        /// <summary>
        /// The order in layer for this drawable object.
        /// </summary>
        public int orderInLayer;

        /// <summary>
        /// The thickness of the line.
        /// </summary>
        public float thickness;

        /// <summary>
        /// The euler angles of the line.
        /// </summary>
        public Vector3 eulerAngles;

        /// <summary>
        /// The line kind of the line (Solid/Dashed/Dashed25/Dashed50/Dashed75/Dashed100)
        /// </summary>
        public GameDrawer.LineKind lineKind;

        /// <summary>
        /// The tiling of a dashed line. Only used for "Dashed" line kind.
        /// </summary>
        public float tiling;

        /// <summary>
        /// Label in the configuration file for the id of a line.
        /// </summary>
        private const string IDLabel = "IDLabel";

        /// <summary>
        /// Label in the configuration file for the position of a line.
        /// </summary>
        private const string PositionLabel = "PositionLabel";

        /// <summary>
        /// Label in the configuration file for the scale of a line.
        /// </summary>
        private const string ScaleLabel = "ScaleLabel";

        /// <summary>
        /// Label in the configuration file for the positions of the line renderer for a line.
        /// </summary>
        private const string RendererPositionsLabel = "RendererPositions";

        /// <summary>
        /// Label in the configuration file for the loop option of a line.
        /// </summary>
        private const string LoopLabel = "LoopLabel";

        /// <summary>
        /// Label in the configuration file for the primary color of a line.
        /// </summary>
        private const string PrimaryColorLabel = "PrimaryColorLabel";

        /// <summary>
        /// Label in the configuration file for the secondary color of a line.
        /// </summary>
        private const string SecondaryColorLabel = "SecondaryColorLabel";

        /// <summary>
        /// Label in the configuration file for the color kind of a line.
        /// </summary>
        private const string ColorKindLabel = "ColorKindLabel";

        /// <summary>
        /// Label in the configuration file for the order in layer of a line.
        /// </summary>
        private const string OrderInLayerLabel = "OrderInLayerLabel";

        /// <summary>
        /// Label in the configuration file for the thickness of a line.
        /// </summary>
        private const string ThicknessLabel = "ThicknessLabel";

        /// <summary>
        /// Label in the configuration file for the euler angles of a line.
        /// </summary>
        private const string EulerAnglesLabel = "EulerAnglesLabel";

        /// <summary>
        /// Label in the configuration file for the line kind of a line.
        /// </summary>
        private const string LineKindLabel = "LineKindLabel";

        /// <summary>
        /// Label in the configuration file for the tiling of a line with a "Dashed" line kind.
        /// </summary>
        private const string TilingLabel = "TilingLabel";

        /// <summary>
        /// Creates a <see cref="LineConf"/> for the given game object.
        /// </summary>
        /// <param name="lineGameObject">The game object with the <see cref="LineRenderer"/> component</param>
        /// <returns>The created <see cref="LineConf"/> object</returns>
        public static LineConf GetLine(GameObject lineGameObject)
        {
            LineConf line = null;
            if (lineGameObject != null && lineGameObject.CompareTag(Tags.Line))
            {   
                LineRenderer renderer = lineGameObject.GetComponent<LineRenderer>();
                line = new()
                {
                    id = lineGameObject.name,
                    position = lineGameObject.transform.localPosition,
                    scale = lineGameObject.transform.localScale,
                    orderInLayer = lineGameObject.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer(),
                    thickness = renderer.startWidth,
                    tiling = renderer.textureScale.x,
                    lineKind = lineGameObject.GetComponent<LineValueHolder>().GetLineKind(),
                    loop = renderer.loop,
                    eulerAngles = lineGameObject.transform.localEulerAngles,
                    colorKind = lineGameObject.GetComponent<LineValueHolder>().GetColorKind(),
                    rendererPositions = new Vector3[renderer.positionCount]
                };
                renderer.GetPositions(line.rendererPositions);
                switch (line.colorKind)
                {
                    case GameDrawer.ColorKind.Monochrome:
                        line.primaryColor = renderer.material.color;
                        line.secondaryColor = Color.clear;
                        break;
                    case GameDrawer.ColorKind.Gradient:
                        line.primaryColor = renderer.startColor;
                        line.secondaryColor = renderer.endColor;
                        break;
                    case GameDrawer.ColorKind.TwoDashed:
                        line.primaryColor = renderer.materials[0].color;
                        line.secondaryColor = renderer.materials[1].color;
                        break;
                }
            }
            return line;
        }

        /// <summary>
        /// Clons the line object.
        /// </summary>
        /// <returns>A copy of this line object.</returns>
        public object Clone()
        {
            return new LineConf
            {
                id = this.id,
                position = this.position,
                rendererPositions = this.rendererPositions,
                loop = this.loop,
                primaryColor = this.primaryColor,
                secondaryColor = this.secondaryColor,
                colorKind = this.colorKind,
                orderInLayer = this.orderInLayer,
                thickness = this.thickness,
                eulerAngles = this.eulerAngles,
                scale = this.scale,
                lineKind = this.lineKind,
                tiling = this.tiling,
            };
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            writer.Save(id, IDLabel);
            writer.Save(position, PositionLabel);
            writer.Save(scale, ScaleLabel);
            writer.Save(colorKind.ToString(), ColorKindLabel);
            writer.Save(primaryColor, PrimaryColorLabel);
            writer.Save(secondaryColor, SecondaryColorLabel);
            writer.Save(orderInLayer, OrderInLayerLabel);
            writer.Save(thickness, ThicknessLabel);
            writer.Save(loop, LoopLabel);
            writer.Save(lineKind.ToString(), LineKindLabel);
            writer.Save(tiling, TilingLabel);
            foreach(Vector3 pos in rendererPositions)
            {
                rendererPositionConfigs.Add(new Vector3Config() { vector = pos });
            }
            writer.Save(rendererPositionConfigs, RendererPositionsLabel);
            writer.Save(eulerAngles, EulerAnglesLabel);
            writer.EndGroup();
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
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;

            /// Try to restores the id.
            if (attributes.TryGetValue(IDLabel, out object name))
            {
                id = (string)name;
            }
            else
            {
                errors = true;
            }

            /// Try to restores the position.
            Vector3 loadedPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, PositionLabel, ref loadedPosition))
            {
                position = loadedPosition;
            }
            else
            {
                position = Vector3.zero;
                errors = true;
            }

            /// Try to restores the scale.
            Vector3 loadedScale = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref loadedScale))
            {
                scale = loadedScale;
            }
            else
            {
                scale = Vector3.zero;
                errors = true;
            }

            /// Try to restores the color kind
            if (attributes.TryGetValue(ColorKindLabel, out object cKind) 
                && Enum.TryParse((string)cKind, out GameDrawer.ColorKind colorResult))
            {
                colorKind = colorResult;
            }
            else
            {
                colorKind = GameDrawer.ColorKind.Monochrome;
                errors = true;
            }

            /// Try to restores the primary color.
            Color loadedColor = Color.black;
            if (ConfigIO.Restore(attributes, PrimaryColorLabel, ref loadedColor))
            {
                primaryColor = loadedColor;
            }
            else
            {
                primaryColor = Color.black;
                errors = true;
            }

            /// Try to restores the secondary color.
            Color loadedSecColor = Color.black;
            if (ConfigIO.Restore(attributes, SecondaryColorLabel, ref loadedSecColor))
            {
                secondaryColor = loadedSecColor;
            }
            else
            {
                secondaryColor = Color.black;
                errors = true;
            }

            /// Try to restores the order in layer.
            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref orderInLayer))
            {
                errors = true;
            }

            /// Try to restores the thickness.
            if (attributes.TryGetValue(ThicknessLabel, out object thick))
            {
                thickness = (float)thick;
            }
            else
            {
                thickness = ValueHolder.currentThickness;
                errors = true;
            }

            /// Try to restores the loop.
            if (attributes.TryGetValue(LoopLabel, out object loadedLoop))
            {
                loop = (bool)loadedLoop;
            }
            else
            {
                loop = false;
                errors = true;
            }
            
            List<Vector3> listRendererPositions = new();
            /// Try to restores the line position points.
            if (attributes.TryGetValue(RendererPositionsLabel, out object positionList))
            {
                foreach (object item in (List<object>) positionList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    Vector3Config config = new();
                    config.Restore(dict);
                    listRendererPositions.Add(config.vector);
                }
                rendererPositions = listRendererPositions.ToArray();
            }

            /// Try to restores the euler angles.
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, EulerAnglesLabel, ref loadedEulerAngles))
            {
                eulerAngles = loadedEulerAngles;
            }
            else
            {
                eulerAngles = Vector3.zero;
                errors = true;
            }

            /// Try to restores the tiling.
            if (attributes.TryGetValue(TilingLabel, out object til))
            {
                tiling = (float)til;
            }
            else
            {
                tiling = 1;
                errors = true;
            }

            /// Try to restores the line kind.
            if (attributes.TryGetValue(LineKindLabel, out object kind) 
                && Enum.TryParse<GameDrawer.LineKind>((string)kind, out GameDrawer.LineKind result))
            {
                lineKind = result;
            }
            else
            {
                lineKind = GameDrawer.LineKind.Solid;
                errors = true;
            }

            return !errors;
        }
    }
}