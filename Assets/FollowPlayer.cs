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
    float oldX;
    float oldY;
    float distance;
    float distanceOffset;
    float distanceScale;
    float step = 0.0001F;


    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(0.64f,-0.48f,-1);
        rend = GetComponent<Renderer>();
        rend.material.mainTextureScale= new(1,-1);
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


            /* //Jumped
            Vector2 currentOffset = rend.material.mainTextureOffset;
            Vector2 currentScale = rend.material.mainTextureScale;
            Vector2 nextOffset = new Vector2(rect.x / 640, rect.y / -480);
            Vector2 nextScale = new Vector2(rect.width / 640, (rect.height / -480));
            float smoothTime = 0.1F;



            if (nextOffset != new Vector2(rect.x / 640, rect.y / -480))
            {
                nextOffset = new Vector2(rect.x / 640, rect.y / -480);
                Vector2 velocityOffset = Vector2.zero;
                
            }
            if (nextScale != new Vector2(rect.width / 640, (rect.height / -480)))
            {
                nextScale = new Vector2(rect.width / 640, (rect.height / -480));
                Vector2 velocityScale = Vector2.zero;
            }


            // Smoothly move the camera towards that target position
            rend.material.mainTextureOffset = Vector2.SmoothDamp(currentOffset, nextOffset, ref velocityOffset, smoothTime);
            rend.material.mainTextureScale = Vector2.SmoothDamp(currentScale, nextScale, ref velocityScale, smoothTime);

            */

            
            //FUNKT! SMooth aber nicht perfekt, manchmal zu lahm außerdem an frames gekoppelt, nicht zeit

            // distance aus width/height vopm quadrat und dann wie oft die entfernung
           // Vector2 currentCorner = new(rect.x, rect.y);
            //Vector2 lastCorner = new(oldX, oldY); // beim ersten mal wohl null, besser programmieren

            //float distance = Vector2.Distance(currentCorner,lastCorner) / rect.width ; // / width ums in relation zum quadrat zu setzen, 0.5 ist dann ein halbes quadrat verschoben
            //oldX = rect.x;
            //oldY = rect.y;
           // Debug.Log("Distance: " + distance);

            //if distance ist klein dann reset auf 0.0001f
            //falls/solange distance groß ist, erhöhe den step, entsprechend der Entfernung pro update stückweise bis ein maximalwert oder keinen

            Vector2 currentOffset = rend.material.mainTextureOffset;
            Vector2 currentScale = rend.material.mainTextureScale;
           

            distanceOffset = Vector2.Distance(currentOffset, nextOffset);
            distanceScale = Vector2.Distance(currentScale, nextScale);
            //Debug.Log("Distance Offset: " + distanceOffset);
            //Debug.Log("Distance Scale: " + distanceScale);
            if (distanceOffset >= 0.05 || distanceScale >= 0.025)
            {
                //step = 0.001F;
                step = step + 0.0001F;
            }
            else { step = 0.0001F; }
            Debug.Log("step: " + step);
            /*if (distanceScale >= 0.05)
            {
                //step = 0.001F;
                step = step + 0.0001F;
            }
            else { step = 0.0001F; }*/

            // hat sich das ziel geändert? Falls ja dann tue was
            if (nextOffset != new Vector2(rect.x / 640, rect.y / -480)) {
                nextOffset = new Vector2(rect.x / 640, rect.y / -480);
                currentFlowStateOffset = 0; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }
            if (nextScale != new Vector2(rect.width / 640, (rect.height / -480)))
            {
                nextScale = new Vector2(rect.width / 640, (rect.height / -480));
                currentFlowStateScale =  0; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }
            //nextOffset = new Vector2(rect.x / 640, rect.y / -480);
            //nextScale = new Vector2(rect.width / 640, (rect.height / -480));

            //Debug.Log("currentFlowStateOffset: " + currentFlowStateOffset);
           // Debug.Log("currentFlowStateScale: " + currentFlowStateScale);
            rend.material.mainTextureOffset = Vector2.Lerp(currentOffset, nextOffset, currentFlowStateOffset);
            rend.material.mainTextureScale = Vector2.Lerp(currentScale, nextScale, currentFlowStateScale);
            // wie sieht das aus? geht ja nur wenn sich ziel nicht ändert
            //step = step + step;
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
