using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeSliderManager : MonoBehaviour, IPointerUpHandler 
    {
    
    
        public event Action OnEndEdit;
        public void OnPointerUp(PointerEventData eventData)
        {
            OnEndEdit?.Invoke();
        }
    }
}
