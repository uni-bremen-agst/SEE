using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DlibFaceLandmarkDetectorExample;
public class FollowPlayer : MonoBehaviour
{
    GameObject player = null;
    Rect rect;
    Renderer rend;
    float scrollSpeed = 0.5f;
    Vector2 velocityOffset = Vector2.zero;
    Vector2 velocityScale = Vector2.zero;
    float currentFlowStateOffset = 0;
    float currentFlowStateScale = 0;
    Vector2 nextOffset;
    Vector2 nextScale;
    private float startTime;


    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(0.64f,0.48f,-1);
        rend = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        {
            //Debug.Log("Player searching, player: " + player);
            player = GameObject.Find("/Local Player 1/DesktopPlayer");
        }
        else {
            //Debug.Log("Player found, player:     " + player);
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;
        }

        //transform.localScale += new Vector3(0.01f, 0.01f, 0);
        rect = GetComponent<FrameOptimizationExample>().testRect;
        //Debug.Log("Rectangle: " + rect);
        //Debug.Log("Scale: " + transform.localScale);
 
        //Debug.Log("Material: " + transform.localScale);

        //float scaleX = Mathf.Cos(Time.time) * 0.5f + 1;
        //float scaleY = Mathf.Sin(Time.time) * 0.5f + 1;
        
         //rend.material.mainTextureScale = new Vector2(scaleX, scaleY);

        //Debug.Log("Material Scale: " + rend.material.mainTextureScale);
        //Debug.Log("Material Offset: " + rend.material.mainTextureOffset);


        // Transform

        // Quad Größe
        //von maximal 640 auf maximal 0.64 und von max 480 auf max 0.48. also von quadrat 640x420 auf QuadFläche 0.64x0.42
        if (rect.height != 0) {
            //transform.localScale = new Vector3(rect.width / 1000f, rect.height / -1000f, -1);


            //Debug.Log("Quad Rescale");

            // Die breite der Anzeige ist festgelegt die Höhe nicht, eventuell immer das kleinere nehmen, aber momentan sowieso quadrat
            float ratio = rect.width / rect.height;
            transform.localScale = new Vector3(0.64f, -0.64f / ratio, -1);

            //funktionier aber überkopf y transform scale muss also - (minus) sein

            /* // muss iwie anders gemacht werden sonst springt das bild
            Vector2 currentOffset = rend.material.mainTextureOffset;
            Vector2 currentScale = rend.material.mainTextureScale;
            Vector2 nextOffset = new Vector2(rect.x / 640, rect.y / -480);
            Vector2 nextScale = new Vector2(rect.width / 640, (rect.height / -480));
            float smoothTime = 0.3F;
            

            // Smoothly move the camera towards that target position
            rend.material.mainTextureOffset = Vector2.SmoothDamp(currentOffset, nextOffset, ref velocityOffset, smoothTime);
            rend.material.mainTextureScale = Vector2.SmoothDamp(currentScale, nextScale, ref velocityScale, smoothTime);
            */

            float step = 0.0001F;

            Vector2 currentOffset = rend.material.mainTextureOffset;
            Vector2 currentScale = rend.material.mainTextureScale;
            // hat sich das ziel geändert? Falls ja dann tue was


            if (nextOffset != new Vector2(rect.x / 640, rect.y / -480)) {
                nextOffset = new Vector2(rect.x / 640, rect.y / -480);
                currentFlowStateOffset = step; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }
            if (nextScale != new Vector2(rect.width / 640, (rect.height / -480)))
            {
                nextScale = new Vector2(rect.width / 640, (rect.height / -480));
                currentFlowStateScale = step; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }
            //nextOffset = new Vector2(rect.x / 640, rect.y / -480);
            //nextScale = new Vector2(rect.width / 640, (rect.height / -480));

            Debug.Log("currentFlowStateOffset: " + currentFlowStateOffset);
            Debug.Log("currentFlowStateScale: " + currentFlowStateScale);
            rend.material.mainTextureOffset = Vector2.Lerp(currentOffset, nextOffset, currentFlowStateOffset);
            rend.material.mainTextureScale = Vector2.Lerp(currentScale, nextScale, currentFlowStateScale);
            currentFlowStateOffset = currentFlowStateOffset + step;
            currentFlowStateScale = currentFlowStateScale + step;
            



            // FUNKT!
            //rend.material.mainTextureOffset = new Vector2(rect.x/640, rect.y/-480);
            //rend.material.mainTextureScale = new Vector2(rect.width / 640, (rect.height / -480) );


            //rend.material.mainTextureOffset = new Vector2(rect.x / 640, 1- (rect.y / 480) );
            //rend.material.mainTextureScale = new Vector2(rect.width / 640, (rect.height / -480) );


            //Debug.Log("Offset: " + rect.y / -480  );



        }
    }
}
