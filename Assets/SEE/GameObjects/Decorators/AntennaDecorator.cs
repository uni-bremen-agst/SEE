using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO.Factories.NodeFactories;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GO.Decorators
{
    /// <summary>
    /// A decorator for game nodes generating an antenna representing various metrics
    /// above a game node (leaf or inner alike).
    /// </summary>
    internal class AntennaDecorator
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="scaler">the scaler to be used to normalize the metric values</param>
        /// <param name="antennaAttributes">the visual attributes to be considered for the antenna segments</param>
        /// <param name="antennaWidth">the width of every antenna segment</param>
        /// <param name="maximalAntennaSegmentHeight">the maximal height of an individual antenna segment</param>
        /// <param name="metricToColor">a mapping of metric names onto colors</param>
        public AntennaDecorator(IScale scaler,
                                AntennaAttributes antennaAttributes,
                                float antennaWidth,
                                float maximalAntennaSegmentHeight,
                                ColorMap metricToColor)
        {
            this.scaler = scaler;
            this.antennaAttributes = antennaAttributes;
            this.antennaWidth = antennaWidth;
            this.maximalAntennaSegmentHeight = maximalAntennaSegmentHeight;
            metricToFactory = CreateSegmentFactories(antennaAttributes, metricToColor);
        }

        /// <summary>
        /// A mapping of metric names within <see cref="antennaAttributes"/> onto <see cref="CylinderFactory"/>s,
        /// which are to be used to create the cylinders for the antenna segments.
        /// </summary>
        private readonly Dictionary<string, CylinderFactory> metricToFactory;

        /// <summary>
        /// The scaler to be used to normalize the metric values.
        /// </summary>
        private readonly IScale scaler;

        /// <summary>
        /// The visual attributes to be considered for the antenna segments.
        /// </summary>
        private readonly AntennaAttributes antennaAttributes;

        /// <summary>
        /// The width of an antenna segment.
        /// </summary>
        private readonly float antennaWidth;

        /// <summary>
        /// The maximal height of an individual antenna segment.
        /// </summary>
        private readonly float maximalAntennaSegmentHeight;

        /// <summary>
        /// The name of the game object representing the antenna as a whole. It
        /// will be an immediate child of the node to be decorated. The antenna
        /// segments are the immediate children of this antenna game object.
        /// </summary>
        private const string antennaGameObjectName = "Antenna";

        /// <summary>
        /// Adds an antenna above <paramref name="gameNode"/>. The antenna consists of
        /// antenna segments as specified by the <see cref="AntennaAttributes"/> passed
        /// to the constructor. All created antenna segments will be children of a new
        /// empty game object named <see cref="antennaGameObjectName"/>, which in turn
        /// will be an immediate child of <paramref name="gameNode"/>. If <paramref name="gameNode"/>
        /// does not have any of the metrics specified in <see cref="antennaAttributes"/>,
        /// nothing happens.
        /// The resulting antenna will have the same portal as <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">the game node to be decorated (must have a graph
        /// node attached)</param>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> does
        /// not have a graph node attached</exception>
        public void AddAntenna(GameObject gameNode)
        {
            RemoveAntenna(gameNode);

            // The empty antenna object that will be the parent of all
            // antenna segments. It will be created on demand, that is,
            // if we have at least one antenna segment.
            GameObject antenna = null;
            Node node = gameNode.GetNode();

            // The world-space position of the segment to be added in the current iteration.
            // Initially set to the center of the roof of gameNode.
            // We will adjust only the y component of it within each iteration.
            Vector3 segmentPosition = gameNode.transform.position;
            segmentPosition.y = gameNode.GetRoof();

            // The world-space scale of the segment to be added in the current iteration.
            // We will adjust only the y component of it for each iteration based on
            // segment's metric.
            Vector3 segmentScale = GetSegmentScale(gameNode);

            foreach (string metricName in antennaAttributes.AntennaSections)
            {
                if (node.TryGetNumeric(metricName, out float sectionMetric) && sectionMetric > 0)
                {
                    GameObject segment = NewSegment(metricToFactory[metricName]);
                    segment.name = metricName + ": " + sectionMetric;
                    segment.tag = Tags.Decoration;

                    float height = Mathf.Clamp(scaler.GetNormalizedValue(metricName, sectionMetric), 0, maximalAntennaSegmentHeight);
                    segmentScale.y = height;
                    segment.transform.localScale = segmentScale;

                    float extent = height / 2;
                    segmentPosition.y += extent;
                    segment.transform.position = segmentPosition;
                    segmentPosition.y += extent;

                    AddToAntenna(segment);

                    Portal.InheritPortal(gameNode, segment);
                }
            }

            antenna?.transform.SetParent(gameNode.transform);

            void AddToAntenna(GameObject segment)
            {
                if (antenna == null)
                {
                    antenna = new(antennaGameObjectName)
                    {
                        tag = Tags.Decoration
                    };
                    antenna.transform.localScale = Vector3.one;
                }
                segment.transform.SetParent(antenna.transform);
            }

            // returns an antenna scale such that the antenna fits on the gameNode.
            Vector3 GetSegmentScale(GameObject gameNode)
            {
                Vector3 scale = gameNode.transform.lossyScale;
                float width = Mathf.Min(antennaWidth,
                                        Mathf.Min(scale.x, scale.z));
                return new Vector3(width, 0, width);
            }
        }

        /// <summary>
        /// Removes the antenna from <paramref name="gameNode"/> if it has one.
        /// </summary>
        /// <param name="gameNode">the game node to be decorated (must have a graph
        /// node attached)</param>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> does
        /// not have a graph node attached</exception>
        public static void RemoveAntenna(GameObject gameNode)
        {
            foreach (Transform child in gameNode.transform)
            {
                if (child.name == antennaGameObjectName && child.CompareTag(Tags.Decoration))
                {
                    child.transform.SetParent(null);
                    Destroyer.Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Returns a mapping of metric names within <paramref name="antennaAttributes"/> onto
        /// <see cref="CylinderFactory"/>s, which are to be used to create the cylinders for the
        /// antenna segments. The color for these cylinders is the one provided in <paramref name="antennaAttributes"/>
        /// for the respective metric.
        /// </summary>
        /// <param name="antennaAttributes">a specification of the antenna segments for which to create
        /// the cylinder factories</param>
        /// <param name="metricToColor">a mapping of metric names onto colors</param>
        /// <returns>mapping of metrics onto factories</returns>
        private static Dictionary<string, CylinderFactory> CreateSegmentFactories(AntennaAttributes antennaAttributes, ColorMap metricToColor)
        {
            Dictionary<string, CylinderFactory> result = new(antennaAttributes.AntennaSections.Count);
            foreach (string metricName in antennaAttributes.AntennaSections)
            {
                Color color;
                if (!metricToColor.TryGetValue(metricName, out ColorRange colorRange))
                {
                    Debug.LogWarning($"No antenna-segment color specification for metric {metricName}.\n");
                    color = Color.white;
                }
                else
                {
                    color = colorRange.Upper;
                }
                result[metricName] = new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(color, color, 1));
            }
            return result;
        }

        /// <summary>
        /// Creates a new segment using the given <paramref name="factory"/>.
        /// </summary>
        /// <param name="factory">the factory to create the beam marker</param>
        /// <returns>new segment</returns>
        private static GameObject NewSegment(NodeFactory factory)
        {
            return factory.NewBlock();
        }
    }
}
