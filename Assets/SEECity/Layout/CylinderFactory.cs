using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for cylinder game objects.
    /// </summary>
    public class CylinderFactory : NodeFactory
    {
        private Material material;

        public static Color DefaultColor = Color.black;

        public CylinderFactory()
        {
            material = new Material(Materials.DefaultMaterial());
            material.color = DefaultColor;
        }

        public override GameObject NewBlock()
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Renderer renderer = result.GetComponent<Renderer>();
            // Re-use default material for all cylinders.
            renderer.sharedMaterial = material;
            return result;
        }
    }
}
