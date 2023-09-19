using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using RTG;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.Game
{
    /// <summary>
    /// Draws a new line on a drawable or deleting these again.
    /// </summary>
    public static class GameDrawer
    {
        /// <summary>
        /// The renderer used to draw the line.
        /// </summary>
        private static LineRenderer renderer;

        /// <summary>
        /// The collider of the line.
        /// </summary>
        private static MeshCollider meshCollider;

        private static GameObject line;

        private static GameObject lineHolder;

        private static void Setup(GameObject drawable, String name, Vector3[] positions, Color color, float thickness)
        {
            if (name.Length > 4)
            {
                lineHolder = new(DrawableHelper.LineHolderPrefix + name.Substring(4));
                line = new(name);
            }
            else
            {
                line = new("");
                line.name = DrawableHelper.LinePrefix + line.GetInstanceID();
                lineHolder = new(DrawableHelper.LineHolderPrefix + line.GetInstanceID());
            }

            //TODO out source this in SetupDrawable with out highestParent and out attachedObjects, needed for other drawabletypes like image etc
            GameObject highestParent;
            GameObject attachedObjects;
            if (GameDrawableFinder.hasAParent(drawable))
            {
                GameObject parent = GameDrawableFinder.GetHighestParent(drawable);
                if (!parent.name.StartsWith(DrawableHelper.DrawableHolderPrefix))
                {
                    highestParent = new GameObject(DrawableHelper.DrawableHolderPrefix + drawable.GetInstanceID());
                    highestParent.transform.position = parent.transform.position;
                    highestParent.transform.rotation = parent.transform.rotation;

                    attachedObjects = new GameObject(DrawableHelper.AttachedObject);
                    attachedObjects.tag = Tags.AttachedObjects;
                    attachedObjects.transform.position = highestParent.transform.position;
                    attachedObjects.transform.rotation = highestParent.transform.rotation;
                    attachedObjects.transform.SetParent(highestParent.transform);
                    parent.transform.SetParent(highestParent.transform);
                } else
                {
                    highestParent = parent;
                    attachedObjects = GameDrawableFinder.FindChildWithTag(highestParent, Tags.AttachedObjects);
                }
            } else
            {
                highestParent = new GameObject(DrawableHelper.DrawableHolderPrefix + drawable.GetInstanceID());
                highestParent.transform.position = drawable.transform.position;
                highestParent.transform.rotation = drawable.transform.rotation;

                attachedObjects = new GameObject(DrawableHelper.AttachedObject);
                attachedObjects.tag = Tags.AttachedObjects;
                attachedObjects.transform.position = highestParent.transform.position;
                attachedObjects.transform.rotation = highestParent.transform.rotation;
                attachedObjects.transform.SetParent(highestParent.transform);

                drawable.transform.SetParent(highestParent.transform);
            } 
            
            lineHolder.transform.parent = attachedObjects.transform;
            lineHolder.transform.position = attachedObjects.transform.position;

            line.tag = Tags.Line;
            line.transform.SetParent(lineHolder.transform);
            renderer = line.AddComponent<LineRenderer>();
            meshCollider = line.AddComponent<MeshCollider>();
            renderer.sharedMaterial = GetMaterial(color);
            renderer.startWidth = thickness;
            renderer.endWidth = renderer.startWidth;
            renderer.useWorldSpace = false;
            renderer.positionCount = positions.Length;

            lineHolder.transform.rotation = highestParent.transform.rotation;
            line.transform.position = lineHolder.transform.position;
            line.transform.position -= line.transform.forward * DrawableHelper.distanceToBoard.z;
            line.transform.rotation = lineHolder.transform.rotation;
        }

        public static GameObject StartDrawing(GameObject drawable, Vector3[] positions, Color color, float thickness)
        {
            Setup(drawable, "", positions, color, thickness);
            renderer.sortingOrder = DrawableHelper.orderInLayer;
            DrawableHelper.orderInLayer++;

            return line;
        }

        /// <summary>
        ///  Draws the line given the <see cref="positions"/>.
        /// </summary>
        public static void Drawing(Vector3[] positions)
        {
            renderer.positionCount = positions.Length;
            renderer.SetPositions(positions);
        }

        public static void FinishDrawing(bool loop)
        {
            renderer.loop = loop;
            Mesh mesh = new();
            renderer.BakeMesh(mesh);
            if (mesh.vertices.Distinct().Count() >= 3)
            {
                meshCollider.sharedMesh = mesh;
            }
        }

        public static GameObject DrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, bool loop)
        {
            if (GameDrawableFinder.FindChild(drawable, name) != null)
            {
                line = GameDrawableFinder.FindChild(drawable, name);
                renderer = line.GetComponent<LineRenderer>();
                meshCollider = line.GetComponent<MeshCollider>();
                Drawing(positions);
                FinishDrawing(loop);
            }
            else
            {
                Setup(drawable, name, positions, color, thickness);
                renderer.SetPositions(positions);
                renderer.sortingOrder = DrawableHelper.orderInLayer;
                DrawableHelper.orderInLayer++;
                FinishDrawing(loop);
            }
            return line;
        }

        public static GameObject ReDrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int orderInLayer, bool loop)
        {
            Setup(drawable, name, positions, color, thickness);
            renderer.SetPositions(positions);
            renderer.sortingOrder = orderInLayer;
            FinishDrawing(loop);

            return line;
        }

        public static GameObject ReDrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int orderInLayer, Vector3 position, Vector3 eulerAngles, bool loop)
        {
            Setup(drawable, name, positions, color, thickness);
            line.transform.position = position;
            line.transform.parent.localEulerAngles = eulerAngles;
            renderer.SetPositions(positions);
            renderer.sortingOrder = orderInLayer;
            FinishDrawing(loop);

            return line;
        }

        public static int DifferentPositionCounter(Vector3[] positions)
        {
            List<Vector3> positionsList = new List<Vector3>(positions);
            return positionsList.Distinct().ToList().Count;
        }

        public static int DifferentMeshVerticesCounter()
        {
            Mesh mesh = new();
            renderer.BakeMesh(mesh);
            return mesh.vertices.Distinct().ToList().Count;
        }

        private static Material GetMaterial(Color color)
        {
            ColorRange colorRange = new ColorRange(color, color, 1);
            Materials materials = new Materials(Materials.ShaderType.DrawableLine, colorRange);
            Material material = materials.Get(0, 0);
            return material;
        }

        public static Vector3 GetConvertedPosition(GameObject drawable, Vector3 position)
        {
            Vector3 convertedPosition;
            Setup(drawable, "", new Vector3[] { position }, DrawableHelper.currentColor, DrawableHelper.currentThickness);
            convertedPosition = line.transform.InverseTransformPoint(position) - DrawableHelper.distanceToBoard;
            Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }

        public static Vector3[] GetConvertedPositions(GameObject drawable, Vector3[] positions)
        {
            Vector3[] convertedPosition = new Vector3[positions.Length];
            Setup(drawable, "", positions, DrawableHelper.currentColor, DrawableHelper.currentThickness);
            for (int i = 0; i < positions.Length; i++)
            {
                convertedPosition[i] = line.transform.InverseTransformPoint(positions[i]) - DrawableHelper.distanceToBoard;
            }
            Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }
    }
}
