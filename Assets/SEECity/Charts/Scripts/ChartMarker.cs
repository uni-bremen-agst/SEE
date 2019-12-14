using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for the markers representing entries linked to objects in the chart.
	/// </summary>
	public class ChartMarker : MonoBehaviour, IPointerDownHandler
	{
		/// <summary>
		/// The object in the code city that is connected with this button.
		/// </summary>
		private GameObject _linkedObject;

		public GameObject LinkedObject
		{
			set => _linkedObject = value;
		}

		/// <summary>
		/// The active camera in the scene.
		/// </summary>
		private Camera _activeCamera;

		private bool _waiting;
		private bool _runningClick;
		private Coroutine _runningCamera;

		[Header("Camera Controls"), SerializeField]
		private readonly float cameraDistance = 50f;

		[SerializeField] private bool moveWithRotation = true;
		[SerializeField] private float cameraFlightTime = 0.5f;

		[Header("User Inputs"), SerializeField, Range(0.1f, 1f)]
		private readonly float clickDelay = 0.5f;

		/// <summary>
		/// The material making the object look highlighted.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private Material _highlightMaterial;

		/// <summary>
		/// The thickness of the highlight outline.
		/// </summary>
		[SerializeField] private float _highlightOutline = 0.001f;

		/// <summary>
		/// Copy of the linked object with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		/// <summary>
		/// 
		/// </summary>
		private static bool _animating;

		/// <summary>
		/// 
		/// </summary>
		private void Update()
		{
			if (!_animating)
			{
				Debug.Log("Test");
				_animating = true;
				StartCoroutine(HighlightAnimation());
			}
		}

		private IEnumerator HighlightAnimation()
		{
			_highlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutline);
			yield return new WaitForEndOfFrame();
			_animating = false;
		}

		/// <summary>
		/// Checks for a double or a single click when the mouse is clicked.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerDown(PointerEventData eventData)
		{
			if (_runningClick)
				_waiting = false;
			else
				StartCoroutine(WaitForDoubleClick());
		}

		/// <summary>
		/// Checks if one or two clicks happen in a given interval.
		/// </summary>
		/// <returns></returns>
		private IEnumerator WaitForDoubleClick()
		{
			_runningClick = true;
			_waiting = true;
			yield return new WaitForSeconds(clickDelay);
			if (_waiting)
				HighlightLinkedObject();
			else
				ShowLinkedObject();
			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Highlights the object linked to this one.
		/// </summary>
		public void HighlightLinkedObject()
		{
			_highlightCopy = Instantiate(_linkedObject, _linkedObject.transform);
			_highlightCopy.GetComponent<Renderer>().material = _highlightMaterial;
		}

		/// <summary>
		/// Moves the camera to view the linked object.
		/// </summary>
		private void ShowLinkedObject()
		{
			_activeCamera = Camera.main; //TODO: Change to active camera and not just main camera.
			if (moveWithRotation)
			{
				if (_runningCamera != null)
				{
					StopCoroutine(_runningCamera);
					_runningCamera = null;
				}

				Vector3 lookPos =
					_linkedObject.transform.position - _activeCamera.transform.position;
				_runningCamera = StartCoroutine(MoveCameraTo(
					Vector3.MoveTowards(_activeCamera.transform.position,
						_linkedObject.transform.position,
						lookPos.magnitude - cameraDistance), Quaternion.LookRotation(lookPos)));
			}
			else
			{
				if (_runningCamera != null)
				{
					StopCoroutine(_runningCamera);
					_runningCamera = null;
				}

				_runningCamera = StartCoroutine(MoveCameraTo(new Vector3(
					_linkedObject.transform.position.x,
					_activeCamera.transform.position.y,
					_linkedObject.transform.position.z - cameraDistance)));
			}
		}

		/// <summary>
		/// Moves the camera smoothly from one position to another and rotates it to look towards a specified
		/// position.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <param name="lookAt">The position to look at.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos, Quaternion lookAt)
		{
			Vector3 oldPos = _activeCamera.transform.position;
			if (newPos != _linkedObject.transform.position)
			{
				Quaternion oldRot = _activeCamera.transform.rotation;
				for (float time = 0f; time <= cameraFlightTime; time += Time.deltaTime)
				{
					_activeCamera.transform.position =
						Vector3.Lerp(oldPos, newPos, time * (1 / cameraFlightTime));
					_activeCamera.transform.rotation =
						Quaternion.Slerp(oldRot, lookAt, time * (1 / cameraFlightTime));
					yield return new WaitForEndOfFrame();
				}

				_activeCamera.transform.rotation = lookAt;
				_activeCamera.transform.position = newPos;
			}
		}

		/// <summary>
		/// Moves the camera smoothly from one position to another.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos)
		{
			Vector3 oldPos = _activeCamera.transform.position;
			for (float time = 0; time <= cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position =
					Vector3.Lerp(oldPos, newPos, time * (1 / cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			_activeCamera.transform.position = newPos;
		}
	}
}