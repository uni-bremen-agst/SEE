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
using static Assets.SEE.Game.Drawable.GameShapesCalculator;

namespace SEE.Game
{
    /// <summary>
    /// Draws a new line on a drawable or deleting these again.
    /// </summary>
    public static class GameDrawer
    {
        [Serializable]
        public enum LineKind
        {
            Solid,
            Dashed,
            Dashed25,
            Dashed50,
            Dashed75,
            Dashed100
        }
        public static List<LineKind> GetLineKinds()
        {
            return Enum.GetValues(typeof(LineKind)).Cast<LineKind>().ToList();
        }

        private static void Setup(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int order, LineKind lineKind, float tiling,
            out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider)
        // out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider)
        {
            if (name.Length > 4)
            {
                //  lineHolder = new(DrawableHelper.LineHolderPrefix + name.Substring(4));
                line = new(name);
            }
            else
            {
                line = new("");
                name = DrawableHelper.LinePrefix + line.GetInstanceID() + DrawableHelper.GetRandomString(4);
                while(GameDrawableFinder.FindChild(drawable, name) != null)
                {
                    name = DrawableHelper.LinePrefix + line.GetInstanceID() + DrawableHelper.GetRandomString(4);
                }
                line.name = name;
                //    lineHolder = new(DrawableHelper.LineHolderPrefix + line.GetInstanceID());
            }

            GameObject highestParent, attachedObjects;
            DrawableHelper.SetupDrawableHolder(drawable, out highestParent, out attachedObjects);

            //  lineHolder.transform.parent = attachedObjects.transform;
            //  lineHolder.transform.position = attachedObjects.transform.position;

            line.tag = Tags.Line;
            line.transform.SetParent(attachedObjects.transform);
            //   line.transform.SetParent(lineHolder.transform);
            renderer = line.AddComponent<LineRenderer>();
            meshCollider = line.AddComponent<MeshCollider>();
            line.AddComponent<LineKindHolder>().SetLineKind(lineKind);
            renderer.alignment = LineAlignment.TransformZ;
            //renderer.textureMode = LineTextureMode.Tile;
            renderer.sharedMaterial = GetMaterial(color, lineKind);
            SetRendererTextrueScale(renderer, lineKind, tiling);
            renderer.startWidth = thickness;
            renderer.endWidth = renderer.startWidth;
            renderer.sortingOrder = order;
            renderer.useWorldSpace = false;
            renderer.positionCount = positions.Length;

            // lineHolder.transform.rotation = highestParent.transform.rotation;
            // line.transform.position = lineHolder.transform.position;
            line.transform.position = attachedObjects.transform.position;
            line.transform.rotation = attachedObjects.transform.rotation;//lineHolder.transform.rotation;
            line.transform.position -= line.transform.forward * DrawableHelper.distanceToBoard.z * order;
            //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
        }

        private static LineRenderer GetRenderer(GameObject line)
        {
            return line.GetComponent<LineRenderer>();
        }

        private static MeshCollider GetMeshCollider(GameObject line)
        {
            return line.GetComponent<MeshCollider>();
        }
        public static GameObject StartDrawing(GameObject drawable, Vector3[] positions, Color color, float thickness, LineKind lineKind, float tiling)
        {
            //Setup(drawable, "", positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            Setup(drawable, "", positions, color, thickness, DrawableHelper.orderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            // renderer.sortingOrder = DrawableHelper.orderInLayer;
            DrawableHelper.orderInLayer++;
            // GameLayerChanger.SetOrder(line, renderer.sortingOrder);

            return line;
        }

        /// <summary>
        ///  Draws the line given the <see cref="positions"/>.
        /// </summary>
        public static void Drawing(GameObject line, Vector3[] positions)
        {
            LineRenderer renderer = GetRenderer(line);
            //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
            renderer.positionCount = positions.Length;
            UpdateZPositions(ref positions);
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

        public static GameObject DrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, bool loop, LineKind lineKind, float tiling)
        {
            GameObject l;
            UpdateZPositions(ref positions);
            if (GameDrawableFinder.FindChild(drawable, name) != null)
            {
                l = GameDrawableFinder.FindChild(drawable, name);
                Drawing(l, positions);
                FinishDrawing(l, loop);
            }
            else
            {
                Setup(drawable, name, positions, color, thickness, DrawableHelper.orderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
                //Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
                l = line;
                renderer.SetPositions(positions);
                //renderer.sortingOrder = DrawableHelper.orderInLayer;
                DrawableHelper.orderInLayer++;
                //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
                FinishDrawing(line, loop);
            }
            return l;
        }

        public static GameObject DrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness,
            int orderInLayer, bool loop, LineKind lineKind, float tiling)
        {
            UpdateZPositions(ref positions);
            Setup(drawable, name, positions, color, thickness, orderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            //Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            renderer.SetPositions(positions);
            //renderer.sortingOrder = orderInLayer;
            //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
            FinishDrawing(line, loop);

            return line;
        }

        public static GameObject ReDrawLine(GameObject drawable, String name, Vector3[] positions, Color color, float thickness, int orderInLayer, Vector3 position,
            Vector3 eulerAngles, Vector3 holderScale, bool loop, LineKind lineKind, float tiling)//, Vector3 holderLocalPosition, Vector3 holderScale, bool loop)
        {
            UpdateZPositions(ref positions);
            if (orderInLayer >= DrawableHelper.orderInLayer)
            {
                DrawableHelper.orderInLayer = orderInLayer + 1;
            }
            if (GameDrawableFinder.FindChild(drawable, name) != null)
            {
                GameObject line = GameDrawableFinder.FindChild(drawable, name);
                line.transform.localScale = holderScale;
                line.transform.localEulerAngles = eulerAngles;
                //line.transform.parent.localEulerAngles = eulerAngles;
                //line.transform.parent.localPosition = holderLocalPosition;
                line.transform.localPosition = position;
                LineRenderer renderer = GetRenderer(line);
                renderer.sortingOrder = orderInLayer;
                //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
                Drawing(line, positions);
                FinishDrawing(line, loop);

                return line;
            }
            else
            {
                Setup(drawable, name, positions, color, thickness, orderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
                //Setup(drawable, name, positions, color, thickness, out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
                line.transform.localScale = holderScale;
                line.transform.localEulerAngles = eulerAngles;
                // line.transform.parent.localEulerAngles = eulerAngles;
                // line.transform.parent.localPosition = holderLocalPosition;
                line.transform.localPosition = position;

                renderer.SetPositions(positions);
                //renderer.sortingOrder = orderInLayer;
                //GameLayerChanger.SetOrder(line, renderer.sortingOrder);
                FinishDrawing(line, loop);

                return line;
            }
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
                 lineToRedraw.eulerAngles,
                 //  lineToRedraw.holderPosition,
                 lineToRedraw.scale,
                 lineToRedraw.loop,
                 lineToRedraw.lineKind,
                 lineToRedraw.tiling);
            return line;
        }
        public static GameObject SetPivot(GameObject line)
        {
            LineRenderer renderer = GetRenderer(line);
            Vector3[] positions = new Vector3[renderer.positionCount];
            renderer.GetPositions(positions);
            Vector3 middlePos = Vector3.zero;
            if (positions.Length % 2 == 1)
            {
                middlePos = positions[(int)Mathf.Round(positions.Length / 2)];
            }
            else
            {
                Vector3 left = positions[(positions.Length / 2) - 1];
                Vector3 right = positions[positions.Length / 2];
                middlePos = (left + right) / 2;
            }
            middlePos.z = line.transform.localPosition.z;//-DrawableHelper.distanceToBoard.z;
            Vector3[] convertedPositions = new Vector3[positions.Length];
            Array.Copy(sourceArray: positions, destinationArray: convertedPositions, length: positions.Length);
            line.transform.TransformPoints(convertedPositions);
            line.transform.localPosition = middlePos;
            line.transform.InverseTransformPoints(convertedPositions);
            Drawing(line, convertedPositions);
            FinishDrawing(line, renderer.loop);
            return line;
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

        public static void ChangeLineKind(GameObject line, LineKind lineKind, float tiling)
        {
            LineKindHolder holder = line.GetComponent<LineKindHolder>();
            LineRenderer renderer = GetRenderer(line);
            renderer.sharedMaterial = GetMaterial(renderer.material.color, lineKind);
            SetRendererTextrueScale(renderer, lineKind, tiling);
            holder.SetLineKind(lineKind);
        }

        private static void SetRendererTextrueScale(LineRenderer renderer, LineKind kind, float tiling)
        {
            switch (kind)
            {
                case LineKind.Dashed:
                    if (tiling == 0)
                    {
                        tiling = 0.05f;
                    }
                    renderer.textureScale = new Vector2(tiling, 0f);
                    break;
                case LineKind.Dashed25:
                    renderer.textureScale = new Vector2(5f / 3f, 0f);
                    break;
                case LineKind.Dashed50:
                    renderer.textureScale = new Vector2(10f / 3f, 0f);
                    break;
                case LineKind.Dashed75:
                    renderer.textureScale = new Vector2(5f, 0f);
                    break;
                case LineKind.Dashed100:
                    renderer.textureScale = new Vector2(20f / 3f, 0f);
                    break;
            }
        }

        private static void UpdateZPositions(ref Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].z = 0;
            }
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

        private static Material GetMaterial(Color color, LineKind kind)
        {
            ColorRange colorRange = new ColorRange(color, color, 1);
            Materials.ShaderType shaderType;
            if (kind.Equals(LineKind.Solid))
            {
                shaderType = Materials.ShaderType.DrawableLine;
            }
            else
            {
                shaderType = Materials.ShaderType.DrawableDashedLine;
            }
            Materials materials = new Materials(shaderType, colorRange);
            Material material = materials.Get(0, 0);
            return material;
        }

        public static Vector3 GetConvertedPosition(GameObject drawable, Vector3 position)
        {
            Vector3 convertedPosition;
            Setup(drawable, "", new Vector3[] { position }, DrawableHelper.currentColor, DrawableHelper.currentThickness, 0, DrawableHelper.currentLineKind, 1,
                out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            //  Setup(drawable, "", new Vector3[] { position }, DrawableHelper.currentColor, DrawableHelper.currentThickness, 
            //      out GameObject line, out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            convertedPosition = line.transform.InverseTransformPoint(position) - DrawableHelper.distanceToBoard;
            Destroyer.Destroy(line);
            //Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }

        public static Vector3[] GetConvertedPositions(GameObject drawable, Vector3[] positions)
        {
            Vector3[] convertedPosition = new Vector3[positions.Length];
            Setup(drawable, "", positions, DrawableHelper.currentColor, DrawableHelper.currentThickness, 0, DrawableHelper.currentLineKind, 1,
                out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            //   Setup(drawable, "", positions, DrawableHelper.currentColor, DrawableHelper.currentThickness, out GameObject line, 
            //       out GameObject lineHolder, out LineRenderer renderer, out MeshCollider meshCollider);
            for (int i = 0; i < positions.Length; i++)
            {
                convertedPosition[i] = line.transform.InverseTransformPoint(positions[i]) - DrawableHelper.distanceToBoard;
            }
            Destroyer.Destroy(line);
            //Destroyer.Destroy(lineHolder);
            return convertedPosition;
        }
    }
}
