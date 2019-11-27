using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChartDragHandler : MonoBehaviour, IDragHandler
{
	[SerializeField]
	GameObject chart;
	public void OnDrag(PointerEventData eventData)
	{
		//TODO: Check if this is local position
		RectTransform pos = GetComponent<RectTransform>();
		chart.transform.position = new Vector3(Input.mousePosition.x - (pos.anchoredPosition.x / 2) + (pos.rect.width / 2), Input.mousePosition.y
			- (pos.anchoredPosition.y / 2) - (pos.rect.height / 2), Input.mousePosition.z);
	}
}