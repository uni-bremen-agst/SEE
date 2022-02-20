using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.GameObjects.Decorators
{
    /// <summary>
    /// A decorater for game nodes generating an antenna representing various metrics
    /// above the game node.
    /// </summary>
    internal class AntennaDecorator
    {
        public AntennaDecorator(IScale scaler, AntennaAttributes antennaAttributes)
        {
            this.scaler = scaler;
            this.antennaAttributes = antennaAttributes;
        }

        private IScale scaler;
        private AntennaAttributes antennaAttributes;

        /// <summary>
        /// Transformation method to scale numbers. Yields <paramref name="input"/> devided
        /// by 100.
        /// </summary>
        /// <returns>input / 100</returns>
        private static float Transform(float input)
        {
            return input;
        }

        public void AddAntenna(GameObject gameNode, int renderQueueOffset = 0)
        {
            Node node = gameNode.GetNode();

            // The local position of the beamMarker to be added in the current iteration
            // relative to the parent. We will adjust only the y component of it within
            // each iteration. At the end, the y component is the total length of the
            // created segments.
            Vector3 segmentPosition = Vector3.zero;
            // The world-space scale of the beamMarker to be added in the current iteration.
            // We will adjust only the y component of it for each iteration.
            Vector3 segmentScale = new Vector3(antennaAttributes.AntennaWidth, 0, antennaAttributes.AntennaWidth);

            // We will first create the segment markers as if they are to appear
            // at the center position of parent. Only if we know their total length,
            // we can place them below their parent.
            foreach (AntennaSection section in antennaAttributes.AntennaSections)
            {
                if (node.TryGetNumeric(section.Metric, out float sectionMetric) && sectionMetric > 0)
                {
                    NodeFactory sectionFactory = GetSegmentFactory(section);

                    // The marker should be drawn as part of the parent, hence, its render
                    // queue offset must be equal to that of the parent.
                    GameObject segmentObject = NewSegment(sectionFactory, renderQueueOffset);
                    segmentObject.name = section.Metric + ": " + sectionMetric;
                    segmentObject.tag = Tags.Decoration;

                    float sectionHeight = Transform(scaler.GetNormalizedValue(section.Metric, sectionMetric));
                    segmentScale.y = sectionHeight;
                    segmentObject.transform.localScale = segmentScale;
                    segmentObject.transform.SetParent(gameNode.transform);
                    float sectionExtent = sectionHeight / 2;
                    segmentPosition.y += sectionExtent;
                    segmentObject.transform.localPosition = segmentPosition;
                    segmentPosition.y += sectionExtent;
                }
            }

            // lower all segment markers so that they appear below their parent.
            foreach (Transform child in gameNode.transform)
            {
                // FIXME
            }
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

        private static NodeFactory GetSegmentFactory(AntennaSection section)
        {
            // FIXME Add a cache for these factories. They should not be created for each marker and loop iteration.
            return new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(section.Color, section.Color, 1));
        }
    }
}
