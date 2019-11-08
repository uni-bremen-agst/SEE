using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapMovementCV : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig; // assign in inspector
    public Canvas menu; // assign in inspector

    void Start()
    {
        Debug.Log("Starting LeapMovementCV\n");
        controller = new Controller();
        menu.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.IsConnected)
        {
            // it would be better to get the hand objects directly from the unity hierarchy
            // get both hands
            Frame frame = controller.Frame();
            Hand left = new Hand();
            Hand right = new Hand();
            List<Hand> hands = frame.Hands;

                for (int i = 0; i < hands.Count; i++)
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

            //observing the triggers
            menu.enabled = palmUp(right);
            menu.transform.LookAt(Camera.main.transform);

        }
    }

    bool palmUp(Hand hand)
    {
        if (hand.PalmNormal.z > 0.8)
        {
            return true;
        }
        return false;
    }
}
