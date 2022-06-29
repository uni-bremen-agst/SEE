using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Text))]
public class TextChangeMyColor : MonoBehaviour
{
    public Color normalColor;
    public Color highlightColor;

    public void Start()
    {
        normalColor = GetComponent<Text>().color;
    }

    public void OnEnter()
    {
        GetComponent<Text>().color = highlightColor;
    }

    public void OnExit()
    {
        GetComponent<Text>().color = normalColor;
    }
}
