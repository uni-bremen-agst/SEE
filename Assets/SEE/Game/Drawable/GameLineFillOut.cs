using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides functionality for creating, updating, and managing fill-out
    /// objects of drawable lines and line-based shapes.
    /// </summary>
    public static class GameLineFillOut
    {
        /// <summary>
        /// Creates or updates the fill-out object for a line-based shape.
        /// The fill-out is only possible for objects tagged as
        /// <see cref="Tags.Line"/> or <see cref="Tags.LineCap"/> that contain
        /// more than two different positions.
        /// If a fill-out object already exists, its color and mesh are updated.
        /// Otherwise, a new fill-out object is created as a child of the given shape.
        /// </summary>
        /// <param name="shape">
        /// The line-based shape whose interior should be filled.
        /// Must be tagged as <see cref="Tags.Line"/> or <see cref="Tags.LineCap"/>.
        /// </param>
        /// <param name="color">
        /// The fill color to use. If null, the current shape color is used.
        /// </param>
        /// <param name="showInfo">
        /// Whether an info notification should be shown if the fill-out cannot be created.
        /// </param>
        /// <returns>
        /// True if the fill-out mesh was successfully created or updated,
        /// otherwise false.
        /// </returns>
        public static bool FillOut(GameObject shape, Color? color = null, bool showInfo = false)
        {
            if ((shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
                && GameLineGeometry.DifferentPositionCounter(shape) > 2)
            {
                GameObject fillOut;
                MeshFilter meshFilter;
                MeshCollider collider;

                GameObject ownFillOut = GetOwnFillOutObject(shape);

                if (ownFillOut == null)
                {
                    fillOut = new GameObject(ValueHolder.FillOut);
                    fillOut.transform.SetParent(shape.transform);
                    fillOut.transform.rotation = shape.transform.rotation;

                    Vector3 pos = shape.transform.position;

                    // To avoid an overlapping issue, position the fill slightly
                    // behind the line.
                    fillOut.transform.position = new Vector3(
                        pos.x,
                        pos.y,
                        pos.z + 0.00001f);

                    meshFilter = fillOut.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = fillOut.AddComponent<MeshRenderer>();
                    collider = fillOut.AddComponent<MeshCollider>();

                    Color fillColor = color ?? shape.GetColor();

                    if (meshRenderer.sharedMaterial == null)
                    {
                        meshRenderer.sharedMaterial =
                            GameLineAppearance.GetMaterial(fillColor, LineKind.Solid);
                    }
                }
                else
                {
                    fillOut = ownFillOut;
                    meshFilter = fillOut.GetComponent<MeshFilter>();
                    collider = fillOut.GetComponent<MeshCollider>();

                    ChangeFillOutColor(shape, color ?? shape.GetColor());
                }

                LineRenderer renderer = shape.GetComponent<LineRenderer>();
                Vector3[] worldPos = new Vector3[renderer.positionCount];

                renderer.GetPositions(worldPos);

                int numPos = renderer.positionCount;
                Vector3[] vertices = new Vector3[numPos];
                int[] triangles = new int[(numPos - 2) * 3];

                for (int i = 0; i < numPos; i++)
                {
                    vertices[i] = worldPos[i];
                }

                int t = 0;

                for (int i = 1; i < numPos - 1; i++)
                {
                    triangles[t] = 0;
                    triangles[t + 1] = i;
                    triangles[t + 2] = i + 1;
                    t += 3;
                }

                Mesh mesh = new()
                {
                    vertices = vertices,
                    triangles = triangles
                };

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                meshFilter.mesh = mesh;

                if (mesh.vertices.Distinct().Count() > 2)
                {
                    collider.sharedMesh = mesh;
                    GameScaler.SetScale(fillOut, Vector3.one);
                    return true;
                }

                GameObject.DestroyImmediate(fillOut);
                return false;
            }

            if (showInfo)
            {
                ShowNotification.Info(
                    "Fill out cannot be applied.",
                    "The fill out cannot be applied because the selected object either is no line or has too few points.");
            }

            GameObject existingFillOut = shape.FindDescendant(ValueHolder.FillOut);

            if (existingFillOut != null)
            {
                Destroyer.Destroy(existingFillOut);
            }

            return false;
        }

        /// <summary>
        /// Changes whether the given shape has a fill-out object.
        /// </summary>
        /// <param name="shape">The shape whose fill-out should be changed.</param>
        /// <param name="status">Whether the fill-out should be enabled.</param>
        /// <param name="color">The color of the fill-out.</param>
        internal static void ChangeFillOut(GameObject shape, bool status, Color color)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                GameObject fillOut = GetOwnFillOutObject(shape);

                if (!status)
                {
                    GameObject.DestroyImmediate(fillOut);
                }
                else
                {
                    FillOut(shape, color);
                }
            }
        }

        /// <summary>
        /// Changes the color of the fill-out belonging to the given shape.
        /// </summary>
        /// <param name="shape">The shape whose fill-out color should be changed.</param>
        /// <param name="color">The new fill-out color.</param>
        public static void ChangeFillOutColor(GameObject shape, Color color)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                GameObject fillOut = GetOwnFillOutObject(shape);

                if (fillOut != null)
                {
                    fillOut.SetColor(color);
                }
            }
        }

        /// <summary>
        /// Returns the direct fill-out child object of the given shape.
        /// Only direct children are considered.
        /// </summary>
        /// <param name="shape">The shape whose own fill-out child should be returned.</param>
        /// <returns>The direct fill-out child, or null if none exists.</returns>
        internal static GameObject GetOwnFillOutObject(GameObject shape)
        {
            if (shape == null)
            {
                return null;
            }

            Transform child = shape.transform.Find(ValueHolder.FillOut);

            return child != null
                ? child.gameObject
                : null;
        }
    }
}
