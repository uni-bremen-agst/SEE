using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Valve.VR;
using Valve.VR.InteractionSystem;


/// <summary>
/// Controlls the intactions with the UI elements of the annoationEditor in VR.
/// </summary>
public class VRGUIInputModule : BaseInputModule
{
    public Camera camera;
    public SteamVR_Input_Sources targetSource;
    public SteamVR_Action_Boolean clickAction;

    private GameObject current_Object = null;
    private GameObject pressed_Object = null;
    private PointerEventData data = null;

    protected override void Awake()
    {
        base.Awake();

        data = new PointerEventData(eventSystem);
    }

    public override void Process() { }

    /// <summary>
    /// Calls onClick() of the UI buttons from the annoationEditor in VR. 
    /// </summary>
    public void Use()
    {
            data.Reset();
            data.position = new Vector2(camera.pixelWidth / 2, camera.pixelHeight / 2);

            eventSystem.RaycastAll(data, m_RaycastResultCache);
            data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            current_Object = data.pointerCurrentRaycast.gameObject;

            m_RaycastResultCache.Clear();

            if (clickAction.GetStateDown(targetSource) && current_Object != null)
            {
                pressed_Object = current_Object;
            }

            if (clickAction.GetStateUp(targetSource) && pressed_Object == current_Object)
            {
                pressed_Object.GetComponentInParent<Button>().onClick.Invoke();
                pressed_Object = null;
            }
    }

    public PointerEventData GetData()
    {
        return data;
    }
}
