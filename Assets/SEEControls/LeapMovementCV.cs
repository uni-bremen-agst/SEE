using System.Collections.Generic;
using UnityEngine;
using Leap;

[System.Obsolete("Will be removed when the transition to new design of input-actions mapping is implemented.")]
public class LeapMovementCV : MonoBehaviour
{
    private Leap.Controller controller;
    public GameObject rig; // assign in inspector

    public GameObject handAnchor; //assign in inspector
    public LineRenderer line; // assign in inspector

    public GameObject grid;
    private Vector3 gridOffset = new Vector3(0, 0.2f, 0);

    private bool collision = false;
    private GameObject collisionCube;

    private bool presentRight = false;

    void Start()
    {
        Debug.Log("Starting LeapMovementCV\n");
        controller = new Controller();

        //make cubes spawn at camera offset
        grid = Instantiate(grid, Camera.main.transform.position - gridOffset, Quaternion.identity);
        grid.transform.SetParent(gameObject.transform);

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

            grid.transform.position = Camera.main.transform.position - gridOffset;

            if (collision)
            {
                if (fist(left))
                {
                    line.SetPosition(0, handAnchor.transform.position);
                    line.SetPosition(1, collisionCube.transform.position);

                    rig.transform.Translate((handAnchor.transform.position - collisionCube.transform.position) * 0.05f);
                }
                else
                {
                    line.SetPosition(0, handAnchor.transform.position);
                    line.SetPosition(1, handAnchor.transform.position);
                }
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

    void ShowUp(GameObject [] obj)
    {
        Debug.Log("Collsision Enter Method\n");
        collision = true;
        collisionCube = obj[0];
        Debug.Log(obj[1].name);

    }

    void Hide(GameObject obj)
    {
        collision = false;
    }
}
