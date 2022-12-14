using UnityEngine;
using SEE.GO;
using SEE.Utils;
using System;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A laser pointer drawing a line of a fixed length from a source that can
    /// be defined by clients.
    /// </summary>
    public class LaserPointer : MonoBehaviour
    {
        /// <summary>
        /// Maximal length of laser beam.
        /// </summary>
        [Tooltip("Maximal length of laser beam.")]
        public float LaserLength = 2.0f;

        /// <summary>
        /// The width of the laser beam.
        /// </summary>
        [Tooltip("Width of laser beam.")]
        public float LaserWidth = 0.005f;

        /// <summary>
        /// Color of the laser beam when it hits anything.
        /// </summary>
        [Tooltip("Color of the laser beam when it hits anything.")]
        public Color HitColor = Color.green;

        /// <summary>
        /// Color of the laser beam when it does not hit anything.
        /// </summary>
        [Tooltip("Color of the laser beam when it does not hit anything.")]
        public Color MissColor = Color.red;

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
        /// As to whether the leaser beam is turned on.
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
            laserMaterial = Materials.New(Materials.ShaderType.PortalFree, MissColor);
            GameObject laserBeam = new GameObject
            {
                name = "Laser " + Guid.NewGuid().ToString()
            };
            laserBeam.transform.SetParent(gameObject.transform);
            laserLine = LineFactory.Draw(laserBeam, from: Vector3.zero, to: Vector3.zero, width: LaserWidth, laserMaterial);
            laserLine.enabled = true;
        }

        /// <summary>
        /// Draws a line from <see cref="Source"/> to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">the end of the line</param>
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
        /// line is <see cref="LaserLength"/> units away from the pointing device's
        /// origin (again into the direction the pointing device is pointing to).
        /// </summary>
        /// <returns>the end of the line drawn (i.e., the point where it hit
        /// anything or the end point of the length-restricted beam, respectively</returns>
        /// <remarks>This method is intended for local interaction of the local player.</remarks>
        internal Vector3 Point()
        {
            Vector3 result;
            Color color;
            if (Raycasting.RaycastAnything(out RaycastHit raycastHit, LaserLength))
            {
                result = raycastHit.point;
                color = HitColor;
            }
            else
            {
                Ray ray = Raycasting.UserPointsTo();
                result = ray.origin + ray.direction * LaserLength;
                color = MissColor;
            }
            laserMaterial.color = color;
            Draw(result);
            return result;
        }

        /// <summary>
        /// The laser beam is to be directed towards given <paramref name="direction"/>.
        /// </summary>
        /// <param name="direction">requested direction of the laser beam</param>
        /// <returns>the position of the tip of the laser beam</returns>
        internal Vector3 PointTowards(Vector3 direction)
        {
            Vector3 target = Source.position + direction * LaserLength;
            Draw(target);
            return target;
        }
    }
}
