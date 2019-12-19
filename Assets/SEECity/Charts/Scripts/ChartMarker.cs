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
		/// The <see cref="GameObject" /> in the code city that is connected with this button.
		/// </summary>
		private GameObject _linkedObject;

		public GameObject LinkedObject
		{
			set => _linkedObject = value;
		}

		/// <summary>
		/// The active <see cref="Camera" /> in the scene.
		/// </summary>
		private Camera _activeCamera;

		private bool _waiting;
		private bool _runningClick;
		private Coroutine _runningCamera;

		[Header("Camera Controls"), SerializeField]
		private float cameraDistance = 50f;

		[SerializeField] private bool moveWithRotation = true;
		[SerializeField] private float cameraFlightTime = 0.5f;

		[Header("User Inputs"), SerializeField, Range(0.1f, 1f)]
		private float clickDelay = 0.5f;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private Material _highlightMaterial;

		/// <summary>
		/// The thickness of the highlight outline of <see cref="_highlightMaterial"/>.
		/// </summary>
		[SerializeField] private float _highlightOutline = 0.001f;

		/// <summary>
		/// The original value of <see cref="_highlightOutline"/>
		/// </summary>
		private float _highlightOutlineOld;

		/// <summary>
		/// Copy of the linked object with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		/// <summary>
		/// Indicates if any <see cref="ChartMarker" /> is currently animating the highlights in the scene.
		/// </summary>
		private static bool _animating;

		/// <summary>
		/// Saves the original values of <see cref="_highlightMaterial" />
		/// </summary>
		private void Start()
		{
			_highlightOutlineOld = _highlightMaterial.GetFloat("g_flOutlineWidth");
		}

		/// <summary>
		/// Animates the highlights in the scene if no other <see cref="ChartMarker" /> is currently animating
		/// them.
		/// </summary>
		private void Update()
		{
			if (!_animating)
			{
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

        public void ButtonClicked()
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
			{
				HighlightLinkedObject();
			}
			else
			{
				ShowLinkedObject();
				HighlightLinkedObject(); //TODO: Replace with long
			}

			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Highlights the <see cref="_linkedObject" />.
		/// </summary>
		public void HighlightLinkedObject()
		{
			_highlightCopy = Instantiate(_linkedObject, _linkedObject.transform);
			_highlightCopy.GetComponent<Renderer>().material = _highlightMaterial;
		}
		//TODO: Create separate methods for short and for continuous highlights.

		/// <summary>
		/// Moves the camera to view the <see cref="_linkedObject" />.
		/// </summary>
		private void ShowLinkedObject()
		{
            Debug.Log("DoubleClick");
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
		/// Moves the <see cref="Camera" /> smoothly from one position to another and rotates it to look
		/// towards a specified position.
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
		/// Moves the camera smoothly from one position to another without rotation.
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

		/// <summary>
		/// Resets the <see cref="_highlightMaterial" />
		/// </summary>
		private void OnDestroy()
		{
			if (_highlightMaterial.GetFloat("g_flOutlineWidth") != _highlightOutlineOld)
				_highlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutlineOld);
		}
	}
}