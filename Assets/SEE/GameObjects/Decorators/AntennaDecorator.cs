using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
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
        public AntennaDecorator(IScale scaler, AntennaAttributes antennaAttributes)
        {
            this.scaler = scaler;
            this.antennaAttributes = antennaAttributes;
            metricToFactory = CreateSegmentFactories(antennaAttributes);
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
        /// The name of the game object representing the antenna as a whole. It
        /// will be an immediate child of the node to be decorated. The antenna
        /// segments are the immediate children of this antenna game object.
        /// </summary>
        private const string AntennaGameObjectName = "Antenna";

        /// <summary>
        /// Adds an antenna above <paramref name="gameNode"/>. The antenna consists of
        /// antenna segments as specified by the <see cref="AntennaAttributes"/> passed
        /// to the constructor. All created antenna segments will be children of a new
        /// empty game object named <see cref="AntennaGameObjectName"/>, which in turn
        /// will be an immediate child of <paramref name="gameNode"/>. If <paramref name="gameNode"/>
        /// does not have any of the metrics specified in <see cref="antennaAttributes"/>,
        /// nothing happens.
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
            // if we have at least on antenna segment.
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
            Vector3 segmentScale = new Vector3(antennaAttributes.AntennaWidth, 0, antennaAttributes.AntennaWidth);

            foreach (AntennaSection section in antennaAttributes.AntennaSections)
            {
                if (node.TryGetNumeric(section.Metric, out float sectionMetric) && sectionMetric > 0)
                {
                    NodeFactory segmentFactory = metricToFactory[section.Metric];
                    GameObject segment = NewSegment(segmentFactory, gameNode.RenderLater());
                    segment.name = section.Metric + ": " + sectionMetric;
                    segment.tag = Tags.Decoration;

                    float height = scaler.GetNormalizedValue(section.Metric, sectionMetric);
                    segmentScale.y = height;
                    segment.transform.localScale = segmentScale;

                    float extent = height / 2;
                    segmentPosition.y += extent;
                    segment.transform.position = segmentPosition;
                    segmentPosition.y += extent;

                    AddToAntenna(segment);
                }
            }

            antenna?.transform.SetParent(gameNode.transform);

            void AddToAntenna(GameObject segment)
            {
                if (antenna == null)
                {
                    antenna = new GameObject(AntennaGameObjectName);
                    antenna.tag = Tags.Decoration;
                    antenna.transform.localScale = Vector3.one;
                }
                segment.transform.SetParent(antenna.transform);
            }
        }

        /// <summary>
        /// Removes the antenna from <paramref name="gameNode"/> if it has one.
        /// </summary>
        /// <param name="gameNode">the game node to be decorated (must have a graph
        /// node attached)</param>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> does
        /// not have a graph node attached</exception>
        public void RemoveAntenna(GameObject gameNode)
        {
            foreach (Transform child in gameNode.transform)
            {
                if (child.name == AntennaGameObjectName && child.CompareTag(Tags.Decoration))
                {
                    child.transform.SetParent(null);
                    Destroyer.DestroyGameObject(child.gameObject);
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
        /// <returns>mapping of metrics onto factories</returns>
        private static Dictionary<string, CylinderFactory> CreateSegmentFactories(AntennaAttributes antennaAttributes)
        {
            Dictionary<string, CylinderFactory> result = new Dictionary<string, CylinderFactory>(antennaAttributes.AntennaSections.Count);
            foreach (AntennaSection section in antennaAttributes.AntennaSections)
            {
                result[section.Metric] = new CylinderFactory(Materials.ShaderType.Transparent, new ColorRange(section.Color, section.Color, 1));
            }
            return result;
        }

        /// <summary>
        /// Creates a new segment using the given <paramref name="factory"/>.
        /// This new game object will have the given <paramref name="renderQueueOffset"/>.
        /// </summary>
        /// <param name="factory">the factory to create the beam marker</param>
        /// <param name="renderQueueOffset">offset in the render queue</param>
        /// <returns>new segment</returns>
        private static GameObject NewSegment(NodeFactory factory, int renderQueueOffset)
        {
            return factory.NewBlock(0, renderQueueOffset);
        }
    }
}
