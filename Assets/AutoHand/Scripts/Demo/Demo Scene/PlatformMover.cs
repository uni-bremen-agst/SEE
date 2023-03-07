using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMover : MonoBehaviour
{
    public Vector3 toRange;
    public float time = 1;

    Vector3 startPos;
    // Start is called before the first frame update
    void Start() {
        startPos = transform.position;
        StartCoroutine(Move());
    }

    IEnumerator Move() {
        while (true) {
            var timePassed = 0f;
            while(timePassed < time){
                timePassed += Time.fixedDeltaTime;
                transform.position = Vector3.Lerp(startPos, startPos+toRange, timePassed/time);
                yield return new WaitForEndOfFrame();
            }
            transform.position = toRange;

            timePassed = 0f;
            while(timePassed < time){
                timePassed += Time.fixedDeltaTime;
                transform.position = Vector3.Lerp(startPos + toRange, startPos, timePassed/time);
                yield return new WaitForEndOfFrame();
            }
            transform.position = toRange;
        }
    }
}
