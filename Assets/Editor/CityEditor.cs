using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

// An editor that allows an Unreal editor user to create a city.
// Note: An alternative to an EditorWindow extension could have been a ScriptableWizard.
public class CityEditor : EditorWindow
{
    [Tooltip("The tag of all buildings")]
    public string houseTag = "House";

    [Tooltip("The tag of all connections")]
    public string edgeTag = "Edge";

    [Tooltip("The relative path to the connection preftab")]
    public string linePreftabPath = "Assets/Prefabs/Line.prefab";
    [Tooltip("The relative path to the building preftab")]
    const string housePrefabPath = "Assets/Prefabs/House.prefab";

    // orientation of the edges; 
    // if -1, the edges are drawn below the houses;
    // if 1, the edges are drawn above the houses;
    // use either -1 or 1
    const float orientation = -1f;

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
        string[] actionLabels = new string[] { "Load City", "Delete City",
                                               "Create Buildings", "Delete Buildings",
                                               "Create Connections", "Delete Connections"
                                             };
        int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
        switch (selectedAction)
        {
            case 0:
                Debug.Log(actionLabels[0]);
                LoadCity();
                break;
            case 1:
                Debug.Log(actionLabels[1]);
                graph.Delete();
                // delete any left-over if there is any
                DeleteAll();
                break;
            case 2:
                Debug.Log(actionLabels[2]);
                graph.Delete();
                LoadNodes();
                break;
            case 3:
                Debug.Log(actionLabels[3]);
                graph.Delete();
                break;
            case 4:
                Debug.Log(actionLabels[4]);
                graph.DeleteEdges();
                LoadEdges();
                break;
            case 5:
                Debug.Log(actionLabels[5]);
                graph.DeleteEdges();
                break;
            default:
                // Debug.LogError("Unexpected action selection.\n");
                break;
        }
    }

    private void DeleteAll()
    {
        try
        {
            DeleteByTag("House");
        }
        catch (UnityException e)
        {
            Debug.LogError(e.ToString());
        }
        try
        {
            DeleteByTag("Edge");
        }
        catch (UnityException e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private void LoadCity()
    {
        // GraphLoader.Load("C:\\Users\\raine\\develop\\see\\data\\gxl\\minimal_test\\minimal_clones.gxl");
        GraphLoader.Load("C:\\Users\\raine\\Downloads\\codefacts.gxl");
        LoadNodes();
        LoadEdges();
    }

    private Graph graph = new Graph();

    private void DeleteByTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        Debug.Log("Deleting objects: " + objects.Length + "\n");
        foreach (GameObject o in objects)
        {
            DestroyImmediate(o);
        }
    }

    private void LoadNodes()
    {
        GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(housePrefabPath);
        if (housePrefab == null)
        {
            Debug.LogError(housePrefabPath + " does not exist.\n");
        }
        else
        {
            const int numberOfNodes = 10;
            int rows = (int)Mathf.Sqrt(numberOfNodes);
            int columns = rows;
            const float epsilon = 0.01f;

            int count = 0;
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= columns; c++)
                {
                    count++;
                    GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab);
                    house.name = house.name + " " + count;

                    float width = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    float breadth = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    float height = UnityEngine.Random.Range(0.0F + epsilon, 1.0F);
                    house.transform.localScale = new Vector3(width, height, breadth);

                    // The position is the center of a GameObject. We want all GameObjects
                    // be placed at the same ground level 0. That is why we need to "lift"
                    // every building by half of its height.
                    house.transform.position = new Vector3(r + r * 0.3f, height/2.0f, c + c * 0.3f);
                    Debug.Log("house name: " + house.name + "\n");
                    Debug.Log("house position: " + house.transform.position + "\n");
                    {
                        Renderer renderer;
                        //Fetch the GameObject's Renderer component
                        renderer = house.GetComponent<Renderer>();
                        //Change the GameObject's Material Color to red
                        //m_ObjectRenderer.material.color = Color.red;
                        Debug.Log("house size: " + renderer.bounds.size + "\n");
                    }
                    graph.AddNode(count.ToString(), house);
                }
                Debug.Log("Created " + r + "/" + rows + " rows of buildings.\n");
            }
            Debug.Log("Created city with " + count + " buildings.\n");
        } 
    }

    private void LoadEdges()
    {
        const int numberOfEdgePerNode = 2;

        int totalEdges = Mathf.Clamp(0, 1000, graph.NodeCount() * numberOfEdgePerNode);

        GameObject linePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(linePreftabPath);

        if (linePrefab == null)
        {
            Debug.LogError(linePreftabPath + " does not exist.\n");
        }
        else
        {
            // the distance of the edges relative to the houses; the maximal height of
            // a house is 1.0
            const float above = orientation * (1f / 2.0f);

            for (int i = 1; i <= totalEdges; i++)
            {
                // pick two nodes randomly (node ids are in the range 1..graph.NodeCount()
                int start = UnityEngine.Random.Range(1, graph.NodeCount()+1);
                int end = UnityEngine.Random.Range(1, graph.NodeCount()+1);
                GameObject edge = drawLine(graph.GetNode(start.ToString()), graph.GetNode(end.ToString()), linePrefab, above);
                graph.AddEdge(edge);
                if (totalEdges % 100 == 0)
                {
                    Debug.Log("Created " + i + "/" + totalEdges + " rows of buildings.\n");
                }
            }
            Debug.Log("Created city with " + totalEdges + " connections.\n");
        }
    }

    private GameObject drawLine(GameObject from, GameObject to, GameObject linePrefab, float above)
    {   
        GameObject edge = (GameObject)PrefabUtility.InstantiatePrefab(linePrefab);
        LineRenderer renderer = edge.GetComponent<LineRenderer>();

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
        return edge;
    }
}
