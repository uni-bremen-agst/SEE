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
        private static void Setup(GameObject drawable, String name, Vector3[] positions, Color color, float thickness,
            out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider)
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

            GameObject highestParent, attachedObjects;
            DrawableHelper.SetupDrawableHolder(drawable, out highestParent, out attachedObjects);

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
            Setup(drawable, "", positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            renderer.sortingOrder = DrawableHelper.orderInLayer;
            DrawableHelper.orderInLayer++;

            return line;
        }

        private static LineRenderer GetRenderer(GameObject line)
        {
            return line.GetComponent<LineRenderer>();
        }

        private static MeshCollider GetMeshCollider(GameObject line)
        {
            return line.GetComponent<MeshCollider>();
        }

        /// <summary>
        ///  Draws the line given the <see cref="positions"/>.
        /// </summary>
        public static void Drawing(GameObject line, Vector3[] positions)
        {
            LineRenderer renderer = GetRenderer(line);
            renderer.positionCount = positions.Length;
            renderer.SetPositions(positions);
        }

        public static void FinishDrawing(GameObject line, bool loop)
        {
            LineRenderer renderer = GetRenderer(line);
            MeshCollider meshCollider = GetMeshCollider(line);
            renderer.loop = loop;
            Mesh mesh = new();
            renderer.BakeMesh(mesh);
            if (mesh.vertices.Distinct().Count() >= 3)
            {
                meshCollider.sharedMesh = mesh;
            }
        }

        public static void RefreshCollider(GameObject line)
        {
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            MeshCollider collider = line.GetComponent<MeshCollider>();
            Mesh mesh = new();
            lineRenderer.BakeMesh(mesh);
            if (mesh.vertices.Distinct().Count() >= 3)
            {
                collider.sharedMesh = mesh;
            }
        }

        public static GameObject DrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, bool loop)
        {
            GameObject l;
            if (GameDrawableFinder.FindChild(drawable, name) != null)
            {
                l = GameDrawableFinder.FindChild(drawable, name);
                Drawing(l, positions);
                FinishDrawing(l, loop);
            }
            else
            {
                Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
                l = line;
                renderer.SetPositions(positions);
                renderer.sortingOrder = DrawableHelper.orderInLayer;
                DrawableHelper.orderInLayer++;
                FinishDrawing(line, loop);
            }
            return l;
        }

        public static GameObject ReDrawRawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int orderInLayer, bool loop)
        {
            Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            renderer.SetPositions(positions);
            renderer.sortingOrder = orderInLayer;
            FinishDrawing(line, loop);

            return line;
        }

        public static GameObject ReDrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int orderInLayer, Vector3 position,
            Vector3 eulerAngles, Vector3 holderLocalPosition, Vector3 holderScale, bool loop)
        {
            Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            line.transform.position = position;
            line.transform.parent.localScale = holderScale;
            line.transform.parent.localEulerAngles = eulerAngles;
            line.transform.parent.localPosition = holderLocalPosition;

            renderer.SetPositions(positions);
            renderer.sortingOrder = orderInLayer;
            FinishDrawing(line, loop);

            return line;
        }

        public static GameObject ReDrawLine(GameObject drawable, Line lineToRedraw)
        {
            GameObject line = ReDrawLine(drawable,
                 lineToRedraw.id,
                 lineToRedraw.rendererPositions,
                 lineToRedraw.color,
                 lineToRedraw.thickness,
                 lineToRedraw.orderInLayer,
                 lineToRedraw.position,
                 lineToRedraw.holderEulerAngles,
                 lineToRedraw.holderPosition,
                 lineToRedraw.scale,
                 lineToRedraw.loop);
            return line;
        }

        public static int DifferentPositionCounter(Vector3[] positions)
        {
            List<Vector3> positionsList = new List<Vector3>(positions);
            return positionsList.Distinct().ToList().Count;
        }

        public static int DifferentMeshVerticesCounter(GameObject line)
        {
            LineRenderer renderer = GetRenderer(line);
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
            Setup(drawable, "", new Vector3[] { position }, DrawableHelper.currentColor, DrawableHelper.currentThickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            convertedPosition = line.transform.InverseTransformPoint(position) - DrawableHelper.distanceToBoard;
            Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }

        public static Vector3[] GetConvertedPositions(GameObject drawable, Vector3[] positions)
        {
            Vector3[] convertedPosition = new Vector3[positions.Length];
            Setup(drawable, "", positions, DrawableHelper.currentColor, DrawableHelper.currentThickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            for (int i = 0; i < positions.Length; i++)
            {
                convertedPosition[i] = line.transform.InverseTransformPoint(positions[i]) - DrawableHelper.distanceToBoard;
            }
            Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }
    }
}
