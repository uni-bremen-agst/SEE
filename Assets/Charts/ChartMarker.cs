using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChartMarker : MonoBehaviour, IPointerDownHandler
{
	private GameObject linkedObject;
	public GameObject LinkedObject
	{
		set { linkedObject = value; }
	}
	[SerializeField, Range(0.1f, 1f)]
	float clickDelay = 0.5f;
	bool waiting = false;
	bool running = false;

	/// <summary>
	/// Checks for a double or a single click when the mouse is clicked.
	/// </summary>
	/// <param name="eventData"></param>
	public void OnPointerDown(PointerEventData eventData)
	{
		if (running)
		{
			waiting = false;
		}
		else
		{
			StartCoroutine(WaitForDoubleClick());
		}
	}

	/// <summary>
	/// Checks if one or two clicks happen in a given interval.
	/// </summary>
	/// <returns></returns>
	IEnumerator WaitForDoubleClick()
	{
		running = true;
		waiting = true;
		yield return new WaitForSeconds(clickDelay);
		if (waiting)
		{
			HighlightLinkedObject();
		}
		else
		{
			ShowLinkedObject();
		}
		waiting = false;
		running = false;
	}

	/// <summary>
	/// Highlights the object linked to this one.
	/// </summary>
	public void HighlightLinkedObject()
	{
		Debug.Log("Your object is " + linkedObject.name);
	}

	/// <summary>
	/// Moves the camera to view the linked object.
	/// </summary>
	void ShowLinkedObject()
	{
		Camera activeCamera = Camera.main;
		Debug.Log("Moved Camera");
	}
}
