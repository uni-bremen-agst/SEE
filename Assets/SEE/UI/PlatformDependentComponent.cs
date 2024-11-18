using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreLinq;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SEE.UI
{
    /// <summary>
    /// This class represents a component whose Start() and Update() method differs based on the current platform.
    /// Inheritors are expected to override the respective Start() and Update() methods (e.g. <see cref="StartVR()"/>.
    /// If the current platform's start method was not overridden, the component will be destroyed.
    /// If the current platform's update method was not overridden, nothing will happen.
    ///
    /// This approach is especially well suited for UI components, as their presentation is almost always different
    /// based on the platform.
    /// </summary>
    public abstract class PlatformDependentComponent : MonoBehaviour
    {
        /// <summary>
        /// The folder where to find UI Prefabs.
        /// </summary>
        protected const string UIPrefabFolder = "Prefabs/UI/";

        /// <summary>
        /// Name of the canvas on which UI elements are placed.
        /// </summary>
        private const string uiCanvasName = "UI Canvas";

        /// <summary>
        /// Path to where the UI Canvas prefab is stored.
        /// This prefab should contain all components necessary for the UI canvas, such as an event system,
        /// a graphic raycaster, etc.
        /// </summary>
        private const string uiCanvasPrefab = UIPrefabFolder + "UICanvas";

        /// <summary>
        /// The canvas on which UI elements are placed.
        /// </summary>
        protected static GameObject Canvas { get; private set; }

        /// <summary>
        /// The current platform.
        /// </summary>
        protected PlayerInputType Platform { get; private set; }

        /// <summary>
        /// Whether the component is initialized.
        /// </summary>
        /// <see cref="Start"/>
        protected bool HasStarted { get; private set; }

        /// <summary>
        /// Properties of this class that have the <see cref="ManagedUIAttribute"/>.
        /// </summary>
        private readonly IList<PropertyInfo> managedUIProperties;

        /// <summary>
        /// Fields of this class that have the <see cref="ManagedUIAttribute"/>.
        /// </summary>
        private readonly IList<FieldInfo> managedUIFields;

        /// <summary>
        /// A cached list of Unity <see cref="Object"/>s that are members of this class,
        /// and have the <see cref="ManagedUIAttribute"/> with <see cref="ManagedUIAttribute.Integral"/> set to true.
        ///
        /// We assume that these objects are not re-assigned and that we can hence save them in this list,
        /// with the purpose of not needing a costly reflection call with each <see cref="Update"/> frame.
        /// </summary>
        private IList<Object> integralUIObjects;

        /// <summary>
        /// The number of frames that have passed since the component was started.
        /// </summary>
        protected uint Frames { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformDependentComponent"/> class
        /// by assigning the managed UI properties and fields.
        /// </summary>
        /// <remarks>
        /// Actual setup of the component should be done in the <see cref="Start"/> or <see cref="Awake"/> method,
        /// as per Unity documentation of <see cref="MonoBehaviour"/>s. This constructor is only used to set up
        /// the managed UI properties and fields, which should happen only once and as early as possible.
        /// </remarks>
        protected PlatformDependentComponent()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            managedUIProperties = GetType().GetProperties(flags).Where(IsManaged).ToList();
            managedUIFields = GetType().GetFields(flags).Where(IsManaged).ToList();
        }

        /// <summary>
        /// Returns a list of Unity <see cref="Object"/>s that are members of this class,
        /// have the <see cref="ManagedUIAttribute"/>, and match the given <paramref name="predicate"/>, if given.
        /// </summary>
        /// <param name="predicate">A predicate that managed UI objects have to match.</param>
        /// <returns>A list of Unity <see cref="Object"/>s that are managed UI objects.</returns>
        private IEnumerable<Object> GetManagedUIObjects(Predicate<ManagedUIAttribute> predicate = null)
        {
            predicate ??= _ => true;
            return managedUIFields.Where(x => predicate(ManagedUIAttribute.FromMember(x)))
                                  .Select(x => x.GetValue(this))
                                  .Concat(managedUIProperties.Select(x => x.GetValue(this)))
                                  .SelectMany(GetUnityObjects)
                                  .Where(x => x != null);
        }

        /// <summary>
        /// Whether the given <paramref name="member"/> is managed by this component,
        /// that is, whether it has the <see cref="ManagedUIAttribute"/>.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>Whether the given <paramref name="member"/> is managed by this component.</returns>
        private static bool IsManaged(MemberInfo member) => Attribute.IsDefined(member, typeof(ManagedUIAttribute));

        /// <summary>
        /// Returns Unity <see cref="Object"/>s from the given <paramref name="obj"/>.
        /// The given <paramref name="obj"/> can be a single <see cref="Object"/> or an <see cref="IEnumerable{T}"/>
        /// of such objects. If it is <c>null</c>, we return an empty list. In all other cases, we throw an exception.
        /// </summary>
        /// <param name="obj">The object to get Unity <see cref="Object"/>s from.</param>
        /// <returns>Unity <see cref="Object"/>s from the given <paramref name="obj"/>.</returns>
        private static IEnumerable<Object> GetUnityObjects(object obj)
        {
            return obj switch
            {
                Object unityObject => new[] { unityObject },
                IEnumerable<Object> objs => objs, // IEnumerable is covariant, so this is safe
                null => new Object[] { },
                _ => throw new($"Member {obj} is a {obj.GetType()}, not a (Unity) Object nor IEnumerable<Object>," +
                               " even though it has the ManagedUIAttribute.")
            };
        }

        /// <summary>
        /// Initializes the component for the current platform.
        /// </summary>
        /// <remarks>
        /// Only override this if you need to execute platform-independent code on startup.
        /// Otherwise, use <see cref="StartDesktop"/>, <see cref="StartVR"/> or <see cref="StartTouchGamepad"/>.
        /// </remarks>
        protected virtual void Start()
        {
            // initializes the Canvas if necessary
            if (Canvas == null)
            {
                // TODO: Is it needed to search for the UI canvas?
                // The canvas is now static and nobody else should instantiate the canvas...
                Canvas = GameObject.Find(uiCanvasName) ?? PrefabInstantiator.InstantiatePrefab(uiCanvasPrefab);
                Canvas.name = uiCanvasName;
            }

            // calls the start method for the current platform
            Platform = SceneSettings.InputType;
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    StartDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    StartTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer:
                    StartVR();
                    //TODO: Apply CurvedUI to canvas
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
                    break;
            }

            // initialization finished
            HasStarted = true;
            OnStartFinished();
            OnComponentInitialized?.Invoke();

            // We assume that integral properties and fields do not get re-assigned, otherwise the performance
            // penalty may be too high. For that reason, we cache the integral UI objects here.
            IEnumerable<PropertyInfo> integralUiProperties = managedUIProperties.Where(x => ManagedUIAttribute.FromMember(x).Integral);
            IEnumerable<FieldInfo> integralUiFields = managedUIFields.Where(x => ManagedUIAttribute.FromMember(x).Integral);
            integralUIObjects = integralUiFields.Select(x => x.GetValue(this))
                                                .Concat(integralUiProperties.Select(x => x.GetValue(this)))
                                                .SelectMany(GetUnityObjects).ToList();
        }

        /// <summary>
        /// Updates the component for the current platform.
        /// </summary>
        protected virtual void Update()
        {
            // We only do this expensive check every 50 frames.
            if (++Frames % 50 == 0 && integralUIObjects.Any(x => x == null))
            {
                Destroyer.Destroy(this);
                return;
            }
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    UpdateDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    UpdateTouchGamepad();
                    break;
                case PlayerInputType.VRPlayer:
                    UpdateVR();
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
                    break;
            }
        }

        /// <summary>
        /// Destroys the component and all managed UI objects.
        /// </summary>
        protected virtual void OnDestroy()
        {
            GetManagedUIObjects(x => x.Destroy).ForEach(Destroyer.Destroy);
        }

        /// <summary>
        /// Disables the component and all managed UI objects.
        /// </summary>
        protected virtual void OnDisable() => ToggleManaged(false);

        /// <summary>
        /// Enables the component and all managed UI objects.
        /// </summary>
        protected virtual void OnEnable() => ToggleManaged(true);

        /// <summary>
        /// Enables/disables all managed UI objects depending on the given <paramref name="enable"/> parameter.
        /// This works on both GameObjects and Behaviours (including <see cref="MonoBehaviour"/>).
        /// </summary>
        /// <param name="enable">Whether to enable or disable the managed UI objects.</param>
        private void ToggleManaged(bool enable)
        {
            foreach (Object managedUiObject in GetManagedUIObjects(x => x.ToggleEnabled))
            {
                switch (managedUiObject)
                {
                    case GameObject go:
                        go.SetActive(enable);
                        break;
                    case Behaviour behaviour:
                        behaviour.enabled = enable;
                        break;
                    default:
                        throw new($"Managed UI object {managedUiObject} is neither a GameObject nor Behaviour,"
                                  + " even though it has the ManagedUIAttribute with ToggleEnabled set to true.");
                }
            }
        }

        /// <summary>
        /// Logs an error with information about this platform and component and destroys this component.
        /// </summary>
        protected void PlatformUnsupported()
        {
            Debug.LogError($"Component '{GetType()}' doesn't support platform '{Platform.ToString()}'."
                           + " Component will now self-destruct.");
            Destroyer.Destroy(this);
        }

        /// <summary>
        /// Start method for the Desktop platform.
        /// </summary>
        protected virtual void StartDesktop() => PlatformUnsupported();

        /// <summary>
        /// Start method for the VR platform.
        /// </summary>
        protected virtual void StartVR() => PlatformUnsupported();

        /// <summary>
        /// Start method for the TouchGamepad platform.
        /// </summary>
        protected virtual void StartTouchGamepad() => PlatformUnsupported();

        /// <summary>
        /// Update method for the Desktop platform.
        /// </summary>
        protected virtual void UpdateDesktop() { }

        /// <summary>
        /// Update method for the VR platform.
        /// </summary>
        protected virtual void UpdateVR() { }

        /// <summary>
        /// Update method for the TouchGamepad platform.
        /// </summary>
        protected virtual void UpdateTouchGamepad() { }

        /// <summary>
        /// Triggered when the component was started. (<see cref="Start"/>)
        /// Can be used add listeners and update UI after initialization.
        /// </summary>
        protected virtual void OnStartFinished() { }

        /// <summary>
        /// Triggers when the component has been initialized, i.e., when the Start() method has been called.
        /// </summary>
        public event UnityAction OnComponentInitialized;

        /// <summary>
        /// An attribute on a Unity <see cref="Object"/> or enumerable of such objects that tells us that the
        /// object is managed by the <see cref="PlatformDependentComponent"/> it is contained in.
        ///
        /// This "management" of contained objects can mean different things, depending on the parameters of the attribute:
        /// <ul>
        /// <li>
        ///      <see cref="Destroy"/>: Whether the object should be destroyed when the object this attribute
        ///      is on is destroyed.
        /// </li>
        /// <li>
        ///     <see cref="Integral"/>: Whether the surrounding <see cref="PlatformDependentComponent"/>
        ///     should be destroyed when the object this attribute is on is destroyed.
        /// </li>
        /// <li>
        ///     <see cref="ToggleEnabled"/>: Whether the object should be enabled/disabled when the
        ///     surrounding <see cref="PlatformDependentComponent"/> is enabled/disabled.
        /// </li>
        /// </ul>
        ///
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        protected class ManagedUIAttribute : Attribute
        {
            /// <summary>
            /// Whether the object this attribute is on has a composition relation to the
            /// <see cref="PlatformDependentComponent"/> it is contained in, that is, whether this object should be
            /// destroyed when the <see cref="PlatformDependentComponent"/> is destroyed.
            /// </summary>
            public readonly bool Destroy;

            /// <summary>
            /// Whether this <see cref="PlatformDependentComponent"/> also has a composition relation
            /// to the UI object this attribute is on, that is, whether this <see cref="PlatformDependentComponent"/>
            /// should be destroyed when the object this attribute is on is destroyed.
            /// </summary>
            /// <remarks>
            /// If this is set to <c>true</c>, we will need to check for destruction of the managed UI objects
            /// in every <see cref="Update"/> frame, which may be costly. For this reason, we cache the integral UI objects
            /// on <see cref="Start"/>, meaning that <em>they must not be re-assigned outside of <see cref="Start"/>!</em>
            /// </remarks>
            public readonly bool Integral;

            /// <summary>
            /// Whether the object this attribute is on should be enabled/disabled when the
            /// surrounding <see cref="PlatformDependentComponent"/> is enabled/disabled.
            /// </summary>
            public readonly bool ToggleEnabled;

            /// <summary>
            /// Marks a Unity <see cref="Object"/> or enumerable of such objects as managed by the
            /// <see cref="PlatformDependentComponent"/> it is contained in.
            /// </summary>
            /// <param name="destroy">Whether the object should be destroyed when the <see cref="PlatformDependentComponent"/>
            /// is destroyed.</param>
            /// <param name="integral">Whether the <see cref="PlatformDependentComponent"/> should be destroyed when
            /// the object this attribute is on is destroyed.</param>
            /// <param name="toggleEnabled">Whether the object should be enabled/disabled when the
            /// <see cref="PlatformDependentComponent"/> is enabled/disabled.</param>
            /// <remarks>
            /// Note that, if <paramref name="integral"/> is set to <c>true</c>, the corresponding GameObject must not
            /// be re-assigned outside of the <see cref="Start"/> method.
            /// </remarks>
            public ManagedUIAttribute(bool destroy = true, bool integral = false, bool toggleEnabled = false)
            {
                Integral = integral;
                Destroy = destroy;
                // TODO: Should the default value for ToggleEnabled be true or false?
                ToggleEnabled = toggleEnabled;
            }

            /// <summary>
            /// Returns the <see cref="ManagedUIAttribute"/> of the given <paramref name="member"/>.
            /// Will throw an exception if there is no such attribute.
            /// </summary>
            /// <param name="member">The member to get the <see cref="ManagedUIAttribute"/> from.</param>
            /// <returns>The <see cref="ManagedUIAttribute"/> of the given <paramref name="member"/>.</returns>
            public static ManagedUIAttribute FromMember(MemberInfo member)
            {
                return (ManagedUIAttribute)GetCustomAttribute(member, typeof(ManagedUIAttribute));
            }
        }
    }
}
