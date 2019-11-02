using UnityEngine;

namespace Assets.SEECity.Layout
{
    public class CylinderFactory
    {
        public static GameObject NewCylinder(Color color, float height = 1.0f)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            
            Renderer renderer = result.GetComponent<Renderer>();
            // FIXME: Re-use material for all cylinders.
            renderer.sharedMaterial = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial.color = color;

            // Turn off reflection of plane
            renderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            renderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            renderer.sharedMaterial.SetFloat("_SpecularHighlights", 0.0f);
            // To turn reflection on again, use (_SPECULARHIGHLIGHTS_OFF and _GLOSSYREFLECTIONS_OFF
            // work as toggle, there is no _SPECULARHIGHLIGHTS_ON and _GLOSSYREFLECTIONS_ON):
            //planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            //planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            //planeRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 1.0f);

            return result;
        }
    }
}
