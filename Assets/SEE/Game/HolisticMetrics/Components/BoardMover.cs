using SEE.Controls.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    /// <summary>
    /// This class is a component that should be attached to the little button underneath every metrics board. It is
    /// responsible for moving the board around when the user is dragging the mouse over the button. 
    /// </summary>
    public class BoardMover : MonoBehaviour
    {
        /// <summary>
        /// The position of the board when the player initially clicks the move button underneath it. This is needed
        /// so we can revert the move action.
        /// </summary>
        private Vector3 oldPosition;

        /// <summary>
        /// The rotation of the board when the player initially clicks the move button underneath it. This is needed so
        /// we can revert the move action.
        /// </summary>
        private Quaternion oldRotation;
        
        /// <summary>
        /// This plane represents the floor when calculating the intersection between a ray from the cursor into the
        /// scene and the floor.
        /// </summary>
        private static Plane floor = new Plane(Vector3.up, Vector3.zero);

        /// <summary>
        /// The parent transform of this game object, i.e., the metrics board transform. This will have its position
        /// and orientation changed.
        /// </summary>
        private Transform parentTransform;

        /// <summary>
        /// When the player starts the moving (left mouse button goes down on the button), we will save the old position
        /// of the metrics board so we can undo the moving action later.
        /// </summary>
        private void OnMouseDown()
        {
            parentTransform = transform.parent;
            oldPosition = parentTransform.position;
            oldRotation = parentTransform.rotation;
        }
        
        /// <summary>
        /// When this method is called, we will see where on the floor the player's mouse points and move the board
        /// there. We will also rotate the board so it is facing the camera, but only around the y-axis.
        /// </summary>
        private void OnMouseDrag()
        {
            if (MainCamera.Camera != null && !Raycasting.IsMouseOverGUI())
            {
                // Set the new position of the board
                Ray ray = Raycasting.UserPointsTo();
                floor.Raycast(ray, out float enter);
                Vector3 enterPoint = ray.GetPoint(enter);
                Vector3 newPosition = Vector3.zero;
                newPosition.x = enterPoint.x;
                newPosition.y = parentTransform.position.y;
                newPosition.z = enterPoint.z;
                parentTransform.position = newPosition;
                
                // Rotate the board to look in the direction of the player (except on the y-axis - we do not wish to
                // tilt the board)
                Vector3 facingDirection = newPosition - MainCamera.Camera.gameObject.transform.position;
                facingDirection.y = 0;
                parentTransform.rotation = Quaternion.LookRotation(facingDirection);
            }
        }

        /// <summary>
        /// When the player releases the mouse, we will transmit the changes to all players/clients.
        /// </summary>
        private void OnMouseUp()
        {
            new MoveBoardAction(
                parentTransform.GetComponent<WidgetsManager>().GetTitle(),
                oldPosition,
                parentTransform.position,
                oldRotation,
                parentTransform.rotation)
                .Execute();
        }
    }
}
