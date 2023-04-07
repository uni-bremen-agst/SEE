using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class RuntimeColorPickerManager : MonoBehaviour, IPointerClickHandler
{
    
    
    public event Action OnEndEdit;
    public void OnPointerClick(PointerEventData eventData)
    {
        OnEndEdit?.Invoke();
    }
}
