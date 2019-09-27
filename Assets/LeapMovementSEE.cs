using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapMovementSEE : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig;
    public float speedFactor = 0.01f;
    private float hightFactor = 1f;
    private float speed = 1f;
    private bool move = false;
    private Vector3 dir = new Vector3(0,0,0);

    void Start()
    {
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
                    dir = Camera.main.transform.forward;
                    speed = left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) * Time.deltaTime * speedFactor;
                    move = true;
                    if (left.Fingers[1].TipPosition.z < -50f && right.Fingers[1].TipPosition.z < -50f)
                    {
                        dir = Camera.main.transform.up;
                        speed = ((left.Fingers[1].TipPosition.z + right.Fingers[1].TipPosition.z) / 2 + 50f) * Time.deltaTime * speedFactor;
                    }
                    else if (left.Fingers[1].TipPosition.z > 50f && right.Fingers[1].TipPosition.z > 50f)
                    {
                        dir = Camera.main.transform.up;
                        speed = ((left.Fingers[1].TipPosition.z + right.Fingers[1].TipPosition.z) / 2 - 50f) * Time.deltaTime * speedFactor;
                    }
                }
                else if(left.PinchDistance < 20f && right.PinchDistance > 50f)
                {
                    dir = - Camera.main.transform.right;
                    speed = left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) * Time.deltaTime * speedFactor;
                    move = true;
                }
                else if(left.PinchDistance > 50f && right.PinchDistance < 20f)
                {
                    dir = Camera.main.transform.right;
                    speed = left.Fingers[1].TipPosition.DistanceTo(right.Fingers[1].TipPosition) * Time.deltaTime * speedFactor;
                    move = true;
                }
                else
                {
                    move = false;
                }
            }

            if (move)
            {
                rig.transform.Translate(dir * speed * hightFactor);
            }
        }
        else
        {
            Debug.Log("Leap motion is not connected");
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
}
