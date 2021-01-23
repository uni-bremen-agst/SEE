using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Implements the behavior of a menu entry. A menu entry can be transient
    /// or persistent. A persistent menu entry remains active until it is 
    /// explicitly deactivated. A transient menu entry remains active until it is 
    /// explicitly deactivated or until a certain amount of time has passed since
    /// the point in time when it was activated. Active and inactive menu entries
    /// have different colors. Delegates will be called when a menu entry is
    /// activated or deactivated.
    /// </summary>
    public class MenuEntry : MonoBehaviour
    {
        /// <summary>
        /// The duration in seconds between a transient activated menu entry is to
        /// be automatically deactivated again.
        /// </summary>
        public static float ActivationDuration = 0.5f;

        /// <summary>
        /// Whether this menu entry is transient. Transient menu entries
        /// last only for <see cref="ActivationDuration"/> and then 
        /// automatically switch back from active to inactive.
        /// </summary>
        public bool IsTransient = false;

        /// <summary>
        /// The color to be used when the menu entry is active.
        /// </summary>
        private Color activeColor;

        /// <summary>
        /// The color to be used when the menu entry is active.
        /// Setting this value will immediately turn on the assigned
        /// color. The activation state itself, however, will not be
        /// changed.
        /// </summary>
        public Color ActiveColor
        {
            get => activeColor;
            set
            {
                activeColor = value;
                SetColor();
            }
        }

        /// <summary>
        /// The color to be used when the menu entry is inactive.
        /// </summary>
        private Color inactiveColor;

        /// <summary>
        /// The color to be used when the menu entry is inactive.
        /// Setting this value will immediately turn on the assigned
        /// color. The activation state itself, however, will not be
        /// changed.
        /// </summary>
        public Color InactiveColor
        {
            get => inactiveColor;
            set
            {
                inactiveColor = value;
                SetColor();
            }
        }

        /// <summary>
        /// Sets the color of the sprite to either <see cref="activeColor"/>
        /// or <see cref="inactiveColor"/> depending on <see cref="isActive"/>.
        /// </summary>
        private void SetColor()
        {
            if (TryGetComponent<SpriteRenderer>(out SpriteRenderer renderer))
            {                
                renderer.color = isActive ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// Whether this menu entry is currently active.
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// Whether this menu entry is currently active. If set to true, the 
        /// color of the sprite will be adapated to <see cref="ActiveColor"/>
        /// and <see cref="EntryOn"/> will be invoked (if defined). If set to false,
        /// the color of the sprite will be adapated to <see cref="InactiveColor"/>
        /// and <see cref="EntryOff"/> will be invoked (if defined).
        /// </summary>
        public bool Active
        {
            get => isActive;
            set
            {
                isActive = value;
                SetColor();
                if (isActive)
                {
                    timer = ActivationDuration;
                    EntryOn?.Invoke();
                }
                else
                {
                    timer = 0.0f;
                    EntryOff?.Invoke();
                }
            }
        }

        /// <summary>
        /// Called when this menu entry is selected, in which case its activation
        /// state will be toggled.
        /// </summary>
        public void Selected()
        {
            Active = !isActive;
        }

        /// <summary>
        /// The delegate to be called whenever the menu entry is activated
        /// by way of assigning <code>true</code> to <see cref="Active"/>.
        /// May or may not be null.
        /// </summary>
        public EntryEvent EntryOn;

        /// <summary>
        /// The delegate to be called whenever the menu entry is deactivated
        /// by way of assigning <code>false</code> to <see cref="Active"/>
        /// or - in case the menu entry is transient - after the duration
        /// since the last activation has exceeded <see cref="ActivationDuration"/>.
        /// May or may not be null.
        /// </summary>
        public EntryEvent EntryOff;

        /// <summary>
        /// The time in seconds until a transient activated menu entry needs to
        /// be deactivated again. The time is running down.
        /// </summary>
        private float timer = 0.0f;

        /// <summary>
        /// If the menu entry is transient and currently activated, <see cref="timer"/>
        /// will be decreased by the amount of time since the last frame. If that 
        /// <see cref="timer"/> reaches 0 (or becomes negative), the menu entry will
        /// be deactivated.
        /// </summary>
        private void Update()
        {
            if (IsTransient && isActive)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    Active = false;
                }
            }
        }
    }
}