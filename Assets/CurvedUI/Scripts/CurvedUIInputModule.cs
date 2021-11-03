#region DEPENDENCIES ----------------------------------------------------------------//
//core
using System;
using System.Text; 
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using CurvedUI;
using Object = UnityEngine.Object;

//per SDK
#if CURVEDUI_UNITY_XR
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
#elif CURVEDUI_STEAMVR_LEGACY || CURVEDUI_STEAMVR_2
using Valve.VR;
#endif 

//optional
[assembly: CurvedUI.OptionalDependency("Valve.VR.InteractionSystem.Player",
    "CURVEDUI_STEAMVR_INT")]
[assembly: CurvedUI.OptionalDependency("UnityEngine.InputSystem.UI.InputSystemUIInputModule",
    "CURVEDUI_UNITY_XR")]
[assembly: CurvedUI.OptionalDependency("UnityEngine.InputSystem.UI.InputSystemUIInputModule",
    "CURVEDUI_NEW_INPUT_MODULE")]
#endregion // end of DEPENDENCIES  -----------------------------------------------------//





[ExecuteInEditMode]
#if CURVEDUI_UNITY_XR
public class CurvedUIInputModule : BaseInputModule {
#else
public class CurvedUIInputModule : StandaloneInputModule {
#endif

    #region SETTINGS ----------------------------------------------------------------//
    #pragma warning disable 414, 0649
    //Common
    [SerializeField] CUIControlMethod controlMethod;
    [SerializeField] string submitButtonName = "Fire1";
    [SerializeField] Camera mainEventCamera;
    [SerializeField] LayerMask raycastLayerMask = 1 << 5;

    //Gaze
    [SerializeField] bool gazeUseTimedClick = false;
    [SerializeField] float gazeClickTimer = 2.0f;
    [SerializeField] float gazeClickTimerDelay = 1.0f;
    [SerializeField] Image gazeTimedClickProgressImage;

    //World Space Mouse
    [SerializeField] float worldSpaceMouseSensitivity = 1;

    //SteamVR and Oculus
    [SerializeField] Hand usedHand = Hand.Right;
    [SerializeField] Transform pointerTransformOverride;

    #if CURVEDUI_STEAMVR_2 
    //SteamVR 2.0 specific
    [SerializeField]
    SteamVR_Action_Boolean m_steamVRClickAction;
    #endif

    //Other settings
    static bool disableOtherInputModulesOnStart = true; //default true

    #endregion // end of SETTINGS --------------------------------------------------------------//






    
    #region VARIABLES --------------------------------------------------------------------------//
    //COMMON VARIABLES---------------------------------------//
    //Support Variables - common
    private static CurvedUIInputModule instance;
    private GameObject currentDragging;
    private GameObject currentPointedAt;
    
    //Support Variables - handheld controllers
    private GameObject m_rightController;
    private GameObject m_leftController;

    //Support Variables - gaze
    private float gazeTimerProgress;

    //Support variables - custom ray
    private Ray customControllerRay;

    //support variables - other
    private float dragThreshold = 10.0f;
    private bool pressedDown = false;
    private bool pressedLastFrame = false;
    
    //support variables - new Event System
    private Vector2 lastEventDataPosition;
    private PointerInputModule.MouseButtonEventData storedData;

    //support variables - world space mouse
    private Vector3 lastMouseOnScreenPos = Vector2.zero;
    private Vector2 worldSpaceMouseInCanvasSpace = Vector2.zero;
    private Vector2 lastWorldSpaceMouseOnCanvas = Vector2.zero;
    private Vector2 worldSpaceMouseOnCanvasDelta = Vector2.zero;
    
    
    
    //PLATFORM DEPENDANT VARIABLES AND SETTINGS----------------//
    #if CURVEDUI_STEAMVR_LEGACY
    //Settings & References - SteamVR
    [SerializeField]
    SteamVR_ControllerManager steamVRControllerManager;

    //Support Variables - SteamVR
    private static SteamVR_ControllerManager controllerManager;
    private static CurvedUIViveController rightCont;
    private static CurvedUIViveController leftCont;
    private CurvedUIPointerEventData rightControllerData;
    private CurvedUIPointerEventData leftControllerData;
    #endif

    
    #if CURVEDUI_STEAMVR_2
    [SerializeField]
    SteamVR_PlayArea steamVRPlayArea;
    #endif

    
    #if CURVEDUI_OCULUSVR
    //Settings & References - Oculus SDK
    [SerializeField]
    Transform TouchControllerTransform;
    [SerializeField]
    OVRInput.Button InteractionButton = OVRInput.Button.PrimaryIndexTrigger;
    [SerializeField]
    OVRCameraRig oculusCameraRig;
    
    //Support variables - Touch
    private OVRInput.Controller activeCont;
    #endif

    
    #if CURVEDUI_UNITY_XR
    //Settings & References - UNITY XR
    [SerializeField] private XRBaseController rightXRController; 
    [SerializeField] private XRBaseController leftXRController; 
    #endif

    
    #pragma warning restore 414, 0649
#endregion // end of VARIABLES ----------------------------------------------------//
    


    
 #region LIFECYCLE //-------------------------------------------------------------------//

    protected override void Awake()
    {
        #if !CURVEDUI_UNITY_XR
        forceModuleActive = true;
        #endif

        if (!Application.isPlaying) return;

        Instance = this;
        base.Awake();

        //component setup
        EventCamera = mainEventCamera == null ? Camera.main : EventCamera;

        //Gaze setup
        if (gazeTimedClickProgressImage != null)
            gazeTimedClickProgressImage.fillAmount = 0;

        //SDK setup
        #if CURVEDUI_STEAMVR_LEGACY
		if(ControlMethod == CUIControlMethod.STEAMVR_LEGACY) SetupViveControllers();
        #elif CURVEDUI_STEAMVR_2
        if (ControlMethod == CUIControlMethod.STEAMVR_2) SetupSteamVR2Controllers();
        #elif CURVEDUI_UNITY_XR
        if (ControlMethod == CUIControlMethod.UNITY_XR) SetupUnityXrControllers();
        #endif
    }



    protected override void Start()
    {
        if (!Application.isPlaying) return;

        base.Start();
        
        #if CURVEDUI_OCULUSVR
        if (oculusCameraRig == null)
        {
            //find the oculus rig - via manager or by findObjectOfType, if unavailable
            if (OVRManager.instance != null) oculusCameraRig = OVRManager.instance.GetComponent<OVRCameraRig>();
            if (oculusCameraRig == null)  oculusCameraRig = Object.FindObjectOfType<OVRCameraRig>();
            if (oculusCameraRig == null && ControlMethod == CUIControlMethod.OCULUSVR)Debug.LogError("CURVEDUI: OVRCameraRig prefab required. Import Oculus Utilities and drag OVRCameraRig prefab onto the scene.");           
        }
        #endif
    }



    protected virtual void Update()
    {
        //find camera, if we lost it
        if (mainEventCamera == null && Application.isPlaying)
            EventCamera = Camera.main;

        if (Time.frameCount % 120 == 0) //do it only once every 120 frames
        {
            //check if we don't have extra eventSystem on the scene, as this may mess up interactions.
            if (EventSystem.current != null && EventSystem.current.gameObject != this.gameObject)
                Debug.LogError(
                    "CURVEDUI: Second EventSystem component detected. This can make UI unusable. Make sure there is only one EventSystem component on the scene. Click on this message to have the extra one selected.",
                    EventSystem.current.gameObject);
        }
    }
 #endregion // end of LIFECYCLE ------------------------------------------------------------//









#if CURVEDUI_UNITY_XR
#region EVENT PROCESSING - NEW UNITY INPUT SYSTEM w/ UNITY_XR -----------------------------------//
    private void SetupUnityXrControllers()
    {
        if (rightXRController != null && leftXRController != null) return; 
        foreach (var controller in GameObject.FindObjectsOfType<XRController>())
        {
            if (rightXRController == null && controller.controllerNode == XRNode.RightHand)
                rightXRController = controller;
            if (leftXRController == null && controller.controllerNode == XRNode.LeftHand)
                leftXRController = controller;
        }
    }

    #region UNITY_XR - INPUT MODULE PROCESS
    public override void Process()
    {
        //get EventData with the position and state of button in our pointing device
        var eventData = GetEventData(); 
        
        //Ask all Raycasters to raycasts their stuff and store the results
        eventSystem.RaycastAll(eventData.buttonData, m_RaycastResultCache); //eventdata position will be updated here
        eventData.buttonData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        
        //Calculate position delta - used for sliders, etc.
        if (eventData.buttonData.pointerCurrentRaycast.isValid)
        {
            eventData.buttonData.delta = lastEventDataPosition - eventData.buttonData.position;
            lastEventDataPosition = eventData.buttonData.position;
        }
        
        //debug values
        // StringBuilder sb = new StringBuilder();
        // sb.Append(" InputModule After Raycast:");
        // sb.Append(" pos:" + eventData.buttonData.position);
        // sb.Append(" delta:" + eventData.buttonData.delta);
        // sb.Append(" pointer press go: " + eventData.buttonData.pointerPress?.name ?? "none");
        // sb.Append(" raycast results: " + m_RaycastResultCache.Count);
        // Debug.Log(sb.ToString());
        //---
        
        //process events on raycast results.
        ProcessMove(eventData.buttonData, eventData.buttonData.pointerCurrentRaycast.gameObject);
        ProcessButton(eventData, eventData.buttonData);
        ProcessDrag(eventData, eventData.buttonData);
        ProcessScroll(eventData, eventData.buttonData);
        
        //save some values for later
        pressedLastFrame = pressedDown;
    }
    
    protected PointerInputModule.MouseButtonEventData GetEventData()
    {
        //create new, or start from old MouseButtonEventData.
        if (storedData == null)
        {
            storedData = new PointerInputModule.MouseButtonEventData
            {
                buttonData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                }
            };
        }
        
        //clear "used" flag
        storedData.buttonData.Reset();

        switch (ControlMethod)
        {
            case CUIControlMethod.MOUSE: ProcessMouseController(); break;
            case CUIControlMethod.WORLD_MOUSE: goto case CUIControlMethod.MOUSE;
            case CUIControlMethod.GAZE: ProcessGaze(); break;
            case CUIControlMethod.UNITY_XR: ProcessUnityXrController(); break;
            case CUIControlMethod.CUSTOM_RAY:  ProcessCustomRayController(); break;
            // not supported through new Input Module
            // case CUIControlMethod.STEAMVR_LEGACY: ProcessViveControllers(); break; 
            // case CUIControlMethod.STEAMVR_2: ProcessSteamVR2Controllers(); break;
            // case CUIControlMethod.OCULUSVR: ProcessOculusVRController();break;
            default: break;
        }
        storedData.buttonData.position = Mouse.current.position.ReadValue();
        storedData.buttonState = CustomRayFramePressedState();
        storedData.buttonData.useDragThreshold = true;
        
        //save initial press position if that's the first frame of interaction
        if (storedData.buttonState == PointerEventData.FramePressState.Pressed)
            storedData.buttonData.pressPosition = storedData.buttonData.position;
        
        return storedData;
    }
    
    private void ProcessMove(PointerEventData eventData, GameObject currentRaycastTarget)
    {
        // If we lost our target, send Exit events to all hovered objects and clear the list.
        if (currentRaycastTarget == null || eventData.pointerEnter == null)
        {
            foreach (var t in eventData.hovered)
                ExecuteEvents.Execute(t, eventData, ExecuteEvents.pointerExitHandler);

            eventData.hovered.Clear();

            if (currentRaycastTarget == null)
            {
                eventData.pointerEnter = null;
                return;
            }
        }

        if (eventData.pointerEnter == currentRaycastTarget && currentRaycastTarget)
            return;
        //------------------------------//
        
        
        // Send events to every object up to a common parent of past and current RaycastTarget--//
        var commonRoot = FindCommonRoot(eventData.pointerEnter, currentRaycastTarget)?.transform;
        
        if (eventData.pointerEnter != null)
        {
            for (var current = eventData.pointerEnter.transform; current != null && current != commonRoot; current = current.parent)
            {
                ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerExitHandler);
                eventData.hovered.Remove(current.gameObject);
            }
        }

        eventData.pointerEnter = currentRaycastTarget;
        if (currentRaycastTarget != null)
        {
            for (var current = currentRaycastTarget.transform;
                 current != null && current != commonRoot;
                 current = current.parent)
            {
                ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
                eventData.hovered.Add(current.gameObject);
            }
        }
        //----------------------------------------//
    }

    private void ProcessButton(PointerInputModule.MouseButtonEventData button, PointerEventData eventData)
    {
        var currentRaycastGo = eventData.pointerCurrentRaycast.gameObject;
        
        if (button.buttonState == PointerEventData.FramePressState.Pressed)
        {
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
            eventData.eligibleForClick = true;
            
            //selectHandler
            var selectHandler = ExecuteEvents.GetEventHandler<ISelectHandler>(currentRaycastGo);
            if (selectHandler != null && selectHandler != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, eventData);

            //pointerDownHandler
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentRaycastGo, eventData, ExecuteEvents.pointerDownHandler);

            //clickHandler
            if (newPressed == eventData.lastPress && (Time.unscaledTime - eventData.clickTime) < 0.28f)
                eventData.clickCount += 1;
            else
                eventData.clickCount = 1;
            eventData.clickTime = Time.unscaledTime;

            if (newPressed == null) 
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentRaycastGo);

            eventData.pointerPress = newPressed;
            eventData.rawPointerPress = currentRaycastGo;

            //dragHandler
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentRaycastGo);
            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }

        // FramePressState.Released
        if (button.buttonState == PointerEventData.FramePressState.Released)
        {
            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentRaycastGo);

            if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
            else if (eventData.dragging && eventData.pointerDrag != null)
                ExecuteEvents.ExecuteHierarchy(currentRaycastGo, eventData, ExecuteEvents.dropHandler);

            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;

            if (eventData.dragging && eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

            eventData.dragging = false;
            eventData.pointerDrag = null;
        }
    }

    private void ProcessDrag(PointerInputModule.MouseButtonEventData button, PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || !eventData.IsPointerMoving()) return;

        if (!eventData.dragging)
        {
            if (!eventData.useDragThreshold || (eventData.pressPosition - eventData.position).sqrMagnitude >=
                (double)eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold)
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                eventData.dragging = true;
            }
        }

        if (eventData.dragging)
        {
            // pointerUpHandler on Objects we moved away from
            if (eventData.pointerPress != eventData.pointerDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
            }
            
            //dragHandler on currently dragged
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
        }
    }
    
    private static void ProcessScroll(PointerInputModule.MouseButtonEventData button, PointerEventData eventData)
    {
        //any scroll this frame?
        if (Mathf.Approximately(eventData.scrollDelta.sqrMagnitude, 0.0f)) return;
        
        var eventHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.pointerEnter);
        ExecuteEvents.ExecuteHierarchy(eventHandler, eventData, ExecuteEvents.scrollHandler);
    }
    #endregion
    
    
    
    
    #region UNITY_XR - CURVEDUI CURVEDUI CONTROL METHODS
    private void ProcessUnityXrController()
    {
        var isPressed = false;
        GetXrControllerButtonState(ref isPressed, usedHand);
        CustomControllerButtonState = isPressed;
        CustomControllerRay = new Ray(ControllerTransform.transform.position,
            ControllerTransform.transform.forward);
        
        ProcessCustomRayController();
        
        //Debug.Log("XR Button: xrc.uiPressUsage on " + usedHand.ToString() +" - " + CustomControllerButtonState ); 
    }
    

    protected virtual void ProcessMouseController()
    {
        CustomControllerButtonState = Mouse.current.leftButton.isPressed;
        CustomControllerRay = mainEventCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    }
    
    protected virtual void ProcessCustomRayController()
    {
        //these values are set by outside script
    }
    
    protected virtual void ProcessGaze()
    {
        CustomControllerRay = mainEventCamera.ViewportPointToRay(Vector3.one * 0.5f);
    }
    #endregion // end of UNITY_XR - CURVEDUI CONTROL METHODS
    
    
    
    
    
    #region UNITY_XR - PUBLIC
    public void GetXrControllerButtonState(ref bool pressed, Hand checkHand) {
        switch ( checkHand ) {
            case Hand.Right:
            {
                if ( rightXRController is ActionBasedController abc ) {
                    pressed = abc.uiPressInteractionState.active;
                } else if ( rightXRController is XRController xrc ) {
                    xrc.inputDevice.IsPressed(xrc.uiPressUsage, out pressed, xrc.axisToPressThreshold);
                }
                break;
            }
            
            case Hand.Left:
            {
                if ( leftXRController is ActionBasedController abc ) {
                    pressed = abc.uiPressInteractionState.active;
                } else if ( leftXRController is XRController xrc ) {
                    xrc.inputDevice.IsPressed(xrc.uiPressUsage, out pressed, xrc.axisToPressThreshold);
                }  
                break;
            }
            default: goto case Hand.Right;
        }
    }
    #endregion // end of UNITY_XR - PUBLIC
    #endregion // end of UNITY_XR ------------------------------------------------------------//
#else // end of CURVEDUI_UNITY_XR if







    #region EVENT PROCESSING - GENERAL -------------------------------------------------------//
    /// <summary>
    /// Process() is called by UI system to process events 
    /// </summary>
    public override void Process()
    {
        switch (controlMethod)
        {
            case CUIControlMethod.MOUSE: base.Process(); break;
            case CUIControlMethod.GAZE: ProcessGaze(); break;
            case CUIControlMethod.STEAMVR_LEGACY: ProcessViveControllers(); break;
            case CUIControlMethod.STEAMVR_2: ProcessSteamVR2Controllers(); break;
            case CUIControlMethod.OCULUSVR: ProcessOculusVRController();break;
            case CUIControlMethod.CUSTOM_RAY:  ProcessCustomRayController(); break;
            //case CUIControlMethod.UNITY_XR: //-> Handled via different Process() when #CURVEDUI_UNITY_XR is defined
            case CUIControlMethod.WORLD_MOUSE:
            {
                //touch can also be used as a world space mouse,
                //although its probably not the best experience
                //Use standard mouse controller with touch.
                if (Input.touchCount > 0)
                    worldSpaceMouseOnCanvasDelta = Input.GetTouch(0).deltaPosition * worldSpaceMouseSensitivity;
                else {
                    worldSpaceMouseOnCanvasDelta = new Vector2((Input.mousePosition - lastMouseOnScreenPos).x, (Input.mousePosition - lastMouseOnScreenPos).y) * worldSpaceMouseSensitivity;
                    lastMouseOnScreenPos = Input.mousePosition;
                }
                lastWorldSpaceMouseOnCanvas = worldSpaceMouseInCanvasSpace;
                worldSpaceMouseInCanvasSpace += worldSpaceMouseOnCanvasDelta;

                base.Process();
                break;
            }
            default: goto case CUIControlMethod.MOUSE;
        }

        //save button pressed state for reference in next frame
        pressedLastFrame = pressedDown;
    }
    #endregion // EVENT PROCESSING - GENERAL ----------------------------------------------------//




    #region EVENT PROCESSING - GAZE -------------------------------------------------------------//
    protected virtual void ProcessGaze()
    {
        bool usedEvent = SendUpdateEventToSelectedObject();

        if (eventSystem.sendNavigationEvents)
        {
            if (!usedEvent) usedEvent |= SendMoveEventToSelectedObject();

            if (!usedEvent) SendSubmitEventToSelectedObject();
        }
        ProcessMouseEvent();
    }
    #endregion // EVENT PROCESSING - GAZE ---------------------------------------------------------//




    #region EVENT PROCESSING - CUSTOM RAY ---------------------------------------------------------//
    protected virtual void ProcessCustomRayController() {

        this.ProcessMouseEvent();
    }

    protected override MouseState GetMousePointerEventData(int id)
    {
        var ret = base.GetMousePointerEventData(id);

        if(ControlMethod != CUIControlMethod.MOUSE && ControlMethod != CUIControlMethod.WORLD_MOUSE)
            ret.SetButtonState(PointerEventData.InputButton.Left, CustomRayFramePressedState(), ret.GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData);

        return ret;
    }
     #endregion // end of EVENT PROCESSING - CUSTOM RAY -------------------------------------------//
    
#endif // end of CURVEDUI_UNITY_XR #else
    
    
    
    
    PointerEventData.FramePressState CustomRayFramePressedState()
    {
        if (pressedDown && !pressedLastFrame)
            return PointerEventData.FramePressState.Pressed;
        else if (!pressedDown && pressedLastFrame)
            return PointerEventData.FramePressState.Released;
        else return PointerEventData.FramePressState.NotChanged;
    }
    

    
    
    
    #region EVENT PROCESSING - STEAMVR LEGACY ---------------------------------------------------//
    protected virtual void ProcessViveControllers()
    {
#if CURVEDUI_STEAMVR_LEGACY
        switch (usedHand)
        {
            case Hand.Right:
            {
                //in case only one controller is turned on, it will still be used to call events.
                if (controllerManager.right.activeInHierarchy)
                    ProcessController(controllerManager.right);
                else if (controllerManager.left.activeInHierarchy)
                    ProcessController(controllerManager.left);
                break;
            }
            case Hand.Left:
            {
                //in case only one controller is turned on, it will still be used to call events.
                if (controllerManager.left.activeInHierarchy)
                    ProcessController(controllerManager.left);
                else if (controllerManager.right.activeInHierarchy)
                    ProcessController(controllerManager.right);
                break;
            }
            case Hand.Both:
            {
                ProcessController(controllerManager.left);
                ProcessController(controllerManager.right);
                break;
            }
            default: goto case Hand.Right;
        }
    }


    /// <summary>
    /// Processes Events from given controller.
    /// </summary>
    /// <param name="myController"></param>
    void ProcessController(GameObject myController)
	{
        //do not process events from this controller if it's off or not visible by base stations.
        if (!myController.gameObject.activeInHierarchy) return;

        //get the assistant or add it if its missing.
        CurvedUIViveController myControllerAssitant = myController.AddComponentIfMissing<CurvedUIViveController>();

        // send update events if there is a selected object - this is important for InputField to receive keyboard events
        SendUpdateEventToSelectedObject();

        // see if there is a UI element that is currently being pointed at
        PointerEventData ControllerData;
        if (myControllerAssitant == Right)
            ControllerData = GetControllerPointerData(myControllerAssitant, ref rightControllerData);
        else
            ControllerData = GetControllerPointerData(myControllerAssitant, ref leftControllerData);


        currentPointedAt = ControllerData.pointerCurrentRaycast.gameObject;

        ProcessDownRelease(ControllerData, myControllerAssitant.IsTriggerDown, myControllerAssitant.IsTriggerUp);

        //Process move and drag if trigger is pressed
        if (!myControllerAssitant.IsTriggerUp)
        {
            ProcessMove(ControllerData);
            ProcessDrag(ControllerData);
        }

        if (!Mathf.Approximately(ControllerData.scrollDelta.sqrMagnitude, 0.0f))
        {
            var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(ControllerData.pointerCurrentRaycast.gameObject);
            ExecuteEvents.ExecuteHierarchy(scrollHandler, ControllerData, ExecuteEvents.scrollHandler);
            // Debug.Log("executing scroll handler");
        }

    }

          /// <summary>
    /// Sends trigger down / trigger released events to gameobjects under the pointer.
    /// </summary>
    protected virtual void ProcessDownRelease(PointerEventData eventData, bool down, bool released)
    {
        var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (down)
        {
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.useDragThreshold = true;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, eventData);

            if (eventData.pointerEnter != currentOverGo)
            {
                // send a pointer enter to the touched element if it isn't the one to select...
                HandlePointerExitAndEnter(eventData, currentOverGo);
                eventData.pointerEnter = currentOverGo;
            }

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);


            float time = Time.unscaledTime;

            if (newPressed == eventData.lastPress)
            {
                var diffTime = time - eventData.clickTime;
                if (diffTime < 0.3f)
                    ++eventData.clickCount;
                else
                    eventData.clickCount = 1;

                eventData.clickTime = time;
            }
            else
            {
                eventData.clickCount = 1;
            }

            eventData.pointerPress = newPressed;
            eventData.rawPointerPress = currentOverGo;

            eventData.clickTime = time;

            // Save the drag handler as well
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }

        // PointerUp notification
        if (released)
        {
            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
                //Debug.Log("click");
            }
            else if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
                //Debug.Log("drop");
            }

            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;

            if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);
                //Debug.Log("end drag");
            }

            eventData.dragging = false;
            eventData.pointerDrag = null;

            // send exit events as we need to simulate this on touch up on touch device
            ExecuteEvents.ExecuteHierarchy(eventData.pointerEnter, eventData, ExecuteEvents.pointerExitHandler);
            eventData.pointerEnter = null;
        }
    }

    /// <summary>
    /// Create a pointerEventData that stores all the data associated with Vive controller.
    /// </summary>
    private CurvedUIPointerEventData GetControllerPointerData(CurvedUIViveController controller, ref CurvedUIPointerEventData ControllerData)
    {

        if (ControllerData == null)
            ControllerData = new CurvedUIPointerEventData(eventSystem);

        ControllerData.Reset();
        ControllerData.delta = Vector2.one; // to trick into moving
        ControllerData.position = Vector2.zero; // this will be overriden by raycaster
        ControllerData.Controller = controller.gameObject; // raycaster will use this object to override pointer position on screen. Keep it safe.
        ControllerData.scrollDelta = controller.TouchPadAxis - ControllerData.TouchPadAxis; // calcualte scroll delta
        ControllerData.TouchPadAxis = controller.TouchPadAxis; // assign finger position on touchpad

        eventSystem.RaycastAll(ControllerData, m_RaycastResultCache); //Raycast all the things!. Position will be overridden here by CurvedUIRaycaster

        //Get a current raycast to find if we're pointing at GUI object. 
        ControllerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();

        return ControllerData;
    }


    private bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
    {
        if (!useDragThreshold)
            return true;

        //this always returns false if override pointereventdata in curveduiraycster.cs is set to false. There is no past pointereventdata to compare with then.
        return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
    }

    /// <summary>
    /// Force selection of a gameobject.
    /// </summary>
    private void Select(GameObject go)
    {
        ClearSelection();
        if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
        {
            eventSystem.SetSelectedGameObject(go);
        }
    }

    /// <summary>
    /// Adds necessary components to Vive controller gameobjects. These will let us know what inputs are used on them.
    /// </summary>
    private void SetupViveControllers()
    {
        //find controller reference
        if (controllerManager == null)
                controllerManager = steamVRControllerManager;

        //Find Controller manager on the scene.
        if (controllerManager == null)
        {
            SteamVR_ControllerManager[] potentialManagers = Object.FindObjectsOfType<SteamVR_ControllerManager>();
            controllerManager = null;

            //ignore external camera created by externalcamera.cfg for mixed reality videos
            if (potentialManagers.GetLength(0) > 0)
            {
                for (int i = 0; i < potentialManagers.GetLength(0); i++)
                {
                    if (potentialManagers[i].gameObject.name != "External Camera")
                        controllerManager = potentialManagers[i];
                }
            }

            if (controllerManager == null)
                Debug.LogError("Can't find SteamVR_ControllerManager on scene. It is required to use VIVE control method. Make sure all SteamVR prefabs are present.");
        }
#endif
    }

    #endregion // end of EVENT PROCESSING - STEAMVR LEGACY ------------------------------------------//



    #region EVENT PROCESSING - OCULUS TOUCH ---------------------------------------------------------//
    protected virtual void ProcessOculusVRController()
    {
#if CURVEDUI_OCULUSVR
        activeCont = OVRInput.GetActiveController();

        //Find the currently used HandAnchor----------------------//
        //and set direction ray using its transform
        switch (activeCont)
        {
            //Oculus Touch
            case OVRInput.Controller.RTouch: CustomControllerRay = new Ray(oculusCameraRig.rightHandAnchor.position, oculusCameraRig.rightHandAnchor.forward); break;
            case OVRInput.Controller.LTouch: CustomControllerRay = new Ray(oculusCameraRig.leftHandAnchor.position, oculusCameraRig.leftHandAnchor.forward); break;
            //Oculus Go controller
            case OVRInput.Controller.RHand: goto case OVRInput.Controller.RTouch;
            case OVRInput.Controller.LHand: goto case OVRInput.Controller.LTouch;
            //edge cases
            default: CustomControllerRay = new Ray(OculusTouchUsedControllerTransform.position, OculusTouchUsedControllerTransform.forward); break;
        }

        //Check if interaction button is pressed ---------------//

        //find if we're using Rift with touch. If yes, we'll have to check if the interaction button is pressed on the proper hand.
        bool touchControllersUsed = (activeCont == OVRInput.Controller.Touch || activeCont == OVRInput.Controller.LTouch || activeCont == OVRInput.Controller.RTouch);

        if (usedHand == Hand.Both || !touchControllersUsed) 
        {
            //check if this button is pressed on any controller. Handles GearVR controller and Oculus Go controller.
            CustomControllerButtonState = OVRInput.Get(InteractionButton);
        }
        else if (usedHand == Hand.Right) // Right Oculus Touch
        {
            CustomControllerButtonState = OVRInput.Get(InteractionButton, OVRInput.Controller.RTouch);
        }
        else if (usedHand == Hand.Left)  // Left Oculus Touch
        {
            CustomControllerButtonState = OVRInput.Get(InteractionButton, OVRInput.Controller.LTouch);
        }

        //process all events based on this data--------------//
        ProcessCustomRayController();
#endif // end of CURVEDUI_OCULUSVR #if
    }
    #endregion // end of EVENT PROCESSING - OCULUS_VR ----------------------------------------------------//


    

    #region EVENT PROCESSING - STEAMVR_2 ----------------------------------------------------------------//
    void ProcessSteamVR2Controllers()
#if CURVEDUI_STEAMVR_2
    {
        if(m_steamVRClickAction != null)
        {
            CustomControllerButtonState = m_steamVRClickAction.GetState(SteamVRInputSource);
            CustomControllerRay = new Ray(ControllerTransform.transform.position, ControllerTransform.transform.forward);

            ProcessCustomRayController();
        }
        else
        {
            Debug.LogError("CURVEDUI: Choose which SteamVR_Action will be used for a Click on CurvedUISettings component.");
        }
    }

    void SetupSteamVR2Controllers()
    {
        if (steamVRPlayArea == null)
            steamVRPlayArea = FindObjectOfType<SteamVR_PlayArea>();

        if (steamVRPlayArea != null)
        {
            foreach (SteamVR_Behaviour_Pose poseComp in steamVRPlayArea.GetComponentsInChildren<SteamVR_Behaviour_Pose>(true))
            {
                if (poseComp.inputSource == SteamVR_Input_Sources.RightHand)
                    m_rightController = poseComp.gameObject;
                else if (poseComp.inputSource == SteamVR_Input_Sources.LeftHand)
                    m_leftController = poseComp.gameObject;
            }
        }
        else
        {
#if CURVEDUI_STEAMVR_INT
            //Optional - SteamVR Interaction System
            Valve.VR.InteractionSystem.Player PlayerComponent = FindObjectOfType<Valve.VR.InteractionSystem.Player>();

            if(PlayerComponent != null)
            {
                m_rightController = PlayerComponent.rightHand.gameObject;
                m_leftController = PlayerComponent.leftHand.gameObject;
            }
            else
#endif
                Debug.LogError("CURVEDUI: Can't find SteamVR_PlayArea component or InteractionSystem.Player component on the scene. One of these is required. Add a reference to it manually to CurvedUIInputModule on EventSystem gameobject.", this.gameObject);
        }

        if (m_steamVRClickAction == null)
            Debug.LogError("CURVEDUI: No SteamVR action to use for button interactions. Choose the action you want to use to click the buttons on CurvedUISettings component.");
    }

#else
    { }
#endif //end of CURVEDUI_STEAMVR_2 #if

#endregion // end of EVENT PROCESSING - STEAMVR_2 --------------------------------------------------------//
    
    

    









    #region HELPER FUNCTIONS ----------------------------------------------------------------------------//
    static T EnableInputModule<T>() where T : BaseInputModule
    {
        bool moduleMissing = true;
        EventSystem eventGO = GameObject.FindObjectOfType<EventSystem>();

        if (eventGO == null)
        {
            Debug.LogError("CurvedUI: Your EventSystem component is missing from the scene! Unity Canvas will not track interactions without it.");
            return null as T;
        }

        foreach (BaseInputModule module in eventGO.GetComponents<BaseInputModule>())
        {
            if (module is T) {
                moduleMissing = false;
                module.enabled = true;
            } else if (disableOtherInputModulesOnStart) {
                module.enabled = false;
            }
        }

        if (moduleMissing)
            eventGO.gameObject.AddComponent<T>();

        return eventGO.GetComponent<T>();
    }
    #endregion  // end of HELPER FUNCTIONS -------------------------------------------------------------//










    #region SETTERS AND GETTERS - GENERAL ------------------------------------------------------------//
    public static CurvedUIInputModule Instance
    {
        get {
            if (instance == null) instance = EnableInputModule<CurvedUIInputModule>();
            return instance;
        }
        private set => instance = value;
    }

    
    public static bool CanInstanceBeAccessed => Instance != null;

    
    /// <summary>
    /// Current controller mode. Decides how user can interact with the canvas. 
    /// </summary>
    public static CUIControlMethod ControlMethod
    {
        get { return Instance.controlMethod; }
        set
        {
            if (Instance.controlMethod != value)
            {
                Instance.controlMethod = value;
                #if CURVEDUI_STEAMVR_LEGACY
                if(value == CUIControlMethod.STEAMVR_LEGACY)
                    Instance.SetupViveControllers();
                #endif 
            }
        }
    }

    /// <summary>
    /// Layermask used by Raycaster classes to perform a Physics.Raycast() in order to find
    /// where user is pointing at the canvas.
    /// </summary>
    public LayerMask RaycastLayerMask
    {
        get { return raycastLayerMask; }
        set { raycastLayerMask = value; }
    }

    /// <summary>
    /// Which hand can be used to interact with canvas. Left, Right or Both. Default Right.
    /// Used in control methods that differentiate hands (STEAMVR, OCULUSVR)
    /// </summary>
    public Hand UsedHand
    {
        get { return usedHand; }
        set { usedHand = value; }
    }

    /// <summary>
    /// Gameobject of the handheld controller used for interactions - Oculus Touch, GearVR remote etc. 
    /// If ControllerTransformOverride is set, that transform will be returned instead.
    /// Used in STEAMVR, STEAMVR_LEGACY, OCULUSVR, UNITY_XR control methods.
    /// </summary>
    public Transform ControllerTransform {
        get
        {
            //use override, if available.
            if (PointerTransformOverride != null) return PointerTransformOverride;

            #if CURVEDUI_OCULUSVR
            return UsedHand == Hand.Left ? oculusCameraRig.leftHandAnchor : oculusCameraRig.rightHandAnchor; 
            #elif CURVEDUI_STEAMVR_LEGACY
            return UsedHand == Hand.Left ? leftCont.transform : rightCont.transform; 
            #elif CURVEDUI_STEAMVR_2
            return UsedHand == Hand.Left ? m_leftController.transform : m_rightController.transform;
            #elif CURVEDUI_UNITY_XR
            return UsedHand == Hand.Left ? leftXRController.transform : rightXRController.transform; 
            #else
            Debug.LogWarning("CURVEDUI: CurvedUIInputModule.ActiveController will only return proper gameobject in  STEAMVR, STEAMVR_LEGACY, OCULUSVR, UNITY_XR or GOOGLEVR control methods.");
            return null;
            #endif
        }
    }

    /// <summary>
    /// Direction where the handheld controller points. Forward (blue) direction of the controller transform.
    /// If ControllerTransformOverride is set, its forward direction will be returned instead.
    /// Used in STEAMVR, STEAMVR_LEGACY, OCULUSVR, UNITY_XR control methods.
    /// </summary>
    public Vector3 ControllerPointingDirection {
        get
        {
            #if  CURVEDUI_STEAMVR_LEGACY || CURVEDUI_STEAMVR_2 || CURVEDUI_OCULUSVR || CURVEDUI_UNITY_XR
            return ControllerTransform.forward;
            #else
            Debug.LogWarning("CURVEDUI: CurvedUIInputModule.PointingDirection will only return proper direction in  STEAMVR, STEAMVR_LEGACY, OCULUSVR, UNITY_XR control methods.");
            return Vector3.forward;
            #endif
        }
    }


    /// <summary>
    /// World Space position where the pointing ray starts. Usually the location of controller transform.
    /// If ControllerTransformOverride is set, its position will be returned instead.
    /// Used in STEAMVR, STEAMVR_LEGACY, OCULUSVR and UNITY_XR control methods.
    /// </summary>
    public Vector3 ControllerPointingOrigin {
        get
        {
            #if  CURVEDUI_STEAMVR_LEGACY || CURVEDUI_STEAMVR_2 || CURVEDUI_OCULUSVR || CURVEDUI_UNITY_XR
            return ControllerTransform.position;
            #else
            Debug.LogWarning("CURVEDUI: CurvedUIInputModule.PointingOrigin will only return proper position in  STEAMVR, STEAMVR_LEGACY, OCULUSVR, UNITY_XR control methods.");
            return Vector3.zero;
            #endif
        }
    }

    /// <summary>
    /// If not null, this transform will be used as the Pointer.
    /// Its position will be used as PointingOrigin and its forward (blue) direction as PointingDirection.
    /// </summary>
    public Transform PointerTransformOverride {
        get => instance.pointerTransformOverride;
        set => instance.pointerTransformOverride = value;
    }

    /// <summary>
    /// Gameobject we're currently pointing at.
    /// Updated every frame.
    /// </summary>
    public GameObject CurrentPointedAt => currentPointedAt;

    public Camera EventCamera {
        get => mainEventCamera;
        set
        {
            mainEventCamera = value;
            //add physics raycaster to event camera, so we can click on 3d objects
            if (mainEventCamera != null) mainEventCamera.AddComponentIfMissing<CurvedUIPhysicsRaycaster>();
        }
    }

    /// <summary>
    ///Get a ray to raycast with. Depends in EventCamera and current Control Method
    /// </summary>
    /// <returns></returns>
    public Ray GetEventRay(Camera eventCam = null) {

        if (eventCam == null) eventCam = mainEventCamera;

        switch (ControlMethod)
        {
            case CUIControlMethod.MOUSE:
            {
                // Get a ray from the camera through the point on the screen - used for mouse input
                return eventCam.ScreenPointToRay(MousePosition);
            }
            case CUIControlMethod.GAZE:
            {
                //get a ray from the center of world camera. used for gaze input
                return new Ray(eventCam.transform.position, eventCam.transform.forward);
            }
            //case CUIControlMethod.WORLD_MOUSE: //processed in CurvedUIRaycaster instead
            case CUIControlMethod.STEAMVR_LEGACY:
            {
                return pointerTransformOverride 
                    ? new Ray(pointerTransformOverride.position, pointerTransformOverride.forward) 
                    : new Ray(ControllerPointingOrigin, ControllerPointingDirection);
            }
            case CUIControlMethod.CUSTOM_RAY:
            {
                return pointerTransformOverride 
                    ? new Ray(pointerTransformOverride.position, pointerTransformOverride.forward) 
                    : CustomControllerRay;
            }
            case CUIControlMethod.STEAMVR_2: goto case CUIControlMethod.CUSTOM_RAY;
            case CUIControlMethod.OCULUSVR: goto case CUIControlMethod.CUSTOM_RAY;
            case CUIControlMethod.UNITY_XR: goto case CUIControlMethod.CUSTOM_RAY;
            default: goto case CUIControlMethod.CUSTOM_RAY;
        }
    }
    
    /// <summary>
    /// What is the mouse position on screen now? Returns value from old or new Input System.
    /// WARNING: Unity reports wrong on-screen mouse position if a VR headset is connected.
    /// </summary>
    public static Vector2 MousePosition => 
        #if CURVEDUI_UNITY_XR
        Mouse.current.position.ReadValue();
        #else
        new Vector2(Input.mousePosition.x,Input.mousePosition.y);
        #endif

    /// <summary>
    /// Is left mouse button pressed now? Returns value from old or new Input System.
    /// </summary>
    public static bool LeftMouseButton =>
        #if CURVEDUI_UNITY_XR
        Mouse.current.leftButton.isPressed;
        #else
        Input.GetButton("Fire1");
        #endif
    
    
    
 #endregion // end of SETTERS AND GETTERS - GENERAL region ---------------------------------------------//






    #region SETTERS AND GETTERS - CUSTOM RAY
    /// <summary>
    /// When in CUSTOM_RAY controller mode, Canvas Raycaster will use this worldspace Ray to determine which Canvas objects are being selected.
    /// </summary>
    public static Ray CustomControllerRay
    {
        get => Instance.customControllerRay;
        set => Instance.customControllerRay = value;
    }

    /// <summary>
    /// Tell CurvedUI if controller button is pressed when in CUSTOM_RAY controller mode. Input module will use this to interact with canvas.
    /// </summary>
    public static bool CustomControllerButtonState
    {
        get => Instance.pressedDown;
        set => Instance.pressedDown = value;
    }
    
    [Obsolete("Use " + nameof(CustomControllerButtonState) + " instead.")]
    public static bool CustomControllerButtonDown
    {
        get => CustomControllerButtonState;
        set => CustomControllerButtonState = value;
    }
    #endregion






    #region SETTERS AND GETTERS - WORLD SPACE MOUSE
    /// <summary>
    /// Returns the position of the world space pointer in Canvas' local space. 
    /// You can use it to position an image on world space mouse pointer's position.
    /// </summary>
    public Vector2 WorldSpaceMouseInCanvasSpace
    {
        get => worldSpaceMouseInCanvasSpace;
        set
        {
            worldSpaceMouseInCanvasSpace = value;
            lastWorldSpaceMouseOnCanvas = value;
        }
    }

    /// <summary>
    /// The change in position of the world space mouse in canvas' units.
    /// Counted since the last frame.
    /// </summary>
    public Vector2 WorldSpaceMouseInCanvasSpaceDelta => worldSpaceMouseInCanvasSpace - lastWorldSpaceMouseOnCanvas;

    /// <summary>
    /// How many units in Canvas space equals one unit in screen space.
    /// </summary>
    public float WorldSpaceMouseSensitivity
    {
        get => worldSpaceMouseSensitivity;
        set => worldSpaceMouseSensitivity = value;
    }
    #endregion // SETTERS AND GETTERS - WORLD SPACE MOUSE





    #region SETTERS AND GETTERS - GAZE
    /// <summary>
    /// Gaze Control Method. Should execute OnClick events on button after user points at them?
    /// </summary>
    public bool GazeUseTimedClick
    {
        get => gazeUseTimedClick;
        set => gazeUseTimedClick = value;
    }

    /// <summary>
    /// Gaze Control Method. How long after user points on a button should we click it? Default 2 seconds.
    /// </summary>
    public float GazeClickTimer
    {
        get => gazeClickTimer;
        set => gazeClickTimer = Mathf.Max(value, 0);
    }

    /// <summary>
    /// Gaze Control Method. How long after user looks at a button should we start the timer? Default 1 second.
    /// </summary>
    public float GazeClickTimerDelay
    {
        get => gazeClickTimerDelay;
        set => gazeClickTimerDelay = Mathf.Max(value, 0);
    }

    /// <summary>
    /// Gaze Control Method. How long till Click method is executed on Buttons under gaze? Goes 0-1.
    /// </summary>
    public float GazeTimerProgress => gazeTimerProgress;

    /// <summary>
    /// Gaze Control Method. This Images's fill will be animated 0-1 when OnClick events are about
    /// to be executed on buttons under the gaze.
    /// </summary>
    public Image GazeTimedClickProgressImage
    {
        get => gazeTimedClickProgressImage;
        set => gazeTimedClickProgressImage = value;
    }
    #endregion // SETTERS AND GETTERS - GAZE






    #region SETTERS AND GETTERS - STEAMVR_LEGACY
#if CURVEDUI_STEAMVR_LEGACY
    /// <summary>
    /// Scene's controller manager. Used to get references for Vive controllers.
    /// </summary>
    public SteamVR_ControllerManager SteamVRControllerManager {
            get { return steamVRControllerManager; }
            set {
                if (steamVRControllerManager != value)  {
                    steamVRControllerManager = value;
                }
            }
        }

        /// <summary>
        /// Get or Set controller manager used by this input module.
        /// </summary>
        public SteamVR_ControllerManager ControllerManager {
            get { return controllerManager; }
            set {
                controllerManager = value;
                SetupViveControllers();
            }
        }
   
        /// <summary>
        /// Returns Right SteamVR Controller. Ask this component for any button states.;
        /// </summary>
        public static CurvedUIViveController Right {
            get {
                if (!rightCont) rightCont = controllerManager.right.AddComponentIfMissing<CurvedUIViveController>();
                return rightCont ; 
            }
        }

        /// <summary>
        /// Returns Left SteamVR Controller. Ask this component for any button states.;
        /// </summary>
        public static CurvedUIViveController Left {
            get {
                if (!leftCont) leftCont = controllerManager.left.AddComponentIfMissing<CurvedUIViveController>();
                return leftCont; 
            }
        }  
#endif // CURVEDUI_STEAMVR_LEGACY
    #endregion // end of SETTERS AND GETTERS - STEAMVR_LEGACY






    #region SETTERS AND GETTERS - STEAMVR_2
#if CURVEDUI_STEAMVR_2
    public SteamVR_PlayArea SteamVRPlayArea {
        get { return steamVRPlayArea; }
        set { steamVRPlayArea = value; }
    }

    /// <summary>
    /// Currently used SteamVR Input Source, based on used Hand.
    /// </summary>
    public SteamVR_Input_Sources SteamVRInputSource {
        get {  return (UsedHand == Hand.Left ? Valve.VR.SteamVR_Input_Sources.LeftHand : Valve.VR.SteamVR_Input_Sources.RightHand); }
    }

    /// <summary>
    /// SteamVR 2.0 Action that should be used to click on UI elements.
    /// </summary>
    public SteamVR_Action_Boolean SteamVRClickAction {
        get { return m_steamVRClickAction;  }
        set { m_steamVRClickAction = value; }
    }
#endif // end of STEAMVR2
    #endregion // SETTERS AND GETTERS - STEAMVR_2





    #region SETTERS AND GETTERS - OCULUSVR
#if CURVEDUI_OCULUSVR
    public OVRCameraRig OculusCameraRig {
        get => oculusCameraRig;
        set => oculusCameraRig = value;
    }

    public OVRInput.Button OculusTouchInteractionButton {
        get => InteractionButton;
        set => InteractionButton = value;
    }

    public Transform OculusTouchUsedControllerTransform 
        => UsedHand == Hand.Left ? oculusCameraRig.leftHandAnchor : oculusCameraRig.rightHandAnchor;
#endif // end of CURVEDUI_OCULUSVR
    #endregion // SETTERS AND GETTERS - OCULUSVR
    
    


    #region SETTERS AND GETTERS - UNITY_XR
#if CURVEDUI_UNITY_XR
    public XRBaseController RightXRController {
        get => rightXRController;
        set => rightXRController = value;
    }

    public XRBaseController LeftXRController {
        get => leftXRController;
        set => leftXRController = value;
    }
#endif // end of CURVEDUI_UNITY_XR
    #endregion // SETTERS AND GETTERS - UNITY_XR
    
    


    #region ENUMS
    public enum CUIControlMethod
    {
	    MOUSE = 0,
	    GAZE = 1,
	    WORLD_MOUSE = 2,
	    CUSTOM_RAY = 3,
	    STEAMVR_LEGACY = 4, //SDK version 1.2.3 or earlier
	    OCULUSVR = 5,
	    //DAYDREAM = 6, //deprecated, GoogleVR is now used for daydream.
	    //GOOGLEVR = 7, //deprecated, use GAZE or CUSTOM_RAY
        STEAMVR_2 = 8, //SDK version 2.0 or later
        UNITY_XR = 9, // Requires XR Interaction 1.0.0pre3 or later
    }

    public enum Hand
    {
	    Both = 0,
	    Right = 1,
	    Left = 2,
    }
    #endregion // ENUMS

}
