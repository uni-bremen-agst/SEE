using UnityEngine;
using UnityEditor;
using System.Collections;
using CScape;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class CSSmartSaver : MonoBehaviour
{

    [MenuItem("File/Save CScape Scene", false, 10)]
    static void SaveSceneCScape(MenuCommand menuCommand)
    {
        List<GameObject> rootObjects = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);
        //iterate root objects and find all City Randomizers (if there are more than one)
        for (int i = 0; i < rootObjects.Count; ++i)
        {
            GameObject gObject = rootObjects[i];
            if (gObject.GetComponent<BuildingEditorOrganizer>() != null)
            {
                BuildingEditorOrganizer getBO = gObject.GetComponent<BuildingEditorOrganizer>();
                if (getBO.isMerged = true)
                {
                    getBO.DeOrganize();
                }
            }
        }

        for (int i = 0; i < rootObjects.Count; ++i)
        {
            GameObject gObject = rootObjects[i];
            if (gObject.GetComponent<CityRandomizer>() != null)
            {
                PrefabUtility.DisconnectPrefabInstance(gObject);
                gObject.GetComponent<CityRandomizer>().savedWithoutMesh = true;
                Component[] renderer = gObject.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter x in renderer)
                {
                    if (x.transform.name != "NordLake")
                        DestroyImmediate(x.sharedMesh, false);
                }

            }
        }


        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        for (int i = 0; i < rootObjects.Count; ++i)
        {
            GameObject gObject = rootObjects[i];
            if (gObject.GetComponent<CityRandomizer>() != null)
            {
                gObject.GetComponent<CityRandomizer>().savedWithoutMesh = false;
                gObject.GetComponent<CityRandomizer>().Refresh();

            }
        }

    }
}
