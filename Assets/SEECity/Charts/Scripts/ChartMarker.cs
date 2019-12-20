using System.Collections;
using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for the markers representing entries linked to objects in the chart.
	/// </summary>
	public class ChartMarker : MonoBehaviour
	{
		private GameManager _gameManager;

		//User Variables
		private float _cameraDistance;
		private bool _moveWithRotation;
		private float _cameraFlightTime;
		private float _clickDelay;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private Material _highlightMaterial;

		/// <summary>
		/// Copy of the linked object with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

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

		private Coroutine _runningCamera;

		//Double click booleans
		/// <summary>
		/// Determines if a second click happened during <see cref="_clickDelay" />.
		/// </summary>
		private bool _waiting;

		/// <summary>
		/// Determines if <see cref="WaitForDoubleClick" /> is currently running.
		/// </summary>
		private bool _runningClick;

		/// <summary>
		/// Links the <see cref="GameManager" /> and calls methods for initialization.
		/// </summary>
		private void Start()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			GetSettingData();
		}

		/// <summary>
		/// Takes the setting data from <see cref="GameManager" /> for use in <see cref="ChartMarker" />.
		/// </summary>
		private void GetSettingData()
		{
			_cameraDistance = _gameManager.CameraDistance;
			_moveWithRotation = _gameManager.MoveWithRotation;
			_cameraFlightTime = _gameManager.CameraFlightTime;
			_clickDelay = _gameManager.ClickDelay;
		}

		/// <summary>
		/// Called by Unity when the button assigned to the <see cref="ChartMarker" /> is pressed.
		/// </summary>
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
			yield return new WaitForSeconds(_clickDelay);
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
			_activeCamera = Camera.main; //TODO: Change to active camera and not just main camera.
			if (_moveWithRotation)
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
						lookPos.magnitude - _cameraDistance), Quaternion.LookRotation(lookPos)));
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
					_linkedObject.transform.position.z - _cameraDistance)));
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
				for (float time = 0f; time <= _cameraFlightTime; time += Time.deltaTime)
				{
					_activeCamera.transform.position =
						Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
					_activeCamera.transform.rotation =
						Quaternion.Slerp(oldRot, lookAt, time * (1 / _cameraFlightTime));
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
			for (float time = 0; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position =
					Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			_activeCamera.transform.position = newPos;
		}
	}
}