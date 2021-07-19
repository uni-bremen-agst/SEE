using System;
using DG.Tweening;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Utils;
using UnityEngine;
using Plane = SEE.GO.Plane;

namespace SEE.Controls
{
    /// <summary>
    /// Pen Input specific camera movement. Right now this only handles the
    /// switch between <see cref="ArchitectureAction"/> and other <see cref="AbstractPlayerAction"/>.
    /// If the Pen should be supported for camera movement within other <see cref="AbstractPlayerAction"/>
    /// than <see cref="ArchitectureAction"/> the logic should be implemented here.
    /// </summary>
    public class PenPlayerMovement : MonoBehaviour
    {
       
        [Tooltip("The city which the player is focusing on.")]
        public Plane focusedObject;

        /// <summary>
        /// Struct that holds the state of the player camera.
        /// </summary>
        private struct CameraState
        {
            public float distance;
            public float yaw;
            public float pitch;
            public Vector3 lastPosition;
            public Vector3 lastRotation;
        }


        /// <summary>
        /// The camera state instance
        /// </summary>
        private CameraState cameraState;

        /// <summary>
        /// Whether the <see cref="ArchitectureAction"/> is activated.
        /// </summary>
        private bool architectureActionSelected;


        private void Awake()
        {
            // On Deactivation of the ArchitectureAction animate the camera to rotate and move into the default position.
            ArchitectureAction.OnArchitectureActionDisabled += () =>
            {
                Tweens.Move(MainCamera.Camera.transform.gameObject, cameraState.lastPosition, 5f);
                Tweens.Rotate(MainCamera.Camera.transform.gameObject, cameraState.lastRotation, 5f,
                    o => architectureActionSelected = false);
            };

            // On activation of the ArchitectureAction animate the camera to rotate and move into a top-view
            ArchitectureAction.OnArchitectureActionEnabled += () =>
            {
                architectureActionSelected = true;
                cameraState.lastPosition = MainCamera.Camera.transform.position;
                cameraState.lastRotation = MainCamera.Camera.transform.rotation.eulerAngles;
                SEECityArchitecture city = SceneQueries.FindArchitectureCity();
                Tweens.Move(MainCamera.Camera.gameObject, city.transform.position + new Vector3(0, 1.25f, 0), 5f);
                Tweens.Rotate(MainCamera.Camera.transform.gameObject, new Vector3(90f, 0f, 0f), 5f);
            };
        }
        
        private void Update()
        {
            // Allow repositioning of the camera within the architecture action. Therefore skipping the update logic.
            if (architectureActionSelected) return;
            Camera mainCamera = MainCamera.Camera;
            // Keep the camera focused on the focused city with an 45 degree pitch.
            if (focusedObject != null)
            {
                cameraState.distance = 2.0f;
                cameraState.yaw = 0.0f;
                cameraState.pitch = 45.0f;
                mainCamera.transform.position = focusedObject.CenterTop;
                mainCamera.transform.position -= mainCamera.transform.forward * cameraState.distance;
                mainCamera.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }
        }
    }
}