using System;
using Lean.Common;
using SEE.DataModel;
using SEE.Game.UI.Architecture;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Component for recording gesture paths.
    /// </summary>
    public class GestureRecorder : MonoBehaviour
    {


        public LeanPlane leanPlane;
        /// <summary>
        /// The input action definiton for the pen.
        /// </summary>
        private ArchitectureInputActions InputActions;
        /// <summary>
        /// The gesture path prefab
        /// </summary>
        private GameObject prefab;
        /// <summary>
        /// The gesture path instance.
        /// </summary>
        private GameObject pathGO;
        
        /// <summary>
        /// Whether the input should be sampled or tested against the existing gesture template.
        /// </summary>
        public bool sample;

        public string gestureName;

        /// <summary>
        /// 
        /// </summary>
        public GestureResultUI ResultUI;

        /// <summary>
        /// Whether the user is drawing
        /// </summary>
        private bool isDrawing;

        private Vector3 heightOffset = new Vector3(0f, 0.1f, 0f);


        /// <summary>
        /// Input action for the pen position.
        /// </summary>
        private InputAction positionAction;

        private void Start()
        {
            prefab = Resources.Load<GameObject>("Prefabs/Architecture/StrokeGesture");
            InputActions = new ArchitectureInputActions();
            InputActions.Drawing.DrawBegin.performed += OnDrawBegin;
            InputActions.Drawing.Draw.performed += ctx => isDrawing = true;
            InputActions.Drawing.DrawEnd.performed += OnDrawEnd;
            positionAction = InputActions.Drawing.Position;
            InputActions.Enable();
        }
        
        /// <summary>
        /// Event handler method for the DrawEnd mapping from <see cref="ArchitectureInputActions.DrawingActions"/>
        /// </summary>
        private void OnDrawEnd(InputAction.CallbackContext obj)
        {
            isDrawing = false;
            if (pathGO == null) return;

            if (sample)
            {
                Sample();
            }
            else
            {
                Recognize();
            }
            
            Destroy(pathGO);

        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Sample()
        {
            Vector3[] rawPoints = DollarPGestureRecognizer.ExtractRawPoints(pathGO);
            GesturePoint[] gesturePath = DollarPGestureRecognizer.ExtractGesturePoints(pathGO);
            GestureIO.SaveDataSetToDisk(gesturePath, gestureName);
            ResultUI.ShowResult(default(DollarPGestureRecognizer.RecognizerResult), rawPoints.Length, sample, gestureName);
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Recognize()
        {
            if (DollarPGestureRecognizer.TryRecognizeGesture(pathGO,
                out DollarPGestureRecognizer.RecognizerResult result, out Vector3[] rawPoints))
            {
                
                ResultUI.ShowResult(result, rawPoints.Length, sample, gestureName);
            }
            else
            {
                ResultUI.ShowResult(result, rawPoints.Length, sample, gestureName, true);
            }
        }
        
       
        /// <summary>
        /// Event handler method for the DrawBegin mapping from <see cref="ArchitectureInputActions.DrawingActions"/>.
        /// If the raycast target is the whiteboard instantiate the gesture path prefab.
        /// </summary>
        private void OnDrawBegin(InputAction.CallbackContext obj)
        {
            isDrawing = true;
            Vector3 hit = default(Vector3);
            if (TryRaycastWhiteboard(ref hit))
            {
                pathGO = Instantiate(prefab, hit, Quaternion.identity);
            }
        }

        private void Update()
        {
            Vector3 hit = default(Vector3);
            // Update the path instance.
            if (isDrawing && pathGO != null && TryRaycastWhiteboard(ref hit))
            {
                pathGO.transform.position = hit;
            }
        }


        /// <summary>
        /// Performs a raycast to find the whiteboard with tag <see cref="Tags.Whiteboard"/>.
        /// </summary>
        /// <param name="raycastHit">the hit struct</param>
        /// <returns>True if the target is the whiteboard go, false otherwise.</returns>
        private bool TryRaycastWhiteboard(ref Vector3 hit)
        {
            Vector2 pointerPosition = positionAction.ReadValue<Vector2>();
            var ray = MainCamera.Camera.ScreenPointToRay(pointerPosition);
            return leanPlane.TryRaycast(ray, ref hit);
            
            
        }
    }
}