using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for cylinder game objects.
    /// </summary>
    public class CylinderFactory : InnerNodeFactory
    {
        public CylinderFactory()
        {
            materials = new Materials(10, Color.yellow, Color.green);
        }

        public override GameObject NewBlock(int index = 0)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            result.isStatic = true;
            Renderer renderer = result.GetComponent<Renderer>();
            // Re-use default material for all cylinders.
            renderer.sharedMaterial = materials.DefaultMaterial(index);

            // Object should not cast shadows: too expensive and may hide information.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Add collider so that we can interact with the object.
            result.AddComponent<BoxCollider>();
            return result;
        }
    }
}
