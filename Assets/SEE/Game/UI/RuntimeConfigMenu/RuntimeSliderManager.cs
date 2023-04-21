using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeSliderManager : MonoBehaviour, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData eventData)
        {
            OnEndEdit?.Invoke();
        }


        public event Action OnEndEdit;
    }
}