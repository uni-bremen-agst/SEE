using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Text))]
public class TextColorControl : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public Color normalColor;
    public Color highlightColor;

    public void Start()
    {
        normalColor = GetComponent<Text>().color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Text>().color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       GetComponent<Text>().color = normalColor ;
    }

 
}
