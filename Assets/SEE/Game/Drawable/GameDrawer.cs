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
using SEE.Game.Drawable.Configurations;
using static Assets.SEE.Game.Drawable.GameShapesCalculator;

namespace SEE.Game
{
    public static class GameDrawer
    {
        [Serializable]
        public enum ColorKind
        {
            Monochrome,
            Gradient,
            TwoDashed
        }
        public static List<ColorKind> GetColorKinds(bool isDashedLineKind)
        {
            if (isDashedLineKind)
            {
                return Enum.GetValues(typeof(ColorKind)).Cast<ColorKind>().ToList();
            } else
            {
                return new List<ColorKind>() { ColorKind.Monochrome, ColorKind.Gradient};
            }
        }


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

        private static void Setup(GameObject drawable, string name, Vector3[] positions, ColorKind colorKind, 
            Color primaryColor, Color secondaryColor, float thickness, int order, LineKind lineKind, float tiling,
            out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider)
        {
            if (name.Length > 4)
            {
                line = new(name);
            }
            else
            {
                line = new("");
                name = ValueHolder.LinePrefix + line.GetInstanceID() + DrawableHolder.GetRandomString(4);
                while(GameDrawableFinder.FindChild(drawable, name) != null)
                {
                    name = ValueHolder.LinePrefix + line.GetInstanceID() + DrawableHolder.GetRandomString(4);
                }
                line.name = name;
            }

            GameObject highestParent, attachedObjects;
            DrawableHolder.Setup(drawable, out highestParent, out attachedObjects);

            line.tag = Tags.Line;
            line.transform.SetParent(attachedObjects.transform);
            
            renderer = line.AddComponent<LineRenderer>();
            meshCollider = line.AddComponent<MeshCollider>();
            renderer.alignment = LineAlignment.TransformZ;
            renderer.sharedMaterial = GetMaterial(primaryColor, lineKind);
            line.AddComponent<LineValueHolder>().SetColorKind(colorKind);
            switch (colorKind)
            {
                case ColorKind.Monochrome:
                    break;
                case ColorKind.Gradient:
                    renderer.material.color = Color.white;
                    renderer.startColor = primaryColor;
                    renderer.endColor = secondaryColor;
                    break;
                case ColorKind.TwoDashed:
                    Material[] materials = new Material[2];
                    materials[0] = renderer.materials[0];
                    materials[1] = GetMaterial(Color.white, LineKind.Solid);
                    GetRenderer(line).materials = materials;
                    renderer.materials[1].color = secondaryColor;
                    break;
            }
            SetRendererTextrueScale(renderer, lineKind, tiling);
            renderer.startWidth = thickness;
            renderer.endWidth = renderer.startWidth;
            renderer.useWorldSpace = false;
            renderer.positionCount = positions.Length;
            renderer.numCapVertices = 90;

            line.transform.position = attachedObjects.transform.position;
            line.transform.rotation = attachedObjects.transform.rotation;
            line.transform.position -= line.transform.forward * ValueHolder.distanceToDrawable.z * order;

            line.AddComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
            line.GetComponent<LineValueHolder>().SetLineKind(lineKind);
        }

        private static LineRenderer GetRenderer(GameObject line)
        {
            return line.GetComponent<LineRenderer>();
        }

        private static MeshCollider GetMeshCollider(GameObject line)
        {
            return line.GetComponent<MeshCollider>();
        }
        public static GameObject StartDrawing(GameObject drawable, Vector3[] positions, ColorKind colorKind, 
            Color primaryColor, Color secondaryColor, float thickness, LineKind lineKind, float tiling)
        {
            Setup(drawable, "", positions, colorKind, primaryColor, secondaryColor, thickness, 
                ValueHolder.currentOrderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            ValueHolder.currentOrderInLayer++;

            return line;
        }

        /// <summary>
        ///  Draws the line given the <see cref="positions"/>.
        /// </summary>
        public static void Drawing(GameObject line, Vector3[] positions)
        {
            LineRenderer renderer = GetRenderer(line);
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

        public static GameObject DrawLine(GameObject drawable, String name, Vector3[] positions, ColorKind colorKind, 
            Color primaryColor, Color secondaryColor, float thickness, bool loop, LineKind lineKind, float tiling)
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
                Setup(drawable, name, positions, colorKind, primaryColor, secondaryColor, thickness, 
                    ValueHolder.currentOrderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
                l = line;
                renderer.SetPositions(positions);
                ValueHolder.currentOrderInLayer++;
                FinishDrawing(line, loop);
            }
            return l;
        }

        public static GameObject ReDrawLine(GameObject drawable, String name, Vector3[] positions, 
            ColorKind colorKind, Color primaryColor, Color secondaryColor, float thickness, int orderInLayer, Vector3 position,
            Vector3 eulerAngles, Vector3 scale, bool loop, LineKind lineKind, float tiling)
        {
            UpdateZPositions(ref positions);
            if (orderInLayer >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = orderInLayer + 1;
            }
            if (GameDrawableFinder.FindChild(drawable, name) != null)
            {
                GameObject line = GameDrawableFinder.FindChild(drawable, name);
                line.transform.localScale = scale;
                line.transform.localEulerAngles = eulerAngles;
                line.transform.localPosition = position;
                line.GetComponent<OrderInLayerValueHolder>().SetOrderInLayer(orderInLayer);
                Drawing(line, positions);
                FinishDrawing(line, loop);

                return line;
            }
            else
            {
                Setup(drawable, name, positions, colorKind, primaryColor, secondaryColor, thickness, 
                    orderInLayer, lineKind, tiling, out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
                line.transform.localScale = scale;
                line.transform.localEulerAngles = eulerAngles;
                line.transform.localPosition = position;

                renderer.SetPositions(positions);
                FinishDrawing(line, loop);

                return line;
            }
        }

        public static GameObject ReDrawLine(GameObject drawable, LineConf lineToRedraw)
        {
            GameObject line = ReDrawLine(drawable,
                 lineToRedraw.id,
                 lineToRedraw.rendererPositions,
                 lineToRedraw.colorKind,
                 lineToRedraw.primaryColor,
                 lineToRedraw.secondaryColor,
                 lineToRedraw.thickness,
                 lineToRedraw.orderInLayer,
                 lineToRedraw.position,
                 lineToRedraw.eulerAngles,
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
            middlePos.z = line.transform.localPosition.z;
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
            LineValueHolder holder = line.GetComponent<LineValueHolder>();
            LineRenderer renderer = GetRenderer(line);
            renderer.sharedMaterial = GetMaterial(renderer.material.color, lineKind);
            SetRendererTextrueScale(renderer, lineKind, tiling);
            holder.SetLineKind(lineKind);
        }

        public static void ChangeColorKind(GameObject line, ColorKind colorKind, LineConf l)
        {
            LineValueHolder holder = line.GetComponent<LineValueHolder>();
            if (colorKind == ColorKind.TwoDashed)
            {
                GetRenderer(line).startColor = Color.white;
                GetRenderer(line).endColor = Color.white;
                if (GetRenderer(line).materials.Length == 1)
                {
                    Material[] materials = new Material[2];
                    materials[0] = GetRenderer(line).materials[0];
                    materials[1] = GetMaterial(Color.white, LineKind.Solid);
                    GetRenderer(line).materials = materials;
                }
            } else
            {
                if (GetRenderer(line).materials.Length > 1)
                {
                    Material[] materials = new Material[1];
                    materials[0] = GetRenderer(line).materials[0];
                    GetRenderer(line).materials = materials;
                }
                if (colorKind == ColorKind.Gradient)
                {
                    GetRenderer(line).material.color = Color.white;
                } else
                {
                    GetRenderer(line).startColor = Color.white;
                    GetRenderer(line).endColor = Color.white;
                }
            }
            holder.SetColorKind(colorKind);
            GameEdit.ChangePrimaryColor(line, l.primaryColor);
            GameEdit.ChangeSecondaryColor(line, l.secondaryColor);
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
            Setup(drawable, "", new Vector3[] { position }, ColorKind.Monochrome, ValueHolder.currentPrimaryColor, 
                Color.clear, ValueHolder.currentThickness, 0, ValueHolder.currentLineKind, 1,
                out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            convertedPosition = line.transform.InverseTransformPoint(position) - ValueHolder.distanceToDrawable;
            Destroyer.Destroy(line);
            return convertedPosition;
        }

        public static Vector3[] GetConvertedPositions(GameObject drawable, Vector3[] positions)
        {
            Vector3[] convertedPosition = new Vector3[positions.Length];
            Setup(drawable, "", positions, ColorKind.Monochrome, ValueHolder.currentPrimaryColor, 
                Color.clear, ValueHolder.currentThickness, 0, ValueHolder.currentLineKind, 1,
                out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            for (int i = 0; i < positions.Length; i++)
            {
                convertedPosition[i] = line.transform.InverseTransformPoint(positions[i]) - ValueHolder.distanceToDrawable;
            }
            Destroyer.Destroy(line);
            return convertedPosition;
        }
    }
}
