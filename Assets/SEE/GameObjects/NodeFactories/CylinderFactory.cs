using UnityEngine;

namespace SEE.GO
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
            // Note: An capsule collider is already attached to the cylinder, but
            // the default capsule collider's dimensions do not fulfill our 
            // requirements. That is why we remove it and replace it by a
            // MeshCollider.
            CapsuleCollider collider = result.GetComponent<CapsuleCollider>();
            Destroyer.DestroyComponent(collider);
            result.AddComponent<MeshCollider>();

            result.isStatic = true;
            Renderer renderer = result.GetComponent<Renderer>();
            // Re-use default material for all cylinders.
            renderer.sharedMaterial = materials.DefaultMaterial(index);

            // Object should not cast shadows: too expensive and may hide information.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return result;
        }
    }
}
