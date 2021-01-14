using System;
using UnityEngine;


public class DrawLine : MonoBehaviour
{
	private LineRenderer lineRenderer;
	private float counter;
	private float dist;

	public Transform origin;
	public Transform destination;

	public float lineDrawSpeed = 6f;

	//Use this for initialization
	void Start()
    {
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.SetPosition(0, origin.position);
		lineRenderer.SetWidth(.45f, .45f);

		dist = Vector3.Distance(origin.position, destination.position);
    }

	//Update is called once per frame
	void Update()
    {
		if (counter < dist)
        {
			counter += .1f / lineDrawSpeed;

			float x = Mathf.Lerp(0, dist, counter);

			Vector3 pointA = origin.position;
			Vector3 pointB = destination.position;

			//Get the unit in the desired direction, multiply by the desired length and add the starting point.
			Vector3 pointAlongLine = x* Vector3.Normalize(pointB-pointA)+pointA;

			lineRenderer.SetPosition(1, pointAlongLine);
        }
    }
}
