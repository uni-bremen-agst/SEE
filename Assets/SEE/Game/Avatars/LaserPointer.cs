using UnityEngine;
using SEE.GO;
using SEE.Utils;
using System;
using SEE.Controls;
using SEE.GO.Factories;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A laser pointer drawing a line of a fixed length from a source that can
    /// be defined by clients.
    /// </summary>
    public class LaserPointer : MonoBehaviour
    {
        /// <summary>
        /// The width of the laser beam.
        /// </summary>
        [Tooltip("Width of laser beam.")]
        public float LaserWidth = 0.005f;

        /// <summary>
        /// Color of the laser beam when it hits anything.
        /// </summary>
        [Tooltip("Color of the laser beam when it hits anything.")]
        public static Color HitColor = Color.green;

        /// <summary>
        /// Color of the laser beam when it does not hit anything.
        /// </summary>
        [Tooltip("Color of the laser beam when it does not hit anything.")]
        public static Color MissColor = Color.red;

        /// <summary>
        /// The last point where the laser beam hit something.
        /// </summary>
        public Vector3 LastHit;

        /// <summary>
        /// The origin of the laser beam.
        /// </summary>
        public Transform Source;

        /// <summary>
        /// The material of the laser beam. Will be used to change its
        /// color depending upon whether it hits anything or not.
        /// </summary>
        private Material laserMaterial;

        /// <summary>
        /// The line renderer that draws the laser beam as a line.
        /// </summary>
        private LineRenderer laserLine;

        /// <summary>
        /// Whether the leaser beam is turned on.
        /// </summary>
        public bool On
        {
            get => laserLine.enabled;
            set => laserLine.enabled = value;
        }

        /// <summary>
        /// An empty game object representing the laser beam is created and added to
        /// <see cref="gameObject"/>. A line renderer is added and the material for
        /// the line representing the laser beam is set up. The laser beam is disabled
        /// initially.
        /// </summary>
        private void Awake()
        {
            laserMaterial = MaterialsFactory.New(MaterialsFactory.ShaderType.PortalFree, Color.white);
            GameObject laserBeam = new()
            {
                name = $"Laser {Guid.NewGuid()}"
            };
            laserBeam.transform.SetParent(gameObject.transform);
            laserLine = LineFactory.Draw(laserBeam, from: Vector3.zero, to: Vector3.zero, width: LaserWidth, laserMaterial);
            laserLine.enabled = true;
        }

        /// <summary>
        /// Draws a line from <see cref="Source"/> to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The end of the line.</param>
        /// <remarks>This method is intended for replicating a pointing gesture
        /// of a remote player for its local representative (avatar).</remarks>
        internal void Draw(Vector3 position)
        {
            if (laserLine != null && Source != null)
            {
                LineFactory.ReDraw(laserLine, from: Source.transform.position, to: position);
            }
        }

        /// <summary>
        /// Draws a line from <see cref="Source"/> to a calculated end point.
        /// The end point will be the hit point using a ray cast from the
        /// pointing device of the user towards the direction in which this
        /// pointing device is directing. If nothing is hit, the end of the drawn
        /// line is <see cref="Raycasting.InteractionRadius"/> units away from the pointing
        /// device's origin (again into the direction the pointing device is pointing to).
        /// </summary>
        /// <returns>The end of the line drawn, that is, the point where it hit
        /// anything or the end point of the length-restricted beam, respectively.</returns>
        /// <remarks>This method is intended for local interaction of the local player.</remarks>
        internal Vector3 Point()
        {
            Vector3 result;
            Color color;
            if (Raycasting.RaycastInteractableObjectBase(out RaycastHit raycastHit, out InteractableObjectBase io, false)
                && (io.IsInteractable(raycastHit.point)
                    || raycastHit.collider.gameObject.CompareTag(Tags.Drawable)))
            {
                result = LastHit = raycastHit.point;
                color = io.HitColor != null ? io.HitColor.Value : HitColor;
            }
            else
            {
                Ray ray = Raycasting.UserPointsTo();
                result = ray.origin + ray.direction * Raycasting.InteractionRadius;
                color = MissColor;
            }
            laserLine.startColor = laserLine.endColor = color;
            Draw(result);
            return result;
        }

        /// <summary>
        /// The laser beam is to be directed towards given <paramref name="direction"/>.
        /// </summary>
        /// <param name="direction">Requested direction of the laser beam in world space.</param>
        /// <returns>The position of the tip of the laser beam in world space.</returns>
        internal Vector3 PointTowards(Vector3 direction)
        {
            Vector3 target = Source.position + direction * Raycasting.InteractionRadius;
            Draw(target);
            return target;
        }
    }
}
