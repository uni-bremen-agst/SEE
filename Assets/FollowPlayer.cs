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
        Debug.Log("Rectangle: " + rect);
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
            transform.localScale = new Vector3(rect.width / 1000f, rect.height / -1000f, -1);
            //Debug.Log("Quad Rescale");

            //funktionier aber überkopf y transform scale muss also - (minus) sein
            rend.material.mainTextureOffset = new Vector2(rect.x/640, rect.y/-480);
            rend.material.mainTextureScale = new Vector2(rect.width / 640, (rect.height / -480) );


            //rend.material.mainTextureOffset = new Vector2(rect.x / 640, 1- (rect.y / 480) );
            //rend.material.mainTextureScale = new Vector2(rect.width / 640, (rect.height / -480) );


            Debug.Log("Offset: " + rect.y / -480  );
        }
    }
}
