using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.Architecture
{
    
    /// <summary>
    /// Data class for the transfering the selected object
    /// </summary>
    public class ObjectPrimaryClicked
    {
        public GameObject Object { get; set; }
    }
    /// <summary>
    /// Delegate method for creating custom pointer events.
    /// </summary>
    /// <typeparam name="T">The type of data the delegate method will transfer</typeparam>
    public delegate void PenEvent<T>(T data);
    public delegate void PenAction();
    
    /// <summary>
    /// Controller component that manages the object tooltips.
    /// The original source code was provided by https://github.com/bfollington/unity-tooltip-system
    /// It has been slightly modified since not all of its features are needed.
    /// </summary>
    public class PenInteractionController : MonoBehaviour

    {
    public List<GameObject> HoveredObjects { get; private set; }
    public GameObject PrimaryHoveredObject => HoveredObjects.FirstOrDefault();

    public PenEvent<string> PointerTooltipUpdated;

    public PenAction PrimaryPenClicked;
    public PenEvent<ObjectPrimaryClicked> ObjectPrimaryClicked;

    public GameObject selectedObject;


    private void Start()
    {
        HoveredObjects = new List<GameObject>();

        PrimaryPenClicked += () =>
        {
            if (TryRaycastPenInteractionObject(out RaycastHit hit, out GameObject obj))
            {
                selectedObject = obj;
                ObjectPrimaryClicked?.Invoke(new ObjectPrimaryClicked()
                {
                    Object = obj
                });
            }
        };
    }


    private void Update()
    {
        if (Pen.current != null && Pen.current.tip.wasReleasedThisFrame)
        {
            PrimaryPenClicked?.Invoke();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            PrimaryPenClicked?.Invoke();
        }
    }


    private void FixedUpdate()
    {
        HoveredObjects.Clear();

        //Find hovered objects with raycast
        if (TryRaycastPenInteractionObject(out RaycastHit hit, out GameObject obj))
        {
            HoveredObjects.Add(obj);
        }
    }

    /// <summary>
    /// Finds GameObjects with the <see cref="PointerInteraction"/> component for toggling the tooltip.
    /// </summary>
    /// <param name="raycastHit">The hit result</param>
    /// <param name="obj">The found gameobject</param>
    /// <returns>Whether the raycast found an GameObject with the <see cref="PointerInteraction"/> component.</returns>
    private bool TryRaycastPenInteractionObject(out RaycastHit raycastHit, out GameObject obj)
    {
        raycastHit = new RaycastHit();
        obj = null;
        Ray ray = MainCamera.Camera.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (!Raycasting.IsMouseOverGUI() && Physics.Raycast(ray, out RaycastHit hit))
        {
            raycastHit = hit;
            if (hit.transform.TryGetComponent(out PenInteraction reactor))
            {
                obj = hit.transform.gameObject;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Shows the tooltip for the GameObject.
    /// </summary>
    /// <param name="o">The GameObject</param>
    public void Show(GameObject o)
    {
        if (!o.activeSelf)
        {
            o.SetActive(true);
        }
    }

    /// <summary>
    /// Shows the tooltip for the GameObject the provided component is attached to.
    /// </summary>
    /// <param name="o">The component of the desired GameObject</param>
    public void Show(MonoBehaviour o)
    {
        Show(o.gameObject);
    }

    /// <summary>
    /// Hides the tooltip for the GameObject the provided component is attached to.
    /// </summary>
    /// <param name="o">The component of the desired GameObject</param>
    public void Hide(MonoBehaviour o)
    {
        Hide(o.gameObject);
    }

    /// <summary>
    /// Hides the tooltip for the GameObject.
    /// </summary>
    /// <param name="o">The GameObject</param>
    public void Hide(GameObject o)
    {
        if (o.activeSelf)
        {
            o.SetActive(false);
        }
    }
    }

}