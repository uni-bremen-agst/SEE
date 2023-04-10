using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeColorPickerManager : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            OnEndEdit?.Invoke();
        }


        public event Action OnEndEdit;
    }
}