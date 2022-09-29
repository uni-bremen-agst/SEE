using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Runtime
{

    /// <summary>
    /// Simulates a function call via letting spheres fly from <see cref="src"/> to
    /// <see cref="dst"/>.
    /// </summary>
    public class FunctionCallSimulator : SerializedMonoBehaviour
    {
        /// <summary>
        /// The size (scale) of a sphere.
        /// </summary>
        private const float SPHERE_SCALE = 0.01f;

        /// <summary>
        /// The desired distance between each of the spheres.
        /// </summary>
        private const float SPHERE_OPTIMAL_DISTANCE = SPHERE_SCALE;

        /// <summary>
        /// The horizontal speed of a sphere.
        /// </summary>
        private const float SPHERE_HORIZONTAL_SPEED = SPHERE_SCALE * 2;

        /// <summary>
        /// The maximum height of a sphere. Is reached right between <see cref="src"/>
        /// and <see cref="dst"/>.
        /// </summary>
        private const float SPHERE_MAX_ALTITUDE = 0.3f;

        /// <summary>
        /// The maximum added size to the scale of a building.
        /// </summary>
        private const float BLOCK_SCALE_ENLARGEMENT = 0.3f;

        /// <summary>
        /// The color pallette for coloring buildings and spheres. See
        /// <see cref="SEE.Layout.DonutFactory.GetPalette"/> for further documentation.
        /// </summary>
        private readonly Color[] viridisColorPalette = new Color[] {
            new Color(0.267f, 0.004f, 0.333f, 1.0f),
            new Color(0.275f, 0.125f, 0.024f, 1.0f),
            new Color(0.259f, 0.235f, 0.506f, 1.0f),
            new Color(0.161f, 0.337f, 0.545f, 1.0f),
            new Color(0.176f, 0.431f, 0.557f, 1.0f),
            new Color(0.145f, 0.522f, 0.553f, 1.0f),
            new Color(0.137f, 0.604f, 0.537f, 1.0f),
            new Color(0.161f, 0.682f, 0.502f, 1.0f),
            new Color(0.325f, 0.769f, 0.408f, 1.0f),
            new Color(0.522f, 0.827f, 0.286f, 1.0f),
            new Color(0.741f, 0.875f, 0.184f, 1.0f),
            new Color(0.992f, 0.906f, 0.145f, 1.0f)
        };

        /// <summary>
        /// The source of the function call.
        /// </summary>
        internal GameObject src;

        /// <summary>
        /// The destination of the function call.
        /// </summary>
        internal GameObject dst;

        /// <summary>
        /// The original scale of the source building.
        /// </summary>
        private Vector3 sourceOriginalScale;

        /// <summary>
        /// The original scale of the target building.
        /// </summary>
        private Vector3 targetOriginalScale;

        /// <summary>
        /// The original color of the source building.
        /// </summary>
        private Color sourceOriginalColor;

        /// <summary>
        /// The original color of the target building.
        /// </summary>
        private Color targetOriginalColor;

        /// <summary>
        /// The spheres.
        /// </summary>
        private GameObject[] spheres;

        /// <summary>
        /// The actual distance of the spheres. Optimally very close to
        /// <see cref="SPHERE_OPTIMAL_DISTANCE"/>.
        /// </summary>
        private float sphereActualDistance;

        /// <summary>
        /// Initializes the simulator. Sets source and target as given arguments.
        /// </summary>
        /// <param name="src">The source of the function call.</param>
        /// <param name="dst">The destination of the function call.</param>
        public void Initialize(GameObject src, GameObject dst, float t = 0.0f)
        {
            Assert.IsNotNull(src, "'src' must not be null!");
            Assert.IsNotNull(dst, "'dst' must not be null!");
            Assert.IsTrue(t >= 0.0f && t <= 1.0f);

            this.src = src;
            this.dst = dst;

            Vector3 sourcePosition = src.transform.position;
            Vector3 targetPosition = dst.transform.position;
            float sourceToTargetLength = Vector3.Distance(sourcePosition, targetPosition);
            int sphereCount = 1 + (int)(sourceToTargetLength / SPHERE_OPTIMAL_DISTANCE);
            sphereActualDistance = sourceToTargetLength / sphereCount;
            spheres = new GameObject[sphereCount];
            for (int i = 0; i < spheres.Length; i++)
            {
                spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spheres[i].transform.position = Vector3.Lerp(sourcePosition, targetPosition, (float)i / (float)spheres.Length);
                spheres[i].transform.position = Vector3.Lerp(sourcePosition, targetPosition, i / (float)spheres.Length);
                spheres[i].transform.rotation = Quaternion.identity;
                spheres[i].transform.localScale = new Vector3(SPHERE_SCALE, SPHERE_SCALE, SPHERE_SCALE);
                spheres[i].transform.parent = transform;
            }

            sourceOriginalScale = src.transform.localScale;
            targetOriginalScale = dst.transform.localScale;
            sourceOriginalColor = src.GetComponentInChildren<MeshRenderer>().material.color;
            targetOriginalColor = dst.GetComponentInChildren<MeshRenderer>().material.color;
        }

        /// <summary>
        /// Resets the source's and target's scale and color and destroys the object.
        /// </summary>
        public void Shutdown()
        {
            src.transform.localScale = sourceOriginalScale;
            dst.transform.localScale = targetOriginalScale;
            src.GetComponentInChildren<MeshRenderer>().material.color = sourceOriginalColor;
            dst.GetComponentInChildren<MeshRenderer>().material.color = targetOriginalColor;
            for (int i = 0; i < spheres.Length; i++)
            {
                Destroy(spheres[i]);
            }
        }

        /// <summary>
        /// Updates the simulation.
        /// </summary>
        /// <param name="t">The position of the animated loop between 0 and 1.</param>
        public void UpdateSimulation(float t)
        {
            Assert.IsTrue(t >= 0.0f && t <= 1.0f);

            UpdateBlocks(t);
            UpdateSpheres();
        }

        /// <summary>
        /// Updates the scale and color of the buildings.
        /// </summary>
        /// <param name="t">The position of the animated loop between 0 and 1.</param>
        private void UpdateBlocks(float t)
        {
            // scale
            float halfBlockScaleEnlargement = 0.5f * BLOCK_SCALE_ENLARGEMENT;
            float scale = halfBlockScaleEnlargement + halfBlockScaleEnlargement * Mathf.Cos(2.0f * Mathf.PI * t + Mathf.PI);
            src.transform.localScale = sourceOriginalScale + new Vector3(scale, scale, scale);
            dst.transform.localScale = targetOriginalScale + new Vector3(scale, scale, scale);

            // color
            int colorIndex = (int)(viridisColorPalette.Length * (0.5f + 0.5f * Mathf.Cos(2.0f * Mathf.PI * t + Mathf.PI))) % viridisColorPalette.Length;
            Color color = viridisColorPalette[colorIndex];
            src.GetComponentInChildren<MeshRenderer>().material.color = color;
            dst.GetComponentInChildren<MeshRenderer>().material.color = color;
        }

        /// <summary>
        /// Updates the position and color of the spheres.
        /// </summary>
        public void UpdateSpheres()
        {
            Vector3 srcPos = src.transform.position;
            Vector3 dstPos = dst.transform.position;
            Vector2 srcPosFlat = new Vector2(srcPos.x, srcPos.z);
            Vector2 dstPosFlat = new Vector2(dstPos.x, dstPos.z);

            Vector2 srcToDstFlat = dstPosFlat - srcPosFlat;
            float srcToDstDistFlat = srcToDstFlat.magnitude;
            if (srcToDstDistFlat < float.Epsilon) // Necessary for recursive function calls
            {
                const float zeroOffset = 0.1f;
                srcPos += new Vector3(zeroOffset, 0.0f, 0.0f);
                dstPos += new Vector3(-zeroOffset, 0.0f, 0.0f);
                srcPosFlat += new Vector2(zeroOffset, 0.0f);
                dstPosFlat += new Vector2(-zeroOffset, 0.0f);
                srcToDstFlat = dstPosFlat - srcPosFlat;
                srcToDstDistFlat = srcToDstFlat.magnitude;
            }
            Vector2 flyDirFlat = srcToDstFlat.normalized;

            // Translate first sphere
            Vector2 stepFlat = flyDirFlat * SPHERE_HORIZONTAL_SPEED * Time.deltaTime * Mathf.Sqrt(srcToDstDistFlat);
            Vector2 fstPosFlat = new Vector2(spheres[0].transform.position.x, spheres[0].transform.position.z) + stepFlat;

            float fstToDstDistFlat = Vector2.Distance(fstPosFlat, dstPosFlat);
            float fstToSrcDistFlat = Vector2.Distance(fstPosFlat, srcPosFlat);
            if (fstToSrcDistFlat > srcToDstDistFlat)
            {
                fstPosFlat -= srcToDstFlat;
            }
            spheres[0].transform.position = new Vector3(fstPosFlat.x, 0f, fstPosFlat.y);

            // Align remaining spheres
            Vector2 sphereOffsetFlat = flyDirFlat * sphereActualDistance;
            bool pastDst = false;
            for (int i = 1; i < spheres.Length; i++)
            {
                Vector3 spherePos = spheres[i].transform.position;
                Vector2 spherePosFlat = new Vector2(spherePos.x, spherePos.z);

                spherePosFlat = fstPosFlat + i * sphereOffsetFlat;

                if (!pastDst)
                {
                    float sphereToSrcDstFlat = Vector2.Distance(spherePosFlat, srcPosFlat);
                    pastDst = sphereToSrcDstFlat > srcToDstDistFlat;
                }

                if (pastDst)
                {
                    spherePosFlat -= srcToDstFlat;
                }
                spheres[i].transform.position = new Vector3(spherePosFlat.x, 0f, spherePosFlat.y);
            }

            // Adjust colors and heights
            for (int i = 0; i < spheres.Length; i++)
            {
                Vector3 spherePos = spheres[i].transform.position;
                Vector2 spherePosFlat = new Vector2(spherePos.x, spherePos.z);

                float sphereToDstDistFlat = Vector3.Distance(spherePosFlat, dstPosFlat);
                float t = 1.0f - (sphereToDstDistFlat / srcToDstDistFlat);

                // Color
                Color color = viridisColorPalette[(int)(t * viridisColorPalette.Length) % viridisColorPalette.Length];
                spheres[i].GetComponentInChildren<MeshRenderer>().material.color = color;

                // Altitude
                float srcRoofHeight = src.transform.position.y + src.transform.lossyScale.y / 2.0f;
                float dstRoofHeight = dst.transform.position.y + dst.transform.lossyScale.y / 2.0f;
                Vector3 srcPoint = src.transform.position;
                srcPoint.y = srcRoofHeight;
                Vector3 dstPoint = dst.transform.position;
                dstPoint.y = dstRoofHeight;
                float altitude = Vector3.Lerp(srcPoint, dstPoint, t).y;
                altitude = altitude + SPHERE_MAX_ALTITUDE * Mathf.Sin(t * Mathf.PI);
                spheres[i].transform.position = new Vector3(spherePosFlat.x, altitude, spherePosFlat.y);
            }
        }
    }

}
