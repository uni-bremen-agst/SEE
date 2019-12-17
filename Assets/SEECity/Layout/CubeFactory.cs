using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for cubes as visual representations of graph nodes in the scene.
    /// </summary>
    public class CubeFactory : InnerNodeFactory
    {
        public CubeFactory()
        {
            materials = new Materials(10, Color.white, Color.red);
        }

        public override GameObject NewBlock(int index)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.tag = Tags.Building;
            Renderer renderer = result.GetComponent<Renderer>();
            // Object should not cast shadows: too expensive and may hide information,
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Assigns a material to the object.
            renderer.sharedMaterial = materials.DefaultMaterial(index);

            // Add collider so that we can interact with the object.
            result.AddComponent<BoxCollider>();

            // Object should be static so that we save rendering time at run-time.
            result.isStatic = true;
            return result;
        }
    }
}

