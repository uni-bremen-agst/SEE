using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///This class implements all positioning changing functions of a player controled rig.
/// </summary>
public class RigMovement : MonoBehaviour
{

    /// <summary>
    /// The factor of speed depending on the hight of the moving object.
    /// </summary>
    public float HightFactor = 1f;

    /// <summary>
    /// The delegate to hold function that needs more than one tick for executing.
    /// Needs to be null after task is done.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    private delegate void MovementLock(Vector3 v1, Vector3 v2);
    MovementLock Lock;
    Vector3 v1, v2;

    void Update()
    {
        HightFactor = Mathf.Pow(gameObject.transform.position.y, 2) * 0.01f + 1;
        if (HightFactor > 5)
        {
            HightFactor = 5;
        }

        if(Lock != null)
        {
            Lock(v1, v2);
        }
    }

    public void MoveForward(float axis)
    {
        transform.Translate(transform.forward * axis * Time.deltaTime);
    }

    public void MoveBackward(float axis)
    {
        transform.Translate(transform.forward * -1 * axis * Time.deltaTime);
    }

    public void MoveUpward(float axis)
    {
        transform.Translate(transform.up * axis * Time.deltaTime);
    }

    public void MoveDownward(float axis)
    {
        transform.Translate(transform.up * -1 * axis * Time.deltaTime);
    }

    public void MoveLeftward(float axis)
    {
        transform.Translate(transform.right * axis * Time.deltaTime);
    }

    public void MoveRightward(float axis)
    {
        transform.Translate(transform.right * -1 * axis * Time.deltaTime);
    }

    /// <summary>
    /// Rotates the GameObject around the y-axis against clockwise direction.
    /// </summary>
    /// <param name="axis">the factor for angle of rotation</param>
    public void TurnLeft(float axis)
    {

    }

    /// <summary>
    /// Rotates the GameObject around the y-axis in clockwise direction.
    /// </summary>
    /// <param name="axis">the factor for angle of rotation</param>
    public void TurnRight(float axis)
    {

    }

    /// <summary>
    /// Moves the object into given direction by the distance of the given axis value per tick.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="axis"></param>
    public void MoveInDirection(Vector3 direction, float axis)
    {
        gameObject.transform.Translate(direction * axis * HightFactor);
    }

    /// <summary>
    /// Turns the object smoothly into the given direction.
    /// Only rotates around the y-axis.
    /// </summary>
    /// <param name="direction">the direction the object is supposed to face in</param>
    /// <param name="speed"></param>
    public void LookInDirection(Vector3 direction, float speed)
    {

    }

    /// <summary>
    /// Moves the object smoothly to the given position.
    /// </summary>
    /// <param name="newPosition"></param>
    /// <param name="speed"></param>
    public void MoveToPosition(Vector3 newPosition, float speed)
    {

    }

    /// <summary>
    /// Instantly spawnes the attached GameObject at the new position.
    /// </summary>
    /// <param name="newPosition">the new position</param>
    /// <param name="newDirection">the direction the GameObject ist supposewd to face at</param>
    public void Teleport(Vector3 newPosition, Vector3 newDirection)
    {
        gameObject.transform.position = newPosition;
        gameObject.transform.rotation = Quaternion.LookRotation(newDirection,Vector3.up);
    }

    /// <summary>
    /// Moves the attached GameObject to the new location along a smooth curve above the map.
    /// Is a macro/movement and takes longer than one tick.
    /// </summary>
    /// <param name="position">the target position</param>
    /// <param name="direction">the direction the object is supposed to face at the end</param>
    public void Travel(Vector3 position, Vector3 direction)
    {
        if(Lock == null)
        {
            Lock = Travel;
            v1 = position;
            v2 = direction;
        }
        else
        {
            //travel behavior

            if(position == transform.position && direction == transform.forward)
            {
                Lock = null;
            }
        }

    }

    /// <summary>
    /// Lets the GameObject moving around the target location in orbits and adjust the center point of the view to the target.
    /// </summary>
    /// <param name="location">the position of the target object to circle around</param>
    /// <param name="direction">x is the factor for moving right, y the factor foe moving up and z the factor for the forward component of the movement</param>
    public void CircleAround(Vector3 target, Vector3 direction)
    {
        Debug.Log("Circle function got called");
        float DistanceFactor = Vector3.Distance(transform.position, target);  //needs to be tuned

        Vector3 nextPos = transform.position;
        nextPos += transform.right * direction.x * DistanceFactor * Time.deltaTime;
        nextPos += transform.up * direction.y * DistanceFactor * Time.deltaTime;
        nextPos += transform.forward * direction.z * DistanceFactor * Time.deltaTime;

        //checks if the next position is not benath the citys ground level
        if(nextPos.y >= 1)
        {
            transform.position = nextPos;
            transform.LookAt(target);
        }
    }

}
