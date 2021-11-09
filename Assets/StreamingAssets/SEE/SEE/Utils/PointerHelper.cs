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
    public class PointerHelper: MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
    {
        /// <summary>
        /// This event will be triggered whenever the mouse pointer leaves the area over the GameObject
        /// this component is attached to.
        /// </summary>
        public readonly UnityEvent ExitEvent = new UnityEvent();

        /// <summary>
        /// This event will be triggered whenever the mouse pointer enters the area over the GameObject
        /// this component is attached to.
        /// </summary>
        public readonly UnityEvent EnterEvent = new UnityEvent();

        /// <summary>
        /// Invokes the <see cref="ExitEvent"/>.
        /// </summary>
        /// <param name="eventData">Data about the pointer. Currently unused.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            ExitEvent.Invoke();
        }

        /// <summary>
        /// Invokes the <see cref="EnterEvent"/>.
        /// </summary>
        /// <param name="eventData">Data about the pointer. Currently unused.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            EnterEvent.Invoke();
        }
    }
}