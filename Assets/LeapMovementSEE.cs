using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapMovementSEE : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig;
    public float speed = 1f;

    void Start()
    {
        controller = new Controller();
    }

    void Update()
    {
        if (controller.IsConnected)
        {
            Frame frame = controller.Frame();
            Hand left = new Hand();
            Hand right = new Hand();
            List<Hand> hands = frame.Hands;
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

                if (left.PinchDistance < 20f)
                {
                    Finger point = right.Fingers[1];
                    Vector palmVector = right.PalmNormal;
                    Vector direction = point.Direction;
                    Vector3 camDir = Camera.main.transform.forward;
                    Vector3 move = new Vector3(-direction.x, palmVector.y, -direction.z);
                    rig.transform.Translate(camDir * speed);
                    Debug.Log("y: " + palmVector.y + "x: " + direction.x + "z: " + direction.z);
                }
            }
        }
        else
        {
            Debug.Log("Leap motion is not connected");
        }

    }
}
