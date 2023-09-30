using Newtonsoft.Json.Linq;
using SEE.Game;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Assets.SEE.Game.Drawable
{
    [Serializable]
    public class Line : ICloneable
    {
        public GameObject gameObject;

        public string id;

        public Vector3 position;

        public Vector3 scale;

        public Vector3[] rendererPositions;

        private List<Vector3Config> rendererPositionConfigs = new();

        public bool loop;

        public Color color;

        public int orderInLayer;

        public float thickness;

        public Vector3 holderEulerAngles;

        public Vector3 holderPosition;

        private const string IdLabel = "IDLabel";
        private const string PositionLabel = "PositionLabel";
        private const string ScaleLabel = "ScaleLabel";
        private const string RendererPositionsLabel = "RendererPositions";
        private const string RenderPosPrefix = "RenPos";
        private const string LoopLabel = "LoopLabel";
        private const string ColorLabel = "ColorLabel";
        private const string OrderInLayerLabel = "OrderInLayerLabel";
        private const string ThicknessLabel = "ThicknessLabel";
        private const string HolderEulerAnglesLabel = "HolderEulerAnglesLabel";
        private const string HolderPositionLabel = "HolderPositionLabel";

        public static Line GetLine(GameObject lineGameObject)
        {
            Line line = null;
            if (lineGameObject != null && lineGameObject.CompareTag(Tags.Line))
            {
                Transform holderTransform = lineGameObject.transform.parent;
                line = new();
                line.gameObject = lineGameObject;
                line.id = lineGameObject.name;
                line.position = lineGameObject.transform.position;
                line.scale = lineGameObject.transform.localScale;
                LineRenderer renderer = lineGameObject.GetComponent<LineRenderer>();
                line.rendererPositions = new Vector3[renderer.positionCount];
                renderer.GetPositions(line.rendererPositions);
                line.loop = renderer.loop;
                line.holderEulerAngles = holderTransform.localEulerAngles;
                line.holderPosition = holderTransform.localPosition;
                line.color = renderer.material.color;
                line.orderInLayer = renderer.sortingOrder;
                line.thickness = renderer.startWidth;
            }
            return line;
        }

        public object Clone()
        {
            return new Line
            {
                gameObject = this.gameObject,
                id = this.id,
                position = this.position,
                rendererPositions = this.rendererPositions,
                loop = this.loop,
                color = this.color,
                orderInLayer = this.orderInLayer,
                thickness = this.thickness,
                holderEulerAngles = this.holderEulerAngles,
                holderPosition = this.holderPosition,
                scale = this.scale,
            };
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            writer.Save(id, IdLabel);
            writer.Save(position, PositionLabel);
            writer.Save(scale, ScaleLabel);
            writer.Save(color, ColorLabel);
            writer.Save(orderInLayer, OrderInLayerLabel);
            writer.Save(thickness, ThicknessLabel);
            writer.Save(loop, LoopLabel);
            
            foreach(Vector3 pos in rendererPositions)
            {
                rendererPositionConfigs.Add(new Vector3Config() { vector = pos });
            }
            writer.Save(rendererPositionConfigs, RendererPositionsLabel);
            writer.Save(holderEulerAngles, HolderEulerAnglesLabel);
            writer.Save(holderPosition, HolderPositionLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Given the representation of a <see cref="Line"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="Line"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="Line"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="Line"/> was loaded without errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;
            if (attributes.TryGetValue(IdLabel, out object name))
            {
                id = (string)name;
            }
            else
            {
                errors = true;
            }
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

            Color loadedColor = Color.black;
            if (ConfigIO.Restore(attributes, ColorLabel, ref loadedColor))
            {
                color = loadedColor;
            }
            else
            {
                color = Color.black;
                errors = true;
            }

            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref orderInLayer))
            {
                orderInLayer = DrawableHelper.orderInLayer;
                DrawableHelper.orderInLayer++;
                errors = true;
            }

            if (attributes.TryGetValue(ThicknessLabel, out object thick))
            {
                thickness = (float)thick;
            }
            else
            {
                thickness = DrawableHelper.currentThickness;
                errors = true;
            }

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
            
            Vector3 loadedHolderPosition = Vector3.zero;
            if (ConfigIO.Restore(attributes, HolderPositionLabel, ref loadedHolderPosition))
            {
                holderPosition = loadedHolderPosition;
            }
            else
            {
                holderPosition = Vector3.zero;
                errors = true;
            }
            Vector3 loadedEulerAngles = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref loadedEulerAngles))
            {
                holderEulerAngles = loadedEulerAngles;
            }
            else
            {
                holderEulerAngles = Vector3.zero;
                errors = true;
            }

            return !errors;
        }
    }
}