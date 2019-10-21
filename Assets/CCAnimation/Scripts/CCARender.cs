using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CCARender : AbstractCCARender
{
    protected override void RenderGraph()
    {
        RenderPlane();
        /*
         foreach node in actual graph get gameobject and set position
             blockFactory.SetPosition(gameObject, position);
             if (showErosions)
        {
            AddErosionIssues(node, scaler);
        }
         */
    }

    private void RenderPlane()
    {
        var isPlaneNew = objectManager.GetPlane(out GameObject plane);

        if (isPlaneNew)
        {
            var planeRenderer = plane.GetComponent<Renderer>();
            planeRenderer.sharedMaterial = new Material(planeRenderer.sharedMaterial)
            {
                color = Color.gray
            };

            // Turn off reflection of plane
            planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            planeRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 0.0f);
        }

        /*
         * TODO Animation
         void onAnimateStart
         void onAnimateTo
         void onAnimateRemove
        */

        plane.transform.position = Layout.PlanePositon;
        plane.transform.localScale = Layout.PlaneScale;
    }

    private void RenderEdges()
    {

    }

    private void RenderCircle(Node node)
    {
        var isCircleNew = objectManager.GetCircle(node, out GameObject circle, out GameObject circleText);
        var circlePosition = Layout.CirclePosition(node);
        var circleRadius = Layout.CircleRadius(node);

        /*
         * TODO Animation
         void onAnimateStart
         void onAnimateTo
         void onAnimateRemove
        */

        ExtendedTextFactory.UpdateText(circleText, node.SourceName, circlePosition, circleRadius);
    }
}
