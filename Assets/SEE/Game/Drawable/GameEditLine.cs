using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class GameEditLine
    {
        public static GameObject selectedLine;

        public static GameObject oldLine;

        public static GameObject editMenuInstance;

        //public static Hashtable tableValueHolder = new();

        public static ValueHolder oldValueHolder;

        public static ValueHolder newValueHolder;

        public static ValueHolder newValueHolderForOldLine;
        
        public class ValueHolder
        {
            public Color color;
            public int layer;
            public float thickness;

            public ValueHolder(Color color, int layer, float thickness)
            {
                this.color = color;
                this.layer = layer;
                this.thickness = thickness;
            }

            public bool CheckEquals(ValueHolder holder)
            {
                return color.Equals(holder.color) && layer.Equals(holder.layer) && thickness.Equals(holder.thickness);
            }
        }

        public static void ChangeThickness(GameObject line, float thickness)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            renderer.startWidth = thickness;
            renderer.endWidth = thickness;
            MeshCollider meshCollider = line.GetComponent<MeshCollider>();
            Mesh mesh = new();
            renderer.BakeMesh(mesh, true);
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
    }
}