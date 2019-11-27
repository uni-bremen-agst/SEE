using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Contains the logic for the markers representing entries linked to objects in the chart.
/// </summary>
public class ChartMarker : MonoBehaviour, IPointerDownHandler
{
	/// <summary>
	/// The object in the code city that is connected with this button.
	/// </summary>
	private GameObject linkedObject;
	public GameObject LinkedObject
	{
		set { linkedObject = value; }
	}
	/// <summary>
	/// The active camera in the scene.
	/// </summary>
	Camera activeCamera;
	bool waiting = false;
	bool runningClick = false;
	Coroutine runningCamera = null;

	[Header("Camera Controls")] //TODO: Move to different script?
	[SerializeField]
	float cameraDistance = 50f;
	[SerializeField]
	bool moveWithRotation = true;
	[SerializeField]
	float cameraFlightTime = 0.5f;

	[Header("User Inputs")] //TODO: Move to different script?
	[SerializeField, Range(0.1f, 1f)]
	float clickDelay = 0.5f;

	/// <summary>
	/// Checks for a double or a single click when the mouse is clicked.
	/// </summary>
	/// <param name="eventData"></param>
	public void OnPointerDown(PointerEventData eventData)
	{
		if (runningClick)
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
		runningClick = true;
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
		runningClick = false;
	}

	/// <summary>
	/// Highlights the object linked to this one.
	/// </summary>
	public void HighlightLinkedObject()
	{
		//TODO
		Debug.Log("Your object is " + linkedObject.name);
	}

	/// <summary>
	/// Moves the camera to view the linked object.
	/// </summary>
	void ShowLinkedObject()
	{
		activeCamera = Camera.main; //TODO: Change to active camera and not just main camera.
		if (moveWithRotation)
		{
			if (runningCamera != null)
			{
				StopCoroutine(runningCamera);
				runningCamera = null;
			}
			Vector3 lookPos = linkedObject.transform.position - activeCamera.transform.position;
			runningCamera = StartCoroutine(MoveCameraTo(Vector3.MoveTowards(activeCamera.transform.position, linkedObject.transform.position, lookPos.magnitude - cameraDistance), Quaternion.LookRotation(lookPos)));
		}
		else
		{
			if (runningCamera != null)
			{
				StopCoroutine(runningCamera);
				runningCamera = null;
			}
			runningCamera = StartCoroutine(MoveCameraTo(new Vector3(linkedObject.transform.position.x, activeCamera.transform.position.y, linkedObject.transform.position.z - cameraDistance)));
		}
	}

	/// <summary>
	/// Moves the camera smoothly from one position to another and rotates it to look towards a specified position.
	/// </summary>
	/// <param name="newPos">The target position.</param>
	/// <param name="lookAt">The position to look at.</param>
	/// <returns></returns>
	IEnumerator MoveCameraTo(Vector3 newPos, Quaternion lookAt)
	{
		Vector3 oldPos = activeCamera.transform.position;
		if (newPos != linkedObject.transform.position)
		{
			Quaternion oldRot = activeCamera.transform.rotation;
			for (float time = 0f; time <= cameraFlightTime; time += Time.deltaTime)
			{
				activeCamera.transform.position = Vector3.Lerp(oldPos, newPos, time * (1 / cameraFlightTime));
				activeCamera.transform.rotation = Quaternion.Slerp(oldRot, lookAt, time * (1 / cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}
			activeCamera.transform.rotation = lookAt;
			activeCamera.transform.position = newPos;
		}
	}

	/// <summary>
	/// Moves the camera smoothly from one position to another.
	/// </summary>
	/// <param name="newPos">The tarrget position.</param>
	/// <returns></returns>
	IEnumerator MoveCameraTo(Vector3 newPos)
	{
		Vector3 oldPos = activeCamera.transform.position;
		for (float time = 0; time <= cameraFlightTime; time += Time.deltaTime)
		{
			activeCamera.transform.position = Vector3.Lerp(oldPos, newPos, time * (1 / cameraFlightTime));
			yield return new WaitForEndOfFrame();
		}
		activeCamera.transform.position = newPos;
	}
}
