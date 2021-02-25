using UnityEngine;

/// <summary>
/// This script is attached to the garbage-can and is responsible for interactions with the garbage-can onclick. 
/// </summary>
public class GarbageCan : MonoBehaviour
{
    /// <summary>
    /// A Ray from the mouse-position to the hovered object
    /// </summary>
    private Ray ray;

    /// <summary>
    /// The object which was hitten by the ray
    /// </summary>
    private RaycastHit hit;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out hit) && hit.transform == gameObject.transform)
        {
            //Fixme: In future - there should be an interaction with the garbage-can possible.
            //A user could choose from all deleted Nodes maybe for undo not only the last operation, but also a specific action from history.
        }
    }
}
