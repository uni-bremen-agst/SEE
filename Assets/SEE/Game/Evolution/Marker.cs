using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.GO;
using UnityEngine;
using UnityEngine.Rendering;
using static SEE.GO.Materials.ShaderType;
using Object = UnityEngine.Object;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// A factory for markers to highlight added, changed, and deleted nodes.
    /// The visual appearance of the markers are cylinders above the marked game objects.
    /// Their color depends upon whether they were added, deleted, or changed.
    /// The markers appear as beams with emissive light growing from 0 to the
    /// requested maximal marker height in a requested time.
    /// </summary>
    public class Marker
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="markerWidth">the width (x and z lengths) of the markers</param>
        /// <param name="markerHeight">the height (y length) of the markers</param>
        /// <param name="additionColor">the color for the markers for added nodes</param>
        /// <param name="changeColor">the color for the markers for changed nodes</param>
        /// <param name="deletionColor">the color for the markers for deleted nodes</param>
        public Marker(float markerWidth, float markerHeight,
                      Color additionColor, Color changeColor, Color deletionColor)
        {
            additionMarkerFactory = new CylinderFactory(Transparent, new ColorRange(additionColor, additionColor, 1));
            changeMarkerFactory = new CylinderFactory(Transparent, new ColorRange(changeColor, changeColor, 1));
            deletionMarkerFactory = new CylinderFactory(Transparent, new ColorRange(deletionColor, deletionColor, 1));

            if (markerHeight < 0)
            {
                this.markerHeight = 0;
                throw new ArgumentException("SEE.Game.Evolution.Marker received a negative marker height.\n");
            }
            else
            {
                this.markerHeight = markerHeight;
            }
            if (markerWidth < 0)
            {
                this.markerWidth = 0;
                throw new ArgumentException("SEE.Game.Evolution.Marker received a negative marker width.\n");
            }
            else
            {
                this.markerWidth = markerWidth;
            }
        }

        /// <summary>
        /// The strength factor of the emitted light for beam markers. It should be in the range [0,5].
        /// </summary>
        private const int EmissionStrength = 3;

        /// <summary>
        /// The gap between the decorated game node and its marker in Unity world-space units.
        /// </summary>
        private const float Gap = 0.001f;

        /// <summary>
        /// The height of the beam markers used to mark new, changed, and deleted
        /// objects from one version to the next one.
        /// </summary>
        private readonly float markerHeight;

        /// <summary>
        /// The width (x and z lengths) of the beam markers used to mark new, changed,
        /// and deleted objects from one version
        /// to the next one.
        /// </summary>
        private readonly float markerWidth;

        /// <summary>
        /// The list of beam markers added for the new game objects since the last call to Clear().
        /// </summary>
        private readonly List<GameObject> beamMarkers = new List<GameObject>();

        /// <summary>
        /// The factory to create beam markers above new blocks coming into existence.
        /// </summary>
        private readonly CylinderFactory additionMarkerFactory;

        /// <summary>
        /// The factory to create beam markers above changed existing blocks.
        /// </summary>
        private readonly CylinderFactory changeMarkerFactory;

        /// <summary>
        /// The factory to create beam markers above existing blocks ceasing to exist.
        /// </summary>
        private readonly CylinderFactory deletionMarkerFactory;

        /// <summary>
        /// Cached shader property for emission strength.
        /// </summary>
        private static readonly int Strength = Shader.PropertyToID("_EmissionStrength");

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as dying/getting alive by putting a
        /// beam marker on top of its roof. If <see cref="markerAttributes.Kind"/>
        /// equals <see cref="MarkerKinds.Stacked"/> the marker will be a set of
        /// stacked line segments, where the length of each segment is proportional
        /// to <see cref="markerAttributes.MarkerSections"/>
        /// </summary>
        /// <param name="gameNode">node above which to add a beam marker</param>
        /// <param name="factory">node above which to add a beam marker</param>
        /// <returns>the resulting beam marker</returns>
        private GameObject MarkByBeam(GameObject gameNode, NodeFactory factory)
        {
            // The marker should be drawn in front of the block, hence, its render
            // queue offset must be greater than the one of the block.
            GameObject beamMarker = NewBeam(factory, gameNode.GetRenderQueue() - (int)RenderQueue.Transparent);
            beamMarker.tag = Tags.Decoration;

            // The initial scale of the marker is Vector3.zero. Later its height will be adjusted to
            // markerWidth and markerHeight through the animator.
            beamMarker.transform.localScale = new Vector3(markerWidth, markerHeight, markerWidth);

            // Lift beamerMarker according to the length of its nested antenna
            // above the roof of the gameNode.
            Vector3 position = gameNode.transform.position;
            position.y = gameNode.GetMaxY();
            position.y += Gap + markerHeight / 2;
            beamMarker.transform.position = position;
            beamMarker.transform.SetParent(gameNode.transform);

            return beamMarker;
        }

        /// <summary>
        /// Creates a new beam marker using the given <paramref name="factory"/>.
        /// This new game object will have the given <paramref name="renderQueueOffset"/>.
        /// Emissive light is added to it. Its strength is defined by <see cref="EmissionStrength"/>.
        /// </summary>
        /// <param name="factory">the factory to create the beam marker</param>
        /// <param name="renderQueueOffset">offset in the render queue</param>
        /// <returns>new beam marker</returns>
        private static GameObject NewBeam(NodeFactory factory, int renderQueueOffset)
        {
            GameObject beamMarker = factory.NewBlock(0, renderQueueOffset);
            AddEmission(beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Adds emission strength to the given <paramref name="gameObject"/>.
        /// This strength defines the intensity of the emitted light. The
        /// strength is defined by <see cref="EmissionStrength"/>.
        ///
        /// Note: The sharedMaterial will be changed. That means all other objects
        /// having the same material will be affected, too.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a render whose
        /// material has a property _EmissionStrength.
        /// </summary>
        /// <param name="gameObject">the object receiving the emission strength</param>
        private static void AddEmission(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                // Set power beam material to emissive
                renderer.sharedMaterial.SetFloat(Strength, EmissionStrength);
            }
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as dying by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkDead(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, deletionMarkerFactory);
            beamMarker.name = "dead " + gameNode.name;
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as coming into existence by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkBorn(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, additionMarkerFactory);
            beamMarker.name = "new " + gameNode.name;
            // We need to add the marker to beamMarkers so that it can be destroyed at the beginning of the
            // next animation cycle.
            beamMarkers.Add(beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as being changed by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkChanged(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, changeMarkerFactory);
            beamMarker.name = "changed " + gameNode.name;
            // We need to add beam marker to beamMarkers so that it can be destroyed at the beginning of the
            // next animation cycle.
            beamMarkers.Add(beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Destroys all marking created since the last call to Clear(). Clears the
        /// cache of markers.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject gameObject in beamMarkers)
            {
                gameObject.transform.SetParent(null);
                Object.Destroy(gameObject);
            }
            beamMarkers.Clear();
        }
    }
}