
using UnityEngine;
using DlibFaceLandmarkDetectorExample;
public class FollowPlayer : MonoBehaviour
{
    GameObject player = null;
    Rect rect;
    Renderer rend;
    float currentFlowStateOffset = 0;
    float currentFlowStateScale = 0;
    Vector2 nextOffset;
    Vector2 nextScale;
    float distanceOffset;
    float distanceScale;
    float step = 0.0001F;


    // Start is called once,
    // before the first frame (and the frame update cycle)
    void Start()
    {
        // Screen der Webcam z -1 damit weg vom player y- einfachere berchenung da ...
        transform.localScale = new Vector3(0.2f, -0.48f, -1);

        // gebraucht für ???? zoom oder so??? 
        rend = GetComponent<Renderer>();
        rend.material.mainTextureScale = new(1, -1);
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) //finde den spieler falls noch nicht gefunden // geht in 'start' oder zu früh?
        {
            //Debug.Log("Player searching, player: " + player);
            //player = GameObject.Find("/Local Player 1/DesktopPlayer");
            player = GameObject.Find("/Local Player 1/Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");
        }

        // Keine Ahnung? ggf. das RECT was ums Gesicht gezeichnet wird
        rect = GetComponent<FrameOptimizationExample>().testRect;

        //Debug.Log("Rectangle: " + rect);
        //Debug.Log("Scale: " + transform.localScale);
        //Debug.Log("Material Scale: " + rend.material.mainTextureScale);
        //Debug.Log("Material Offset: " + rend.material.mainTextureOffset);


        // Quad Größe
        //von maximal 640 auf maximal 0.64 und von max 480 auf max 0.48. also von quadrat 640x420 auf QuadFläche 0.64x0.42

        // zur sicherheit, falls das Rect da ist, weil es(height) ist 0 wenn es noch nicht da ist
        if (rect.height != 0)
        {
            //Debug.Log("Quad Rescale");

            // Die breite der Anzeige ist festgelegt die Höhe nicht, eventuell immer das kleinere nehmen, aber momentan sowieso quadrat
            // wieso sowieso quadrat
            float ratio = rect.width / rect.height;
            transform.localScale = new Vector3(0.2f, (-0.2f / ratio) - 0.06f, -1);

            //funktionier aber überkopf y transform scale muss also - (minus) sein


            // aktuelles Tile and Ofsset vom Material was die Camera (webcam) auf dem Screen darstellt.
            Vector2 currentOffset = rend.material.mainTextureOffset;
            Vector2 currentScale = rend.material.mainTextureScale;

            // Der Abstand zum nächsten Tile/Ofsset vom nächsten Frame(der nichtmal berechnet wurde?)
            distanceOffset = Vector2.Distance(currentOffset, nextOffset);
            distanceScale = Vector2.Distance(currentScale, nextScale);


            //Debug.Log("Distance Offset: " + distanceOffset);
            //Debug.Log("Distance Scale: " + distanceScale);
            //Falls ca. (z.B ein Quadrat entfernt) oder (z.B ca. Hälfter der Größe Weggezoomt)  
            if (distanceOffset >= 0.05 || distanceScale >= 0.025) // Werte gerschätzt ggf. nicht sinvoll, und weiter Berechnung notwendig
            {
                //step = 0.001F;
                step = step + 0.0001F; // Geschwindigeit der Bewegung zum Gesicht wenn es woanders(weit genug weg) erkannt wird // geschätzt. // Geschwindigkeit wird pro frame erhöht
            }
            else { step = 0.0001F; } // Falls das Gesicht nur 1 Pixel oder minimal woanders (ggf. sehr oft pro sekunde) woanders erkannt wird, bewege dich dort hin, eig. ganricht, da dies nur erkennsufehler sind.. so wie irgendwo im raum.. aber sehr langsam bewegen ist okay (ist es nicht, links/rechts 1pxl bewegegeung von weneiger als 1 (0.5) oder sogar großér werden je nach implementierung ggf. nicht so gut aussehen (wie stillstand), besser ist dann sitllstand) leichte bewegung aber gut, falls nur ein kleines stück, so dass nicht extra bewegt werden muss, sieht gut aus, auch ohgne bewegung, aber falls dadurch nicht 100% oder ändlich zentriert, ist eine langsame anpassung um zu zentrieren cool und nice (optisch wünschenswert) bsp. falls jemand sein kopf 1 cm woanders hin bewegegt, und so nicht mehr mittig ist, wägre ggf. durch "keine anspassung und sltillstand"der  kopf immer minimal nichbt mittig!.. DANN könnte der Kopf ganz langsam, kaum merkbar, mittig gesetzt werden, um dies "perfekt" zu korrigieren, und keine "1cm/1mm" Mini ruckler zu erlauben oder im gegensatz "1cm/1mm ungenauigkteit", nicht zentriertheit" zu erlauben.  Müsste so insgesammt wesentlich smoother und cleaner wirken und wäre "prefekter und sinvoller" da keine Distraction gefordert wird. (direkte kommunikation ohne ablenkung in see erwünscht).L337
            //Debug.Log("step: " + step);

            // hat sich das ziel geändert? Falls ja dann tue was // aktuelles quadrat wird nächsten quadrat vergleicht??
            if (nextOffset != new Vector2(rect.x / 640, rect.y * 0.76f / -480))//-(480 * 1.3f)
            {
                nextOffset = new Vector2(rect.x / 640, rect.y * 0.76f / -480);
                //scale wird geupdatet
                currentFlowStateOffset = 0; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }
            if (nextScale != new Vector2(rect.width / 640, (rect.height * 1.3f / -480)))
            {
                nextScale = new Vector2(rect.width / 640, (rect.height * 1.3f / -480));
                //offset wird geupdatet
                currentFlowStateScale = 0; // step = quasi wie 0 nur ein schritt weiter, damit nicht unnötig gestoppt wird, falls das ziel immer wechselt und es sofort losgeht mit der transition
            }

            //Debug.Log("currentFlowStateOffset: " + currentFlowStateOffset);
            // Debug.Log("currentFlowStateScale: " + currentFlowStateScale);
            rend.material.mainTextureOffset = Vector2.Lerp(currentOffset, nextOffset, currentFlowStateOffset);
            rend.material.mainTextureScale = Vector2.Lerp(currentScale, nextScale, currentFlowStateScale);
            // wie sieht das aus? geht ja nur wenn sich ziel nicht ändert
            //step = step + step;
            currentFlowStateOffset = currentFlowStateOffset + step;
            currentFlowStateScale = currentFlowStateScale + step;

        }
    }

    void LateUpdate()
    {
        // falls gefunden, klemme deine Position an den Spieler..
        //Debug.Log("Player found, player:     " + player);
        transform.position = player.transform.position-new Vector3(-0.1f,0.15f,-0.2f);
        transform.rotation = player.transform.rotation;
        

        /// nach vorne bewegen:
        transform.position += transform.forward * 0.025f;
        //mittig machen 
        transform.position -= transform.forward * 0.11f;
        //camera Anschauen
        transform.LookAt(UnityEditor.SceneView.lastActiveSceneView.camera.transform);
        //vorwärts bewegen
        //transform.position += transform.forward * 0.13f;
        transform.position += transform.up * 0.3f;
    }

}
