using UnityEngine;

[System.Obsolete("Will be removed when the transition to new design of input-actions mapping is implemented.")]
public class TouchStickSEE : MonoBehaviour
{
    private Collider2D collider;
    private Vector2 oldScreenPos;

    void Start()
    {
        collider = GetComponent<Collider2D>();
        oldScreenPos = transform.position;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Vector3 touchPointInWorldSpace = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            Vector2 touchPointScreen = new Vector2(touchPointInWorldSpace.x, touchPointInWorldSpace.y);

            if (collider == Physics2D.OverlapPoint(touchPointScreen))
            {
                Vector2 newPos = touchPointScreen - oldScreenPos;
                transform.position = newPos;
            }
            else if(Input.touchCount == 0)
            {
                transform.localPosition = Vector2.zero;
            }

        }
    }
}
