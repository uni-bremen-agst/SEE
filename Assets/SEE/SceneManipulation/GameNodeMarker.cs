using SEE.Extensions;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.SceneManipulation
{

    /// <summary>
    /// Marks nodes with a sphere and removes the mark.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Used to find the marker sphere later.
        /// </summary>
        const string sphereName = "sphereObj";

        /// <summary>
        /// Adds a sphere if it is not marked, removes it if it is marked.
        /// </summary>
        /// <param name="obj">The node to be marked; must not be null.</param>
        public static void ToggleMark(GameObject obj)
        {

            Transform sphereFound = obj.transform.Find(sphereName);

            if (sphereFound == null)
            {
                GameObject newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                newSphere.name = sphereName;

                Vector3 buildingSize = obj.WorldSpaceSize();

                float dia = Mathf.Min(buildingSize.x, buildingSize.z);
                newSphere.transform.localScale = new Vector3(dia, dia, dia);

                newSphere.transform.position = obj.GetTop() + (Vector3.up * (dia / 2f));
                newSphere.transform.SetParent(obj.transform, true);

            }
            else { 
                Destroyer.Destroy(sphereFound.gameObject); 
            }

        }
    }
}