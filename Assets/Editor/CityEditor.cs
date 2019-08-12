using UnityEngine;
using UnityEditor;

// An editor that allows an Unreal editor user to create a city.
// Note: An alternative to an EditorWindow extension could have been a ScriptableWizard.
public class CityEditor : EditorWindow
{

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
                Debug.Log(actionLabels[0] + "\n");
                LoadCity();
                break;
            case 1:
                Debug.Log(actionLabels[1] + "\n");
                graph.Delete();
                // delete any left-over if there is any
                DeleteAll();
                break;
            case 2:
                Debug.Log(actionLabels[2] + "\n");
                graph.Delete();
                graph.CreateNodes();
                break;
            case 3:
                Debug.Log(actionLabels[3] + "\n");
                graph.Delete();
                break;
            case 4:
                Debug.Log(actionLabels[4] + "\n");
                graph.DeleteEdges();
                graph.CreateEdges();
                break;
            case 5:
                Debug.Log(actionLabels[5] + "\n");
                graph.DeleteEdges();
                break;
            default:
                // Debug.LogError("Unexpected action selection.\n");
                break;
        }
    }

    private SceneGraph graph = new SceneGraph();

    private void LoadCity()
    {
        graph.Load("C:\\Users\\raine\\develop\\see\\data\\gxl\\minimal_test\\minimal_clones.gxl");
        //graph.Load("C:\\Users\\raine\\Downloads\\codefacts.gxl");
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

    private void DeleteByTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        Debug.Log("Deleting objects: " + objects.Length + "\n");
        foreach (GameObject o in objects)
        {
            DestroyImmediate(o);
        }
    }
}
