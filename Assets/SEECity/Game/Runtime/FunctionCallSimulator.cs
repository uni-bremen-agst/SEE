using System;
using UnityEngine;

namespace SEE.Game.Runtime
{

    /// <summary>
    /// Simulates a function call via letting spheres fly from <see cref="source"/> to
    /// <see cref="target"/>.
    /// </summary>
    public class FunctionCallSimulator : MonoBehaviour
    {
        /// <summary>
        /// The size (scale) of a sphere.
        /// </summary>
        private const float SPHERE_SCALE = 0.3f;

        /// <summary>
        /// The desired distance between each of the spheres.
        /// </summary>
        private const float SPHERE_OPTIMAL_DISTANCE = 0.8f;

        /// <summary>
        /// The horizontal speed of a sphere.
        /// </summary>
        private const float SPHERE_HORIZONTAL_SPEED = 1.6f;

        /// <summary>
        /// The maximum height of a sphere. Is reached right between <see cref="source"/>
        /// and <see cref="target"/>.
        /// </summary>
        private const float SPHERE_MAX_ALTITUDE = 1.0f;

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
        private GameObject source;

        /// <summary>
        /// The target of the function call.
        /// </summary>
        private GameObject target;

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
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void Initialize(GameObject source, GameObject target, float t = 0.0f)
        {
            this.source = source ?? throw new ArgumentException("'source' must not be null!");
            this.target = target ?? throw new ArgumentException("'target' must not be null!");

            Vector3 sourcePosition = source.transform.position;
            Vector3 targetPosition = target.transform.position;
            float sourceToTargetLength = Vector3.Distance(sourcePosition, targetPosition);
            int sphereCount = 1 + (int)(sourceToTargetLength / SPHERE_OPTIMAL_DISTANCE);
            sphereActualDistance = sourceToTargetLength / sphereCount;
            spheres = new GameObject[sphereCount];
            for (int i = 0; i < spheres.Length; i++)
            {
                spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spheres[i].transform.position = Vector3.Lerp(sourcePosition, targetPosition, (float)i / (float)spheres.Length);
                spheres[i].transform.rotation = Quaternion.identity;
                spheres[i].transform.localScale = new Vector3(SPHERE_SCALE, SPHERE_SCALE, SPHERE_SCALE);
                spheres[i].transform.parent = transform;
            }
            sourceOriginalScale = source.transform.localScale;
            targetOriginalScale = target.transform.localScale;
            sourceOriginalColor = source.GetComponentInChildren<MeshRenderer>().material.color;
            targetOriginalColor = target.GetComponentInChildren<MeshRenderer>().material.color;
        }
        
        /// <summary>
        /// Resets the source's and target's scale and color and destroys the object.
        /// </summary>
        public void Shutdown()
        {
            source.transform.localScale = sourceOriginalScale;
            target.transform.localScale = targetOriginalScale;
            source.GetComponentInChildren<MeshRenderer>().material.color = sourceOriginalColor;
            target.GetComponentInChildren<MeshRenderer>().material.color = targetOriginalColor;
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
            source.transform.localScale = sourceOriginalScale + new Vector3(scale, scale, scale);
            target.transform.localScale = targetOriginalScale + new Vector3(scale, scale, scale);

            // color
            int colorIndex = (int)(viridisColorPalette.Length * (0.5f + 0.5f * Mathf.Cos(2.0f * Mathf.PI * t + Mathf.PI))) % viridisColorPalette.Length;
            Color color = viridisColorPalette[colorIndex];
            source.GetComponentInChildren<MeshRenderer>().material.color = color;
            target.GetComponentInChildren<MeshRenderer>().material.color = color;
        }

        /// <summary>
        /// Updates the position and color of the spheres.
        /// </summary>
        private void UpdateSpheres()
        {
            Vector3 sourcePosition = source.transform.position;
            Vector3 targetPosition = target.transform.position;
            float sourceToTargetLength = Vector3.Distance(sourcePosition, targetPosition);
            if (sourceToTargetLength < float.Epsilon) // This is necessary for recursive function calls.
            {
                const float zeroOffset = 0.1f;
                sourcePosition += new Vector3(zeroOffset, 0.0f, 0.0f);
                targetPosition += new Vector3(-zeroOffset, 0.0f, 0.0f);
                sourceToTargetLength = Vector3.Distance(sourcePosition, targetPosition);
            }

            // Translate the first sphere.
            Vector3 direction = (targetPosition - sourcePosition).normalized;
            Vector3 step = direction * SPHERE_HORIZONTAL_SPEED * Time.deltaTime * Mathf.Sqrt(sourceToTargetLength) * 1f;
            spheres[0].transform.position = new Vector3(spheres[0].transform.position.x, targetPosition.y, spheres[0].transform.position.z);
            Vector3 fstSpherePositionFlatToTarget = targetPosition - spheres[0].transform.position;
            float fstSpherePositionFlatToTargetLength = fstSpherePositionFlatToTarget.magnitude;
            if (step.magnitude > fstSpherePositionFlatToTargetLength)
            {
                step -= fstSpherePositionFlatToTarget;
                spheres[0].transform.position = sourcePosition;
            }
            spheres[0].transform.position += step;
            fstSpherePositionFlatToTarget = targetPosition - spheres[0].transform.position;
            fstSpherePositionFlatToTargetLength = fstSpherePositionFlatToTarget.magnitude;

            // Align remaining spheres.
            Vector3 sphereOffset = direction * sphereActualDistance;
            for (int i = 1; i < spheres.Length; i++)
            {
                if (sphereActualDistance > Vector3.Distance(spheres[i - 1].transform.position, targetPosition))
                {
                    Vector3 fromSourceOffset = sphereOffset - (targetPosition - spheres[i - 1].transform.position);
                    spheres[i].transform.position = sourcePosition + fromSourceOffset;
                }
                else
                {
                    spheres[i].transform.position = spheres[i - 1].transform.position + sphereOffset;
                }
            }

            // Adjust colors and heights.
            for (int i = 0; i < spheres.Length; i++)
            {
                float sphereToTargetLength = Vector3.Distance(spheres[i].transform.position, targetPosition);
                float t = 1.0f - (sphereToTargetLength / sourceToTargetLength);

                // color
                Color color = viridisColorPalette[(int)(t * viridisColorPalette.Length) % viridisColorPalette.Length];
                spheres[i].GetComponentInChildren<MeshRenderer>().material.color = color;

                // height
                float height = SPHERE_MAX_ALTITUDE * Mathf.Sin(t * Mathf.PI);
                if (height == float.NaN)
                {
                    Debug.Log("");
                }
                spheres[i].transform.position = new Vector3(spheres[i].transform.position.x, height, spheres[i].transform.position.z);
            }
        }
    }

}
