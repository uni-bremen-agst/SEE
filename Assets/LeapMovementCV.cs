using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapMovementCV : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig; // assign in inspector

    public GameObject handAnchor; //assign in inspector
    public GameObject roomAnchor; // assign in inspector
    public LineRenderer line; // assign in inspector

    private bool presentRight = false;

    void Start()
    {
        Debug.Log("Starting LeapMovementCV\n");
        controller = new Controller();

        //make cubes spawn at camera offset
        //handAnchor.transform.Translate(Camera.main.transform.position);
        //handAnchor.transform.Translate(Vector3.down * 0.2f);

        roomAnchor.SetActive(false);

    }

    private void Awake()
    {
        
    }

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
            presentRight = false;

                for (int i = 0; i < hands.Count; i++)
                {
                    Hand hand = frame.Hands[i];

                    if (!hand.IsLeft)
                    {
                        right = hand;
                        presentRight = true;
                    }
                    else
                    {
                        left = hand;
                    }
                }

            
            
            if(presentRight && fist(right))
            {
                roomAnchor.SetActive(true);
                line.SetPosition(0, handAnchor.transform.position);
                line.SetPosition(1, roomAnchor.transform.position);

                rig.transform.Translate((handAnchor.transform.position - roomAnchor.transform.position) * 0.01f);
            }
            else
            {
                roomAnchor.SetActive(false);
                line.SetPosition(0, handAnchor.transform.position);
                line.SetPosition(1, handAnchor.transform.position);
            }
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
