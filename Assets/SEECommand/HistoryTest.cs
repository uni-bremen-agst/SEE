using SEE.Command;
using UnityEngine;

public class HistoryTest : MonoBehaviour
{
    private Transform draggedTransform = null;
    private bool dragging = false;
    private float distanceToCamera = 0.0f;
    private Vector3 oldPosition = Vector3.zero;

    void Start()
    {
        Random.InitState(13031995);
    }

    void Update()
    {
        RaycastHit hit = new RaycastHit();
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                if (hit.transform != null && hit.transform.gameObject.GetComponent<Interactable>() != null)
                {
                    draggedTransform = hit.transform;
                    dragging = true;
                    distanceToCamera = Vector3.Distance(Camera.main.transform.position, hit.transform.position);
                    oldPosition = hit.transform.position;
                }
            }
        }

        if (dragging)
        {
            if (!Input.GetMouseButton(0))
            {
                int breakhere = 0;
            }
            new MoveBlockCommand(draggedTransform.gameObject, oldPosition, Camera.main.transform.position + Camera.main.ScreenPointToRay(Input.mousePosition).direction * distanceToCamera, !Input.GetMouseButton(0)).Execute();
            if (!Input.GetMouseButton(0))
            {
                draggedTransform = null;
                dragging = false;
                distanceToCamera = 0.0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            new CreateBlockCommand(new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f))).Execute();
        }
    }
}
