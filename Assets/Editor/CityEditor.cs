using UnityEngine;
using UnityEditor;
using System;

// An editor that allows an Unreal editor user to create a city.
// Note: An alternative to an EditorWindow extension could have been a ScriptableWizard.
public class CityEditor : EditorWindow
{
    // The tag of all houses as defined in the house preftab.
    public string houseTag = "House";

    // The relative path to the house preftab.
    const string housePrefabPath = "Assets/Prefabs/House.prefab";

    [MenuItem("Window/City Editor")]
    // This method will be called when the user selects the menu item.
    // Such methods must be static and void. They can have any name.
    static void Init()
    {
        CityEditor window = (CityEditor)EditorWindow.GetWindow(typeof(CityEditor));
        window.Show();
    }

    void OnGUI()
    {
        float width = position.width - 5;
        const float height = 30;
        string[] actionLabels = new string[] { "Create City", "Delete City" };
        int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
        switch (selectedAction)
        {
            case 0:
                Debug.Log("Create");
                Create();
                break;
            case 1:
                Debug.Log("Delete");
                Delete();
                break;
            default:
                // Debug.LogError("Unexpected action selection");
                break;
        }
    }

    private void Delete()
    {
        GameObject[] houses = GameObject.FindGameObjectsWithTag(houseTag);
        Debug.Log("Found houses: " + houses.Length);
        foreach (GameObject house in houses)
        {
            DestroyImmediate(house);
        }
    }

    private void Create()
    {
        GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(housePrefabPath);
        if (housePrefab == null)
        {
            Debug.LogError(housePrefabPath + " does not exist.");
        }
        else
        {
            const int rows = 100;
            const int columns = rows;

            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= columns; c++)
                {
                    GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab);
                    house.transform.position = new Vector3(r + r*0.3f, 0f, c + c*0.3f);
                }
            }
            // Undo.RegisterCreatedObjectUndo(house, "Create city");
        }
    }

    /*
    void OnScene(SceneView sceneview)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseUp)
        {
            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, Camera.current.pixelHeight - e.mousePosition.y));
            if (selectedAction == 1)
            {
                // TODO: Create OpenSpot
                Debug.Log("Create OpenSpot");
            }
            else if (selectedAction == 2)
            {
                // TODO: Delete OpenSpot
                Debug.Log("Delete OpenSpot");
            }
        }
    }
    */
}
