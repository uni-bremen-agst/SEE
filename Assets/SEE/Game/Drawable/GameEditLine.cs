using System.Collections;
using System.Linq;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class GameEditLine
    {
        public static void ChangeThickness(GameObject line, float thickness)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            renderer.startWidth = thickness;
            renderer.endWidth = thickness;
            MeshCollider meshCollider = line.GetComponent<MeshCollider>();
            Mesh mesh = new();
            renderer.BakeMesh(mesh);
            meshCollider.sharedMesh = mesh;
        }
        public static void ChangeLayer(GameObject line, int layer)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            renderer.sortingOrder = layer;
        }
        public static void ChangeColor(GameObject line, Color color)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            renderer.material.color = color;
        }

        public static void ChangeLoop(GameObject line, bool loop)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            renderer.loop = loop;
        }
    }
}