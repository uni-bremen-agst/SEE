using System;
using System.Collections.Generic;
using DG.Tweening;
using SEE.Game;
using SEE.Game.City;
using SEE.GO.Factories.NodeFactories;
using SEE.Utils;
using UnityEngine;
using static SEE.GO.Factories.MaterialsFactory.ShaderType;

namespace SEE.GO.Factories
{
    /// <summary>
    /// A factory for markers to highlight added, changed, and deleted nodes.
    /// The visual appearance of the markers are cylinders above the marked game objects.
    /// Their color depends upon whether they were added, deleted, or changed.
    /// The markers appear as beams with emissive light growing from 0 to the
    /// requested maximal marker height in a requested time.
    /// </summary>
    public class MarkerFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="markerAttributes">the attributes to be used</param>
        public MarkerFactory(MarkerAttributes markerAttributes)
        {
            additionMarkerFactory = new CylinderFactory(Opaque,
                new ColorRange(markerAttributes.AdditionBeamColor, markerAttributes.AdditionBeamColor, 1));
            changeMarkerFactory = new CylinderFactory(Opaque,
                new ColorRange(markerAttributes.ChangeBeamColor, markerAttributes.ChangeBeamColor, 1));
            deletionMarkerFactory = new CylinderFactory(Opaque,
                new ColorRange(markerAttributes.DeletionBeamColor, markerAttributes.DeletionBeamColor, 1));

            if (markerAttributes.MarkerHeight < 0)
            {
                throw new ArgumentException($"{nameof(MarkerFactory)} received a negative marker height.\n");
            }
            if (markerAttributes.MarkerWidth < 0)
            {
                throw new ArgumentException($"{nameof(MarkerFactory)} received a negative marker width.\n");
            }
            markerScale = new Vector3(markerAttributes.MarkerWidth, markerAttributes.MarkerHeight, markerAttributes.MarkerWidth);
        }

        /// <summary>
        /// The strength factor of the emitted light for beam markers. It should be in the range [0,5].
        /// </summary>
        private const int emissionStrength = 1;

        /// <summary>
        /// The gap between the decorated game node and its marker in Unity world-space units.
        /// </summary>
        private const float gap = 0.001f;

        /// <summary>
        /// The world-space scale of the markers used to mark new, changed, and deleted
        /// objects from one version to the next one.
        /// </summary>
        private readonly Vector3 markerScale;

        /// <summary>
        /// The list of beam markers added for the new game objects since the last call to Clear().
        /// </summary>
        private readonly List<GameObject> beamMarkers = new();

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
        private static readonly int emissionStrengthProperty = Shader.PropertyToID("_EmissionStrength");

        /// <summary>
        /// A mapping of the materials used by the three factories onto their original color.
        /// This will be used to remember which materials have already been animated
        /// along with their original color, so that we can restore it.
        /// </summary>
        private readonly Dictionary<Material, Color> materials = new();

        /// <summary>
        /// The duration of an animation cycle in seconds.
        /// </summary>
        private const float animationDuration = 1.0f;

        /// <summary>
        /// The name of game objects representing a marker for dead nodes.
        /// </summary>
        private const string deadMarkerName = "DEAD_NODE";

        /// <summary>
        /// The name of game objects representing a marker for born nodes.
        /// </summary>
        private const string bornMarkerName = "BORN_NODE";

        /// <summary>
        /// The name of game objects representing a marker for changed nodes.
        /// </summary>
        private const string changeMarkerName = "CHANGED_NODE";

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as by putting a beam marker
        /// on top of its roof (including any of its children). The resulting
        /// game object representing the marker is tagged by <see cref="Tags.Decoration"/>.
        /// </summary>
        /// <param name="gameNode">node above which to add a beam marker</param>
        /// <param name="factory">factory to create the beam marker</param>
        /// <returns>the resulting beam marker</returns>
        private GameObject MarkByBeam(GameObject gameNode, NodeFactory factory)
        {
            GameObject beamMarker = NewBeam(factory);
            beamMarker.tag = Tags.Decoration;
            beamMarker.SetAbsoluteScale(markerScale);
            beamMarker.transform.SetParent(gameNode.transform);
            Portal.InheritPortal(from: gameNode, to: beamMarker);
            PutAbove(gameNode, beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Creates a new beam marker using the given <paramref name="factory"/>.
        /// This new game object will have the given <paramref name="renderQueueOffset"/>.
        /// Emissive light is added to it, where the emission strength is defined by
        /// <see cref="emissionStrength"/>.
        /// </summary>
        /// <param name="factory">the factory to create the beam marker</param>
        /// <param name="renderQueueOffset">offset in the render queue</param>
        /// <returns>new beam marker</returns>
        private GameObject NewBeam(NodeFactory factory)
        {
            GameObject result = factory.NewBlock();
            AddEmissionAndAnimation(result);
            return result;
        }

        /// <summary>
        /// If the sharedMaterial of the <paramref name="gameObject"/> has been adjusted already
        /// (i.e., is contained in <see cref="materials"/>), nothing happens. Otherwise
        /// <see cref="emissionStrength"/> and an animation is added to the shared material of
        /// <paramref name="gameObject"/> and the shared material is added to <see cref="materials"/>.
        ///
        /// Note: The sharedMaterial will be changed. That means all other objects
        /// having the same material will be affected, too.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a renderer whose
        /// material has a property <see cref="emissionStrengthProperty"/>.
        /// </summary>
        /// <param name="gameObject">the object whose shared material is receiving the emission
        /// strength and animation</param>
        private void AddEmissionAndAnimation(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out Renderer renderer) && !materials.ContainsKey(renderer.sharedMaterial))
            {
                materials[renderer.sharedMaterial] = renderer.sharedMaterial.color;
                renderer.sharedMaterial.SetFloat(emissionStrengthProperty, emissionStrength);
                // FIXME: Gradual fade does not work. The marker blinks in and out of existence.
                renderer.sharedMaterial.DOFade(0.0f, animationDuration).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            }
        }

        /// <summary>
        /// Puts <paramref name="beamMarker"/> above <paramref name="gameNode"/> and all
        /// its active children (with a little <see cref="gap"/>).
        /// </summary>
        /// <param name="gameNode">game node above which <paramref name="beamMarker"/> is to be put</param>
        /// <param name="beamMarker">marker for <paramref name="gameNode"/></param>
        private static void PutAbove(GameObject gameNode, GameObject beamMarker)
        {
            Vector3 position = gameNode.GetRoofCenter();
            position.y += gap + beamMarker.transform.lossyScale.y / 2;
            beamMarker.transform.position = position;
        }

        /// <summary>
        /// Adjusts the y position of the marker in <paramref name="gameNode"/>
        /// such that it is above <paramref name="gameNode"/> and all its children.
        /// </summary>
        /// <param name="gameNode">game node whose marker is to be adjusted</param>
        public void AdjustMarkerY(GameObject gameNode)
        {
            foreach (Transform child in gameNode.transform)
            {
                if (child.CompareTag(Tags.Decoration)
                    && child.name is changeMarkerName or bornMarkerName or deadMarkerName)
                {
                    GameObject marker = child.gameObject;
                    // The scale of gameNode may have changed, but we want our markers to
                    // have a world-space scale independent from its gameNode; hence,
                    // we may need to re-set the scale again.
                    marker.SetAbsoluteScale(markerScale);
                    PutAbove(gameNode, marker);
                    break;
                }
            }
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as dying by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// Its material will be animated, fading in and out.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkDead(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, deletionMarkerFactory);
            beamMarker.name = deadMarkerName;
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as coming into existence by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call. Its material will
        /// be animated, fading in and out.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkBorn(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, additionMarkerFactory);
            beamMarker.name = bornMarkerName;
            // We need to add the marker to beamMarkers so that it can be destroyed at the beginning of the
            // next animation cycle.
            beamMarkers.Add(beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as being changed by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.  Its material will
        /// be animated, fading in and out.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkChanged(GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(gameNode, changeMarkerFactory);
            beamMarker.name = changeMarkerName;
            // We need to add beam marker to beamMarkers so that it can be destroyed at the beginning of the
            // next animation cycle.
            beamMarkers.Add(beamMarker);
            return beamMarker;
        }

        /// <summary>
        /// Destroys all cached markers created since the last call to Clear(). Clears the
        /// cache of markers.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject gameObject in beamMarkers)
            {
                if (gameObject != null)
                {
                    gameObject.transform.SetParent(null);
                    Destroyer.Destroy(gameObject);
                }
            }
            beamMarkers.Clear();
            // We reset all marker materials to their original value.
            // They need to be reset because the animation is still running and thus
            // modifying the color of the materials while new blocks are being created
            // receiving these materials interfere. I don't know the exact implementation details
            // of the animation and, hence, have no clear explanation, but I observed
            // that the materials tended to fade more and more, never reaching their
            // original value again.
            foreach (var entry in materials)
            {
                Material material = entry.Key;
                material.color = entry.Value;
            }
        }
    }
}
