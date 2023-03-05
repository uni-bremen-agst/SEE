using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowlingManager : MonoBehaviour
{
    [Header("Bowling Ball Settings")]
    public GameObject bowlingBall;

    [Header("Bowling Pin Settings")]
    public Vector3 pinCenter;
    public float pinSpaceX;
    public float pinSpaceZ;

    public List<GameObject> pins = new List<GameObject>();

    public Vector3 ballPosition;
    private void Start()
    {
        ballPosition = bowlingBall.transform.position;
        ResetPins();
    }

    public void ResetBall()
    {
        bowlingBall.GetComponent<Rigidbody>().isKinematic = true;
        bowlingBall.transform.position = ballPosition;
        bowlingBall.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void ResetPins()
    {
        int max = (pins.Count / 2) - 2;
        int topRow = (pins.Count / 2) - 2; ;
        int row = 0;

        float currentX = pinCenter.x;
        float currentZ = pinCenter.z;

        for (int i = 0; i < pins.Count; i++)
        {
            pins[i].GetComponent<Rigidbody>().isKinematic = true;

            pins[i].transform.rotation = Quaternion.Euler(0, 0, 0);
            pins[i].transform.position = new Vector3(currentX, pinCenter.y, currentZ);

            pins[i].GetComponent<Rigidbody>().isKinematic = false;

            currentZ += pinSpaceZ;

            if (row == topRow)
            {
                topRow--;
                row = 0;

                currentZ = pinCenter.z + ((pinSpaceZ / 2) * (max - topRow));
                currentX = pinCenter.x + (pinSpaceX * (max - topRow));
            }
            else
            {
                row++;
            }
        }
    }
}
