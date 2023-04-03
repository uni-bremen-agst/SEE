using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RuntimeSliderManager : MonoBehaviour, IPointerUpHandler 
{
    
    
    public event Action OnEndEdit;
    public void OnPointerUp(PointerEventData eventData)
    {
        OnEndEdit?.Invoke();
    }
}
