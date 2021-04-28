using UnityEngine;
using UnityEngine.Events;
using Lean.Common;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace Lean.Touch
{
	/// <summary>This script allows you to change the color of the SpriteRenderer attached to the current GameObject.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanSelectableDrop")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Selectable Drop")]
	public class LeanSelectableDrop : LeanSelectableByFingerBehaviour
	{
		[System.Serializable] public class GameObjectEvent : UnityEvent<GameObject> {}
		[System.Serializable] public class IDropHandlerEvent : UnityEvent<IDropHandler> {}

		public enum SelectType
		{
			Raycast3D,
			Overlap2D,
			CanvasUI
		}

		public enum SearchType
		{
			GetComponent,
			GetComponentInParent,
			GetComponentInChildren
		}

		public SelectType SelectUsing { set { selectUsing = value; } get { return selectUsing; } } [FSA("SelectUsing")] [SerializeField] private SelectType selectUsing;

		/// <summary>This stores the layers we want the raycast/overlap to hit.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [FSA("LayerMask")] [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>The GameObject you drop this on must have this tag.
		/// Empty = No tag required.</summary>
		public string RequiredTag { set { requiredTag = value; } get { return requiredTag; } } [FSA("RequiredTag")] [SerializeField] private string requiredTag;

		/// <summary>How should the IDropHandler be searched for on the dropped GameObject?</summary>
		public SearchType Search { set { search = value; } get { return search; } } [FSA("Search")] [SerializeField] private SearchType search;

		/// <summary>The camera this component will calculate using.
		/// None/null = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [FSA("Camera")] [SerializeField] private Camera _camera;

		/// <summary>Called on the first frame the conditions are met.
		/// GameObject = The GameObject instance this was dropped on.</summary>
		public GameObjectEvent OnGameObject { get { if (onGameObject == null) onGameObject = new GameObjectEvent(); return onGameObject; } } [SerializeField] private GameObjectEvent onGameObject;

		/// <summary>Called on the first frame the conditions are met.
		/// IDropHandler = The IDropHandler instance this was dropped on.</summary>
		public IDropHandlerEvent OnDropHandler { get { if (onDropHandler == null) onDropHandler = new IDropHandlerEvent(); return onDropHandler; } } [SerializeField] private IDropHandlerEvent onDropHandler;

		//private static RaycastHit[] raycastHits = new RaycastHit[1024];

		private static RaycastHit2D[] raycastHit2Ds = new RaycastHit2D[1024];

		protected override void OnSelectedFingerUp(LeanFinger finger)
		{
			// Stores the component we hit (Collider or Collider2D)
			var component = default(Component);

			switch (selectUsing)
			{
				case SelectType.Raycast3D:
				{
					// Make sure the camera exists
					var camera = LeanHelper.GetCamera(_camera, gameObject);

					if (camera != null)
					{
						var ray = camera.ScreenPointToRay(finger.ScreenPosition);
						var hit = default(RaycastHit);

						if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layers) == true)
						{
							component = hit.collider;
						}
					}
					else
					{
						Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
					}
				}
				break;

				case SelectType.Overlap2D:
				{
					// Make sure the camera exists
					var camera = LeanHelper.GetCamera(_camera, gameObject);

					if (camera != null)
					{
						var ray   = camera.ScreenPointToRay(finger.ScreenPosition);
						var count = Physics2D.GetRayIntersectionNonAlloc(ray, raycastHit2Ds, float.PositiveInfinity, layers);

						if (count > 0)
						{
							component = raycastHit2Ds[0].transform;
						}
					}
					else
					{
						Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
					}
				}
				break;

				case SelectType.CanvasUI:
				{
					var results = LeanTouch.RaycastGui(finger.ScreenPosition, layers);

					if (results != null && results.Count > 0)
					{
						component = results[0].gameObject.transform;
					}
				}
				break;
			}

			// Select the component
			Drop(finger, component);
		}

		private void Drop(LeanFinger finger, Component component)
		{
			var dropHandler = default(IDropHandler);

			if (component != null)
			{
				switch (search)
				{
					case SearchType.GetComponent:           dropHandler = component.GetComponent          <IDropHandler>(); break;
					case SearchType.GetComponentInParent:   dropHandler = component.GetComponentInParent  <IDropHandler>(); break;
					case SearchType.GetComponentInChildren: dropHandler = component.GetComponentInChildren<IDropHandler>(); break;
				}
			}

			if (dropHandler != null)
			{
				if (string.IsNullOrEmpty(requiredTag) == false)
				{
					if (component.tag != requiredTag)
					{
						return;
					}
				}

				dropHandler.HandleDrop(gameObject, finger);

				if (onGameObject != null)
				{
					onGameObject.Invoke(component.gameObject);
				}

				if (onDropHandler != null)
				{
					onDropHandler.Invoke(dropHandler);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using TARGET = LeanSelectableDrop;

	[UnityEditor.CanEditMultipleObjects]
	[UnityEditor.CustomEditor(typeof(TARGET))]
	public class LeanSelectableDrop_Editor : LeanEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("selectUsing");
			Draw("layers", "This stores the layers we want the raycast/overlap to hit.");
			Draw("requiredTag", "The GameObject you drop this on must have this tag.\n\nEmpty = No tag required.");
			Draw("search", "How should the IDropHandler be searched for on the dropped GameObject?");
			Draw("_camera", "The camera used to calculate the ray.\n\nNone = MainCamera.");

			Separator();

			var usedA = Any(tgts, t => t.OnGameObject.GetPersistentEventCount() > 0);
			var usedB = Any(tgts, t => t.OnDropHandler.GetPersistentEventCount() > 0);

			var showUnusedEvents = DrawFoldout("Show Unused Events", "Show all events?");

			if (usedA == true || showUnusedEvents == true)
			{
				Draw("onGameObject");
			}

			if (usedB == true || showUnusedEvents == true)
			{
				Draw("onDropHandler");
			}
		}
	}
}
#endif