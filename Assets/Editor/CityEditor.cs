using UnityEngine;
using UnityEditor;
using System;

// An editor that allows an Unreal editor user to create a city.
// Note: An alternative to an EditorWindow extension could have been a ScriptableWizard.
public class CityEditor : EditorWindow
{
    // The tag of all houses as defined in the house preftab.
    public string houseTag = "House";

    // The tag of all edges as defined in the edge preftab.
    public string edgeTag = "Edge";

    // The relative path to the line preftab.
    public string linePreftabPath = "Assets/Prefabs/Line.prefab";
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
                // Debug.LogError("Unexpected action selection.\n");
                break;
        }
    }

    private void Delete()
    {
        DeleteByTag(houseTag);
        DeleteByTag(edgeTag);
    }

    private void DeleteByTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        Debug.Log("Deleting objects: " + objects.Length + "\n");
        foreach (GameObject o in objects)
        {
            DestroyImmediate(o);
        }
    }

    private void Create()
    {
        GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(housePrefabPath);
        GameObject linePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(linePreftabPath);
        if (housePrefab == null)
        {
            Debug.LogError(housePrefabPath + " does not exist.\n");
        }
        else if (linePrefab == null)
        {
            Debug.LogError(linePreftabPath + " does not exist.\n");
        }
        else
        {
            const int rows = 4;
            const int columns = rows;
            const float epsilon = 0.01f;

            int count = 0;
            for (int r = 1; r <= rows; r++)
            {
                GameObject previousHouse = null;
                for (int c = 1; c <= columns; c++)
                {
                    count++;
                    GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab);
                    house.name = house.name + " " + count;

                    float width   = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    float breadth = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    float height  = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    house.transform.localScale = new Vector3(width, height, breadth);

                    house.transform.position = new Vector3(r + r*0.3f, 0f, c + c*0.3f);
                    Debug.Log("house name: " + house.name + "\n");
                    Debug.Log("house position: " + house.transform.position + "\n");
                    {
                        Renderer m_ObjectRenderer;
                        //Fetch the GameObject's Renderer component
                        m_ObjectRenderer = house.GetComponent<Renderer>();
                        //Change the GameObject's Material Color to red
                        //m_ObjectRenderer.material.color = Color.red;
                        Debug.Log("house size: " + m_ObjectRenderer.bounds.size + "\n");
                    }

                    if (previousHouse != null)
                    {
                        drawLine(previousHouse, house, linePrefab);
                    }
                    previousHouse = house;
                }
            }
            // Undo.RegisterCreatedObjectUndo(house, "Create city");
        }
    }

    private void drawLine(GameObject from, GameObject to, GameObject linePrefab)
    {
        const float above = 4f;
        
        GameObject line = (GameObject)PrefabUtility.InstantiatePrefab(linePrefab);
        LineRenderer renderer = line.GetComponent<LineRenderer>();

        renderer.sortingLayerName = "OnTop";
        renderer.sortingOrder = 5;
        renderer.positionCount = 4; // number of vertices

        var points = new Vector3[renderer.positionCount];
        // starting position
        points[0] = from.transform.position;
        // position above starting position
        points[1] = from.transform.position;
        points[1].y += above;
        // position above ending position
        points[2] = to.transform.position;
        points[2].y += above;
        // ending position
        points[3] = to.transform.position;
        renderer.SetPositions(points);

        //renderer.SetWidth(0.5f, 0.5f);
        renderer.useWorldSpace = true;
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
