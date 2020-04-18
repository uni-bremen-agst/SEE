using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectBuilding : MonoBehaviour
{
    private Transform direction;
    private LineRenderer line;
    public TextMeshPro text; // assign in inspector
    public Color colorOnHit = Color.green;
    public Color defaultColor = Color.red;

    private bool hit = false;
    private RaycastHit hitInfo;
    private bool active = false;
    private bool show = false;
    private GameObject house;
    private string currentName;
    private bool lockBuilding = false;

    private List<string> data = new List<string>();
    public string fileName = "buildings.csv";
    private const string delimiter = ";";

    private void Start()
    {
        transform.gameObject.SetActive(false);
        direction = transform.parent.transform;
        line = gameObject.GetComponent<LineRenderer>();
    }

    private void Update()
    {
        hitInfo = new RaycastHit();
        hit = Physics.Raycast(direction.position, direction.TransformDirection(Vector3.down), out hitInfo, Mathf.Infinity);

        line.SetPosition(0, direction.position);
        line.SetPosition(1, direction.position);

        if (hit && hitInfo.collider.gameObject.CompareTag("Building"))
        {
            line.SetPosition(1, hitInfo.point);
            line.material.color = colorOnHit;
            currentName = hitInfo.collider.gameObject.name;
        }
        else
        {
            currentName = "";
            line.material.color = defaultColor;
        }

        if (!lockBuilding)
            text.text = currentName;
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    Debug.Log("collision detected");
    //    if(collision.gameObject.CompareTag("House") && !show)
    //    {
    //        house = collision.gameObject;
    //        collision.gameObject.GetComponent<MeshRenderer>().material = Resources.Load("BrickTextures/BricksTexture01/BricksTexture01", typeof(Material)) as Material;
    //        show = true;
    //        Debug.Log("Trigger has been pulled");
    //    }
    //    else if(collision.gameObject.CompareTag("House") && show && house == collision.gameObject)
    //    {
    //        collision.gameObject.GetComponent<MeshRenderer>().material = Resources.Load("BrickTextures/BricksTexture13/BricksTexture13", typeof(Material)) as Material;
    //        show = false;
    //    }
    //}


    // This is the same source code like it is used for the implementation of the camera position tracking
    public void saveBuildig()
    {
        if (hit && hitInfo.collider.gameObject.CompareTag("Building"))
        {
            string output = currentName 
                            + delimiter + FloatToString(Camera.main.transform.position.x) 
                            + delimiter + FloatToString(Camera.main.transform.position.y)
                            + delimiter + FloatToString(Camera.main.transform.position.z)
                            + delimiter + Mathf.RoundToInt(Time.realtimeSinceStartup);
            data.Add(output);
            Debug.Log("building saved\n");
            lockBuilding = true;
            text.text = currentName;
            text.color = colorOnHit;
        }
    }

    public void resetBuilding()
    {
            string output = "building reset"
                            + delimiter + FloatToString(Camera.main.transform.position.x)
                            + delimiter + FloatToString(Camera.main.transform.position.y)
                            + delimiter + FloatToString(Camera.main.transform.position.z)
                            + delimiter + Mathf.RoundToInt(Time.realtimeSinceStartup);
        data.Add(output);
        Debug.Log("building reset\n");
        lockBuilding = false;
        text.color = Color.white;
        text.text = "";
    }

    void OnApplicationQuit()
    {
        if (data.Count == 0)
        {
            Debug.Log("The building information is not stored.\n");
        }
        else
        {
            SaveFile();
        }
    }

    public void SaveFile()
    {
        // WriteAllLines creates a file, writes a collection of strings to the file,
        // and then closes the file.  You do NOT need to call Flush() or Close().
        string path = UnityProject.GetPath() + fileName;
        System.IO.File.WriteAllLines(path, data);
        Debug.LogFormat("Saved camera path to {0}\n", path);
    }

    public void RayOnOff()
    {
        active = !active;
        transform.gameObject.SetActive(active);
        Debug.Log("Ray on/off\n");
    }

    private string FloatToString(float value)
    {
        return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
