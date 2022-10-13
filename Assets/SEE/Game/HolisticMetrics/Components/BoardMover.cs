using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    public class BoardMover : MonoBehaviour
    {
        private static Plane plane = new Plane(Vector3.up, Vector3.zero);

        private Transform parentTransform;

        private void OnMouseDrag()
        {
            if (Camera.main != null)
            {
                parentTransform = transform.parent;
                // Set the new position of the board
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                plane.Raycast(ray, out float enter);
                Vector3 enterPoint = ray.GetPoint(enter);
                Vector3 newPosition = Vector3.zero;
                newPosition.x = enterPoint.x;
                newPosition.y = parentTransform.position.y;
                newPosition.z = enterPoint.z;
                parentTransform.position = newPosition;
                
                // Rotate the board to look in the direction of the player (except on the y-axis - we do not wish to
                // tilt the board)
                Vector3 facingDirection = newPosition - Camera.main.gameObject.transform.position;
                facingDirection.y = 0;
                parentTransform.rotation = Quaternion.LookRotation(facingDirection);
            }
        }

        
        private void OnMouseUp()
        {
            parentTransform = transform.parent;
            new MoveBoardNetAction(
                parentTransform.GetComponent<WidgetsManager>().GetTitle(),
                parentTransform.position,
                parentTransform.rotation)
                .Execute();
        }
    }
}
