using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SEE.Utils
{
    /// <summary>
    /// Provides events about pointer behaviour to the outside.
    /// Similar to: https://docs.unity3d.com/2018.1/Documentation/ScriptReference/EventSystems.IPointerEnterHandler.html
    ///
    /// While an <see cref="EventTrigger"/> could also be used, this leads to bugs with scrolling in certain cases.
    /// See http://answers.unity.com/answers/1024120/view.html (https://archive.vn/ZScfd) for more information.
    /// </summary>
    public class PointerHelper: MonoBehaviour, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
    {
        /// <summary>
        /// This event will be triggered whenever the mouse pointer leaves the area over the GameObject
        /// this component is attached to.
        /// </summary>
        public readonly UnityEvent<PointerEventData> ExitEvent = new();

        /// <summary>
        /// This event will be triggered whenever the mouse pointer enters the area over the GameObject
        /// this component is attached to.
        /// </summary>
        public readonly UnityEvent<PointerEventData> EnterEvent = new();

        /// <summary>
        /// This event will be triggered whenever the mouse pointer clicks the GameObject
        /// this component is attached to.
        /// </summary>
        public readonly UnityEvent<PointerEventData> ClickEvent = new();

        /// <summary>
        /// This event will be triggered whenever the user clicks the GameObject
        /// this component is attached to with the thumbstickbutton in VR.
        /// </summary>
        public readonly UnityEvent<PointerEventData> ThumbstickEvent = new();

        /// <summary>
        /// Invokes the <see cref="ExitEvent"/>.
        /// </summary>
        /// <param name="eventData">Data about the pointer.</param>
        public void OnPointerExit(PointerEventData eventData) => ExitEvent.Invoke(eventData);

        /// <summary>
        /// Invokes the <see cref="EnterEvent"/>.
        /// </summary>
        /// <param name="eventData">Data about the pointer.</param>
        public void OnPointerEnter(PointerEventData eventData) => EnterEvent.Invoke(eventData);

        /// <summary>
        /// Invokes the <see cref="ClickEvent"/>.
        /// </summary>
        /// <param name="eventData">Data about the pointer.</param>
        public void OnPointerClick(PointerEventData eventData) => ClickEvent.Invoke(eventData);
    }
}
