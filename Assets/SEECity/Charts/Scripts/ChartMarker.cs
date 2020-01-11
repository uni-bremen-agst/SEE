using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for the markers representing entries linked to objects in the chart.
	/// </summary>
	public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		/// <summary>
		/// Contains some settings used by <see cref="ChartMarker" />.
		/// </summary>
		private GameManager _gameManager;

		//User Variables from GameManager
		private float _cameraDistance;
		private bool _moveWithRotation;
		private float _cameraFlightTime;
		private float _clickDelay;
		private float _highlightDuration;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		private Material _buildingHighlightMaterial;

		/// <summary>
		/// Copy of the <see cref="LinkedObject" /> with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		/// <summary>
		/// The <see cref="GameObject" /> in the code city that is connected with this button.
		/// </summary>
		[HideInInspector] public GameObject LinkedObject;

		/// <summary>
		/// The active <see cref="Camera" /> in the scene.
		/// </summary>
		private Camera _activeCamera;

		/// <summary>
		/// The currently running camera movement <see cref="Coroutine" />.
		/// </summary>
		private Coroutine _cameraMoving;

		/// <summary>
		/// Determines if a second click happened during <see cref="_clickDelay" />.
		/// </summary>
		private bool _waiting;

		/// <summary>
		/// Determines if <see cref="WaitForDoubleClick" /> is currently running.
		/// </summary>
		private bool _runningClick;

		/// <summary>
		/// The currently running <see cref="TimedHighlightRoutine" />.
		/// </summary>
		private Coroutine _timedHighlight;

		/// <summary>
		/// The <see cref="GameObject" /> making the marker look highlighted when active.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private GameObject _markerHighlight = null;

		/// <summary>
		/// A text popup containing useful information about the marker and its <see cref="LinkedObject" />.
		/// </summary>
		[Header("Other"), SerializeField] private TextMeshProUGUI _infoText = null;

		/// <summary>
		/// Links the <see cref="GameManager" /> and calls methods for initialization.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
		}

		/// <summary>
		/// Takes the setting data from <see cref="GameManager" /> for use in <see cref="ChartMarker" />.
		/// </summary>
		private void GetSettingData()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			_cameraDistance = _gameManager.CameraDistance;
			_moveWithRotation = _gameManager.MoveWithRotation;
			_cameraFlightTime = _gameManager.CameraFlightTime;
			_clickDelay = _gameManager.ClickDelay;
			_highlightDuration = _gameManager.HighlightDuration;
			_buildingHighlightMaterial = _gameManager.BuildingHighlightMaterial;
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
				TimedHighlight(_highlightDuration);
			}
			else
			{
				ShowLinkedObject();
				HighlightLinkedObjectToggle(true); //TODO: Deactivate when user switches target.
			}

			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Highlights the <see cref="LinkedObject" />.
		/// </summary>
		private void HighlightLinkedObjectToggle(bool highlight)
		{
			if (highlight)
			{
				_highlightCopy = Instantiate(LinkedObject, LinkedObject.transform);
				_highlightCopy.GetComponent<Renderer>().material = _buildingHighlightMaterial;
			}
			else
			{
				Destroy(_highlightCopy);
			}

			ToggleMarkerHighlight(highlight);
		}

		public void ToggleMarkerHighlight(bool active)
		{
			_markerHighlight.SetActive(active);
		}

		/// <summary>
		/// Highlights this marker and its <see cref="LinkedObject" /> for a given amount of time.
		/// </summary>
		/// <param name="time">How long the highlight will last.</param>
		public void TimedHighlight(float time)
		{
			if (_timedHighlight != null)
			{
				StopCoroutine(_timedHighlight);
				HighlightLinkedObjectToggle(false);
			}

			_timedHighlight = StartCoroutine(TimedHighlightRoutine(time));
		}

		public IEnumerator TimedHighlightRoutine(float time)
		{
			HighlightLinkedObjectToggle(true);
			yield return new WaitForSeconds(time);
			HighlightLinkedObjectToggle(false);
		}

		/// <summary>
		/// Moves the camera to view the <see cref="LinkedObject" />.
		/// </summary>
		private void ShowLinkedObject()
		{
			_activeCamera = Camera.main; //TODO: Change to active camera and not just main camera.
			if (_moveWithRotation)
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				Vector3 lookPos =
					LinkedObject.transform.position - _activeCamera.transform.position;
				_cameraMoving = StartCoroutine(MoveCameraTo(
					Vector3.MoveTowards(_activeCamera.transform.position,
						LinkedObject.transform.position,
						lookPos.magnitude - _cameraDistance), Quaternion.LookRotation(lookPos)));
			}
			else
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				_cameraMoving = StartCoroutine(MoveCameraTo(new Vector3(
					LinkedObject.transform.position.x,
					_activeCamera.transform.position.y,
					LinkedObject.transform.position.z - _cameraDistance)));
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
			if (newPos != LinkedObject.transform.position)
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

		/// <summary>
		/// Changes the <see cref="_infoText" /> of this marker.
		/// </summary>
		/// <param name="info">The new text.</param>
		public void SetInfoText(string info)
		{
			_infoText.text = info;
		}

		/// <summary>
		/// Activates the <see cref="_infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			_infoText.gameObject.SetActive(true);
		}

		/// <summary>
		/// Deactivates the <see cref="_infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			_infoText.gameObject.SetActive(false);
		}
	}
}