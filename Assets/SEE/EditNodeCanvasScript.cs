using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.UI;


public class EditNodeCanvasScript : NodeCanvasScript
{
    public Node nodeToEdit;

    public string gameObjectID;

    private static bool editNode = false;

    public static bool EditNode { get => editNode; set => editNode = value; }

    InputField inputname;

    InputField inputtype;

    public GameObject canvasObject;

    private string prefabDirectory = "Prefabs/EditNodeCanvas";

    // Start is called before the first frame update
    void Start()
    {
        InstantiatePrefab(prefabDirectory);
        canvas.transform.SetParent(gameObject.transform);

        Component[] c = canvas.GetComponentsInChildren<InputField>();
        inputname = (InputField)c[0];
        inputtype = (InputField)c[1];
        inputname.text = nodeToEdit.SourceName;
        inputtype.text = nodeToEdit.Type;
        canvasObject = GameObject.Find("CanvasObject");
    }

    // Update is called once per frame
    void Update()
    {
        if(editNode)
        {
            if (!inputname.text.Equals(nodeToEdit.SourceName))
            {
                nodeToEdit.SourceName = inputname.text;
            }
            if (!inputtype.text.Equals(nodeToEdit.Type))
            {
                nodeToEdit.Type = inputtype.text;
            }
            CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
            generator.DestroyEditNodeCanvas();
            Debug.Log(nodeToEdit.SourceName);
            new EditNodeNetAction(nodeToEdit.SourceName,nodeToEdit.Type,gameObjectID).Execute(null);
            editNode = false;
        }
    }
}
