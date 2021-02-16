using UnityEngine;

public class GarbageCan : MonoBehaviour
{
    /// <summary>
    /// A Ray from the mouse-position to the hovered object
    /// </summary>
    Ray ray;

    /// <summary>
    /// The object which was hitten by the ray
    /// </summary>
    RaycastHit hit;

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
            // Question: Is this mechanism not implemented yet?
            // Fixme: Onclick - just the colliderName(GarbageCan) will be printed out - there has to be a interaction-possibility.
            Debug.Log(hit.collider.name);
        }
    }
}
