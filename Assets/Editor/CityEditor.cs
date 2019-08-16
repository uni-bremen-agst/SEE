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

    // The name of the file containing the graph data.
    public string graphFilename = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_clones.gxl";
    //public string graphFilename = "C:\\Users\\raine\\develop\\see\\data\\gxl\\linux-clones\\clones.gxl";
    // The following graph will not work because it does not have the necessary metrics.
    // public string graphFilename = "C:\\Users\\raine\\Downloads\\codefacts.gxl";

    /// <summary>
    /// Creates a new window offering the city editor commands.
    /// </summary>
    void OnGUI()
    {
        sceneGraph = GetSceneGraph();

        float width = position.width - 5;
        const float height = 30;
        string[] actionLabels = new string[] { "Load City", "Delete City" };
        int selectedAction = GUILayout.SelectionGrid(-1, actionLabels, actionLabels.Length, GUILayout.Width(width), GUILayout.Height(height));
        switch (selectedAction)
        {
            case 0:
                Debug.Log(actionLabels[0] + "\n");
                LoadCity();
                break;
            case 1:
                Debug.Log(actionLabels[1] + "\n");
                sceneGraph.Delete();
                // delete any left-over if there is any
                DeleteAll();
                break;
            default:
                // Debug.LogError("Unexpected action selection.\n");
                break;
        }
    }

    // The scene graph created by this CityEditor.
    private SceneGraph sceneGraph = null;

    /// <summary>
    /// Returns the scene graph if it exists. Will return null if it does not exist.
    /// </summary>
    /// <returns>the scene graph or null</returns>
    private SceneGraph GetSceneGraph()
    {
        if (sceneGraph == null)
        {
            sceneGraph = SceneGraph.GetInstance();
        }
        return sceneGraph;
    }

    /// <summary>
    /// Loads a graph from disk and creates the scene objects representing it.
    /// </summary>
    private void LoadCity()
    {
        SceneGraph sgraph = GetSceneGraph();
        if (sgraph != null)
        {
            Debug.Log("Loading graph from " + graphFilename + "\n");
            sgraph.LoadAndDraw(graphFilename);
        }
        else
        {
            Debug.LogError("There is no scene graph.\n");
        }
    }

    /// <summary>
    /// Deletes all scene nodes and edges via the tags defined in sceneGraph.
    /// </summary>
    private void DeleteAll()
    {
        try
        {
            DeleteByTag(sceneGraph.houseTag);
        }
        catch (UnityException e)
        {
            Debug.LogError(e.ToString());
        }
        try
        {
            DeleteByTag(sceneGraph.edgeTag);
        }
        catch (UnityException e)
        {
            Debug.LogError(e.ToString());
        }
    }

    /// <summary>
    /// Destroys immediately all game objects with given tag.
    /// </summary>
    /// <param name="tag">tag of the game objects to be destroyed.</param>
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
