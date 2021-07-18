// GENERATED AUTOMATICALLY FROM 'Assets/SEE/Controls/Actions/Architecture/ArchitectureInputActions.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @ArchitectureInputActions : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @ArchitectureInputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ArchitectureInputActions"",
    ""maps"": [
        {
            ""name"": ""Drawing"",
            ""id"": ""225f8ad6-74ba-4da4-9a5c-7879960653a9"",
            ""actions"": [
                {
                    ""name"": ""DrawBegin"",
                    ""type"": ""Button"",
                    ""id"": ""1fb461b5-4de4-4c11-876f-bbf1e505ae9b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Draw"",
                    ""type"": ""Button"",
                    ""id"": ""083075a2-a2b0-46e6-b52e-3544985e1e10"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""DrawEnd"",
                    ""type"": ""Button"",
                    ""id"": ""4a4bcabd-3287-4a09-83e2-05168d2d4b34"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Position"",
                    ""type"": ""PassThrough"",
                    ""id"": ""aa2a82f9-3873-4c4c-915f-112fae8a73d3"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""5fb2fa56-1759-4561-b745-96b36cd8c73e"",
                    ""path"": ""<Pen>/tip"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DrawBegin"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a30bee44-b658-44e3-aa81-b6d92cda8153"",
                    ""path"": ""<Pen>/press"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Draw"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""38923bcf-5209-462a-b9c7-92a1aa5b7510"",
                    ""path"": ""<Pen>/press"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DrawEnd"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f7db42d0-f682-4119-bde0-216abf02df4a"",
                    ""path"": ""<Pen>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Position"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Moving"",
            ""id"": ""7cd3705f-3a15-448e-8adf-1903a0b553dc"",
            ""actions"": [
                {
                    ""name"": ""New action"",
                    ""type"": ""Button"",
                    ""id"": ""84c8b9c7-0427-4232-bcb0-1b0d323f6afb"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""e42dabbe-cf15-4459-bc7c-ad8b660020f6"",
                    ""path"": """",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""New action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Drawing
        m_Drawing = asset.FindActionMap("Drawing", throwIfNotFound: true);
        m_Drawing_DrawBegin = m_Drawing.FindAction("DrawBegin", throwIfNotFound: true);
        m_Drawing_Draw = m_Drawing.FindAction("Draw", throwIfNotFound: true);
        m_Drawing_DrawEnd = m_Drawing.FindAction("DrawEnd", throwIfNotFound: true);
        m_Drawing_Position = m_Drawing.FindAction("Position", throwIfNotFound: true);
        // Moving
        m_Moving = asset.FindActionMap("Moving", throwIfNotFound: true);
        m_Moving_Newaction = m_Moving.FindAction("New action", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Drawing
    private readonly InputActionMap m_Drawing;
    private IDrawingActions m_DrawingActionsCallbackInterface;
    private readonly InputAction m_Drawing_DrawBegin;
    private readonly InputAction m_Drawing_Draw;
    private readonly InputAction m_Drawing_DrawEnd;
    private readonly InputAction m_Drawing_Position;
    public struct DrawingActions
    {
        private @ArchitectureInputActions m_Wrapper;
        public DrawingActions(@ArchitectureInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @DrawBegin => m_Wrapper.m_Drawing_DrawBegin;
        public InputAction @Draw => m_Wrapper.m_Drawing_Draw;
        public InputAction @DrawEnd => m_Wrapper.m_Drawing_DrawEnd;
        public InputAction @Position => m_Wrapper.m_Drawing_Position;
        public InputActionMap Get() { return m_Wrapper.m_Drawing; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(DrawingActions set) { return set.Get(); }
        public void SetCallbacks(IDrawingActions instance)
        {
            if (m_Wrapper.m_DrawingActionsCallbackInterface != null)
            {
                @DrawBegin.started -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawBegin;
                @DrawBegin.performed -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawBegin;
                @DrawBegin.canceled -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawBegin;
                @Draw.started -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDraw;
                @Draw.performed -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDraw;
                @Draw.canceled -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDraw;
                @DrawEnd.started -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawEnd;
                @DrawEnd.performed -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawEnd;
                @DrawEnd.canceled -= m_Wrapper.m_DrawingActionsCallbackInterface.OnDrawEnd;
                @Position.started -= m_Wrapper.m_DrawingActionsCallbackInterface.OnPosition;
                @Position.performed -= m_Wrapper.m_DrawingActionsCallbackInterface.OnPosition;
                @Position.canceled -= m_Wrapper.m_DrawingActionsCallbackInterface.OnPosition;
            }
            m_Wrapper.m_DrawingActionsCallbackInterface = instance;
            if (instance != null)
            {
                @DrawBegin.started += instance.OnDrawBegin;
                @DrawBegin.performed += instance.OnDrawBegin;
                @DrawBegin.canceled += instance.OnDrawBegin;
                @Draw.started += instance.OnDraw;
                @Draw.performed += instance.OnDraw;
                @Draw.canceled += instance.OnDraw;
                @DrawEnd.started += instance.OnDrawEnd;
                @DrawEnd.performed += instance.OnDrawEnd;
                @DrawEnd.canceled += instance.OnDrawEnd;
                @Position.started += instance.OnPosition;
                @Position.performed += instance.OnPosition;
                @Position.canceled += instance.OnPosition;
            }
        }
    }
    public DrawingActions @Drawing => new DrawingActions(this);

    // Moving
    private readonly InputActionMap m_Moving;
    private IMovingActions m_MovingActionsCallbackInterface;
    private readonly InputAction m_Moving_Newaction;
    public struct MovingActions
    {
        private @ArchitectureInputActions m_Wrapper;
        public MovingActions(@ArchitectureInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Newaction => m_Wrapper.m_Moving_Newaction;
        public InputActionMap Get() { return m_Wrapper.m_Moving; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(MovingActions set) { return set.Get(); }
        public void SetCallbacks(IMovingActions instance)
        {
            if (m_Wrapper.m_MovingActionsCallbackInterface != null)
            {
                @Newaction.started -= m_Wrapper.m_MovingActionsCallbackInterface.OnNewaction;
                @Newaction.performed -= m_Wrapper.m_MovingActionsCallbackInterface.OnNewaction;
                @Newaction.canceled -= m_Wrapper.m_MovingActionsCallbackInterface.OnNewaction;
            }
            m_Wrapper.m_MovingActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Newaction.started += instance.OnNewaction;
                @Newaction.performed += instance.OnNewaction;
                @Newaction.canceled += instance.OnNewaction;
            }
        }
    }
    public MovingActions @Moving => new MovingActions(this);
    public interface IDrawingActions
    {
        void OnDrawBegin(InputAction.CallbackContext context);
        void OnDraw(InputAction.CallbackContext context);
        void OnDrawEnd(InputAction.CallbackContext context);
        void OnPosition(InputAction.CallbackContext context);
    }
    public interface IMovingActions
    {
        void OnNewaction(InputAction.CallbackContext context);
    }
}
