using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeColorPickerManager : MonoBehaviour, IPointerClickHandler
    {
    
    
        public event Action OnEndEdit;
        public void OnPointerClick(PointerEventData eventData)
        {
            OnEndEdit?.Invoke();
        }
    }
}
