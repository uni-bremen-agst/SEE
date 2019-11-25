using System.Collections.Generic;
using UnityEngine;
using Leap;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class LeapMovementSEE : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig;
    public float speedFactor = 0.5f;
    private float hightFactor = 1f;
    private float speed = 1f;
    private bool move = false;
    private Vector3 dir = new Vector3(0,0,0);

    [SerializeField]
    [FormerlySerializedAs("OnLeftFist")]
    private UnityEvent _OnLeftFist = new UnityEvent();

    public 


    void Start()
    {
        Debug.Log("Starting LeapMovementSEE\n");
        controller = new Controller();
    }

    void Update()
    {
        if (controller.IsConnected)
        {
            // get both hands
            Frame frame = controller.Frame();
            Hand left = new Hand();
            Hand right = new Hand();
            List<Hand> hands = frame.Hands;
            hightFactor = Mathf.Pow(rig.transform.position.y, 2) * 0.01f + 1;
            if (hightFactor > 5)
            {
                hightFactor = 5;
            }
            if (hands.Count == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    Hand hand = frame.Hands[i];

                    if (!hand.IsLeft)
                    {
                        right = hand;
                    }
                    else
                    {
                        left = hand;
                    }
                }
                // checking for double pinch to move forward
                if (TwoThumbsToIndex(left, right))
                {
                    speed = left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) * Time.deltaTime * speedFactor;
                    dir = Camera.main.transform.forward * speed;
                    move = true;

                    MoveUpAndDown(right, left, ref dir);
                }
                // checking if the left hand is pinching and the right not to moving leftwards
                // movement speed depends on teh distance between both thumbs
                else if (left.PinchDistance < 20f && right.PinchDistance > 50f)
                {
                    speed = left.Fingers[0].TipPosition.DistanceTo(right.Fingers[0].TipPosition) * Time.deltaTime * speedFactor;
                    dir = -Camera.main.transform.right * speed;
                    move = true;

                    MoveUpAndDown(right, left, ref dir);
                }
                // checking if the right hand is pinching and the left not for moving rightwards
                // movement speed depends on teh distance between both thumbs
                else if(left.PinchDistance > 50f && right.PinchDistance < 20f)
                {
                    speed = left.Fingers[0].TipPosition.DistanceTo(right.Fingers[0].TipPosition) * Time.deltaTime * speedFactor;
                    dir = Camera.main.transform.right * speed;
                    move = true;

                    MoveUpAndDown(right, left, ref dir);
                }
                //checking if the thumbs are together and the indexfinger not for moving bachwards
                // movement speed depends on the distance between the indexfingers
                else if(left.Fingers[0].TipPosition.DistanceTo(right.Fingers[0].TipPosition) < 30f && left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) > 120f)
                {
                    speed = (left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) - 120) * Time.deltaTime * speedFactor;
                    dir = -Camera.main.transform.forward * speed;
                    move = true;

                    MoveUpAndDown(right, left, ref dir);
                }
                else
                {
                    move = false;
                }
            }

            if(fist(left))
            {

            }

            if (move)
            {
                rig.transform.Translate(dir * hightFactor);
            }
        }
        else
        {
            Debug.Log("Leap motion is not connected");
        }
    }

    void MoveUpAndDown(Hand right, Hand left, ref Vector3 direction)
    {
        // checking if double pinch happens in the lower half of the camera angle to move upwards
        // movementspeed depends on the distance between both index fingers and the z=0 level
        if (left.Fingers[1].TipPosition.z < -50f && right.Fingers[1].TipPosition.z < -50f)
        {
            float upSpeed = ((left.Fingers[1].TipPosition.z + right.Fingers[1].TipPosition.z) / 2 + 50f) * Time.deltaTime * speedFactor * 0.5f;
            dir = dir + (Camera.main.transform.up * upSpeed);
        }
        // checking if double pinch happens in the upper half of the camera angle to move downwards
        // movementspeed depends on the distance between both index fingers and the z=0 level
        else if (left.Fingers[1].TipPosition.z > 50f && right.Fingers[1].TipPosition.z > 50f)
        {
            float downSpeed = ((left.Fingers[1].TipPosition.z + right.Fingers[1].TipPosition.z) / 2 - 50f) * Time.deltaTime * speedFactor * 0.5f;
            dir = dir + (Camera.main.transform.up * downSpeed);
        }
    }

    bool TwoThumbsToIndex(Hand right, Hand left)
    {
        if (left.PinchDistance < 20f && right.PinchDistance < 20f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool fist(Hand hand)
    {
        if (hand.PinchDistance < 20)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
