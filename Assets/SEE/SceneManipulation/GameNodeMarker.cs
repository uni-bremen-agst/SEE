using SEE.Extensions;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// provide the necessary functions to create and destroy the sphere for the marking
    /// </summary>
    public static class GameNodeMarker
    { 
        public static void MarkingNode(GameObject node)
        {
            Transform find = node.transform.Find("kugel");

            if (find != null)
            {
                //Marker erkennt, dass beim 2. Klick kugel vorhanden ist und löscht sie 
                Destroyer.Destroy(find.gameObject);

            } 
            else
            {
                //Kugel erstellen
                Vector3 grosse = node.WorldSpaceSize();
                float durchmesser = Mathf.Min(grosse.x, grosse.z); //breite, tiefe

                // GetTop: bestimmt Mitte des Node
                Vector3 mitte = node.GetTop();

                GameObject kugel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                kugel.transform.localScale = new Vector3(durchmesser, durchmesser, durchmesser);
                mitte.y = mitte.y + durchmesser / 2;
                kugel.transform.position = mitte;
                kugel.name = "kugel";
                kugel.transform.SetParent(node.transform); //kugel als kind von node

            }




        }
        
    }
}
