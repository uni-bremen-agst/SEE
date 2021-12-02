using SEE.DataModel;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game.Charts;
using SEE.Game.City;
using SEE.DataModel.DG;

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
        /// <param name="graphRenderer">renderer used to retrieve the roof position of the game objects to be marked</param>
        /// <param name="markerWidth">the width (x and z lengths) of the markers</param>
        /// <param name="markerHeight">the height (y length) of the markers</param>
        /// <param name="additionColor">the color for the markers for added nodes</param>
        /// <param name="changeColor">the color for the markers for changed nodes</param>
        /// <param name="deletionColor">the color for the markers for deleted nodes</param>
        /// <param name="duration">the duration until the final height of the markers must be reached</param>
        public Marker(GraphRenderer graphRenderer, MarkerAttributes markerAttributes, float markerWidth, float markerHeight,
                      Color additionColor, Color changeColor, Color deletionColor, float duration)
        {
            this.graphRenderer = graphRenderer;
            this.markerAttributes = markerAttributes;
            this.duration = duration;

            // graphRenderer.ShaderType
            additionMarkerFactory = new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(additionColor, additionColor, 1));
            changeMarkerFactory = new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(changeColor, changeColor, 1));
            deletionMarkerFactory = new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(deletionColor, deletionColor, 1));

            if (markerHeight < 0)
            {
                this.markerHeight = 0;
                Debug.LogError("SEE.Game.Evolution.Marker received a negative marker height.\n");
            }
            else
            {
                this.markerHeight = markerHeight;
            }
            if (markerWidth < 0)
            {
                this.markerWidth = 0;
                Debug.LogError("SEE.Game.Evolution.Marker received a negative marker width.\n");
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
        /// The time in seconds until the markers must have reached their final height.
        /// </summary>
        private float duration;

        /// <summary>
        /// Set the time in seconds until the markers must have reached their final height
        /// to given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">duration of animation</param>
        public void SetDuration(float value)
        {
            duration = value;
        }

        /// <summary>
        /// The height of the beam markers used to mark new, changed, and deleted
        /// objects from one version to the next one.
        /// </summary>
        private readonly MarkerAttributes markerAttributes;

        /// <summary>
        /// The height of the beam markers used to mark new, changed, and deleted
        /// objects from one version to the next one.
        /// </summary>
        private float markerHeight;

        /// <summary>
        /// The width (x and z lengths) of the beam markers used to mark new, changed,
        /// and deleted objects from one version
        /// to the next one.
        /// </summary>
        private readonly float markerWidth;

        /// <summary>
        /// The renderer used to retrieve the roof position of the game objects to be marked.
        /// </summary>
        private readonly GraphRenderer graphRenderer;

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
        /// Transformation method to scale numbers.
        /// </summary>
        private float transform(float input)
        {
            return input / 100;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as dying/getting alive by putting a beam marker on top
        /// of its roof.
        /// </summary>
        /// <param name="node">node above which to add a beam marker</param>
        /// <param name="gameNode">node above which to add a beam marker</param>
        /// <param name="factory">node above which to add a beam marker</param>
        /// <returns>the resulting beam marker</returns>
        private GameObject MarkByBeam(Node node, GameObject gameNode, CylinderFactory factory)
        {
            Vector3 position = graphRenderer.GetRoof(gameNode);
            Vector3 beamScale = new Vector3(markerWidth, 0, markerWidth);

            if (markerAttributes.Kind == MarkerKinds.Dynamic)
            {
                float lengthMetric = 0; 
                node.TryGetNumeric(markerAttributes.LengthMetric, out lengthMetric);
                float totalHeight = transform(lengthMetric);

                // Offset from bottom against overlapping beams.
                float offset = 0;

                // New game object as parent for multiple beams.
                GameObject output = new GameObject();
                foreach (MarkerSection section in markerAttributes.MarkerSections)
                {
                    float sectionMetric = 0;
                    node.TryGetNumeric(section.Metric, out sectionMetric);
                    float sectionHeight = transform(sectionMetric);

                    CylinderFactory customFactory = new CylinderFactory(Materials.ShaderType.Opaque, new ColorRange(section.Color, section.Color, 1));

                    // The marker should be drawn in front of the block, hence, its render
                    // queue offset must be greater than the one of the block.
                    //GameObject beamMarker = NewBeam(factory, gameNode.GetRenderQueue() + 1);
                    GameObject beamMarker = NewBeam(customFactory, 0);

                    // FIXME: These kinds of beam markers make sense only for leaf nodes.
                    // Could we better use some kind of blinking now that the cities
                    // are drawn in miniature?

                    beamMarker.tag = Tags.Decoration;

                    Vector3 localBeamScale = new Vector3(markerWidth, sectionHeight, markerWidth);

                    beamMarker.transform.position = new Vector3(0, offset, 0);
                    beamMarker.transform.localScale = localBeamScale;

                    // Makes beamMarker a child of block so that it moves along with it during the animation.
                    // In addition, it will also be destroyed along with its parent block.
                    beamMarker.transform.SetParent(output.transform);

                    offset += sectionHeight;
                }

                output.transform.position = position;
                output.transform.localScale = new Vector3(1, 0, 1);
                output.transform.SetParent(gameNode.transform);

                BeamRaiser raiser = output.AddComponent<BeamRaiser>();
                raiser.SetTargetHeightAndDuration(new Vector3(1, 1, 1), duration: duration);
                return output;
            } 
            else
            {
                // The marker should be drawn in front of the block, hence, its render
                // queue offset must be greater than the one of the block.
                //GameObject beamMarker = NewBeam(factory, gameNode.GetRenderQueue() + 1);
                GameObject beamMarker = NewBeam(factory, 0);

                // FIXME: These kinds of beam markers make sense only for leaf nodes.
                // Could we better use some kind of blinking now that the cities
                // are drawn in miniature?

                beamMarker.tag = Tags.Decoration;

                position.y += beamScale.y / 2.0f;
                beamMarker.transform.position = position;
                beamMarker.transform.localScale = beamScale;

                // Makes beamMarker a child of block so that it moves along with it during the animation.
                // In addition, it will also be destroyed along with its parent block.
                beamMarker.transform.SetParent(gameNode.transform);
                BeamRaiser raiser = beamMarker.AddComponent<BeamRaiser>();
                raiser.SetTargetHeightAndDuration(new Vector3(markerWidth, markerHeight, markerWidth), duration: duration);
                return beamMarker;
            }
        }

        /// <summary>
        /// Creates a new beam marker using the given <paramref name="factory"/>.
        /// This new game object will have the given <paramref name="renderQueueOffset"/>.
        /// Emissive light is added to it. Its strength is defined by <see cref="EmissionStrength"/>.
        /// </summary>
        /// <param name="factory">the factory to create the beam marker</param>
        /// <param name="renderQueueOffset">offset in the render queue</param>
        /// <returns>new beam marker</returns>
        private static GameObject NewBeam(CylinderFactory factory, int renderQueueOffset)
        {
            GameObject beamMarker = factory.NewBlock(0, renderQueueOffset);
            AddEmission(beamMarker);
            //Portal.SetPortal(beamMarker.transform.parent.gameObject);
            // Add the added node id to the revision changes list
            NodeChangesBuffer.GetSingleton().addedNodeIDs.Add(beamMarker.name);
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
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                // Set power beam material to emissive
                renderer.sharedMaterial.SetFloat("_EmissionStrength", EmissionStrength);
            }
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as dying by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// </summary>
        /// <param name="node">node to extract metric data from</param>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkDead(Node node, GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(node, gameNode, deletionMarkerFactory);
            beamMarker.name = "dead " + gameNode.name;
            beamMarker.transform.SetParent(gameNode.transform);
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as coming into existence by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="node">node to extract metric data from</param>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkBorn(Node node, GameObject gameNode)
        {
            NodeChangesBuffer.GetSingleton().addedNodeIDs.Add(gameNode.gameObject.name);
            GameObject beamMarker = MarkByBeam(node, gameNode, additionMarkerFactory);
            beamMarker.name = "new " + gameNode.name;
            // We need to add the marker to beamMarkers so that it can be destroyed at the beginning of the
            // next animation cycle.
            beamMarkers.Add(beamMarker);
            beamMarker.transform.SetParent(gameNode.transform);
            return beamMarker;
        }

        /// <summary>
        /// Marks the given <paramref name="gameNode"/> as being changed by putting a beam marker on top
        /// of its roof. The color of that beam was specified through the constructor call.
        /// Adds the created beam marker to the cache.
        /// </summary>
        /// <param name="node">node to extract metric data from</param>
        /// <param name="gameNode">game node to be marked</param>
        /// <returns>the resulting beam marker</returns>
        public GameObject MarkChanged(Node node, GameObject gameNode)
        {
            GameObject beamMarker = MarkByBeam(node, gameNode, changeMarkerFactory);
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
                Object.Destroy(gameObject);
            }
            beamMarkers.Clear();
        }

        /// <summary>
        /// The behaviour to grow a beam until it reaches its target height over
        /// multiple frames. This component is assumed to be attached to a
        /// marker. The width and depth of all markers will be maintained to WidthDepth.
        /// </summary>
        private class BeamRaiser : MonoBehaviour
        {
            /// <summary>
            /// The uniform width (x) and depth (z) of all markers. The height should
            /// always be zero as this is the parameter we want to grow during the animation.
            /// </summary>
            private Vector3 targetScale = Vector3.zero;

            /// <summary>
            /// The initial height of the beam marker. Will be set in Start() as
            /// the initial height of the game object.
            /// </summary>
            private float initialHeight = 0;

            /// <summary>
            /// Sets the target scale of the beam marker that should be reached eventually
            /// and the time in seconds until this scale is reached.
            /// </summary>
            /// <param name="targetScale">the final scale to be reached</param>
            /// <param name="duration">the duration in seconds until the requested scale is reached</param>
            public void SetTargetHeightAndDuration(Vector3 targetScale, float duration)
            {
                this.targetScale = targetScale;
                this.duration = duration;
            }

            /// <summary>
            /// The time in seconds since this BeamRaiser was started.
            /// </summary>
            private float timeSinceStart = 0;

            /// <summary>
            /// The duration in seconds until the requested scale must be reached.
            /// </summary>
            private float duration;

            /// <summary>
            /// Sets <see cref="initialHeight"/> to the initial height of the game object
            /// that we are to grow.
            /// </summary>
            private void Start()
            {
                initialHeight = transform.lossyScale.y;
            }

            /// <summary>
            /// At ever frame cycle, gameObject's height will be increased continuously
            /// until it reaches its desired height. After that nothing is done anymore.
            /// </summary>
            private void Update()
            {
                if (transform.lossyScale != targetScale)
                {
                    // Resize gameObject independent of parent so that the scale relates to
                    // world space. This can be done by first unparenting the gameObject.
                    Transform parent = transform.parent;
                    transform.parent = null;

                    timeSinceStart += Time.deltaTime;
                    // The relative time spent so far in relation to the requested duration.
                    float relativeTime = Mathf.Min(timeSinceStart / duration, 1.0f);

                    // The height is further increased as a linear interpolation
                    // between the initial and final height relative to the time so far.
                    Vector3 newScale = targetScale;
                    newScale.y = Mathf.Lerp(initialHeight, targetScale.y, relativeTime);
                    transform.localScale = newScale;

                    transform.SetParent(parent);
                }
            }
        }
    }
}