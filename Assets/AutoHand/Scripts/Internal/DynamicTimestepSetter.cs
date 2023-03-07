using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTimestepSetter : MonoBehaviour {

    public float slowestTimestep = 1 / 50f;
    public float fastestTimestep = 1 / 144f;

    float[] deltaTimeList = new float[10];
    Coroutine slowUpdateRounte = null;
    int id;

    private void OnEnable() {
        if(slowUpdateRounte != null)
            StopCoroutine(slowUpdateRounte);

        slowUpdateRounte = StartCoroutine(SlowUpdate());
        GameObject.DontDestroyOnLoad(gameObject);
    }

    private void OnDisable() {
        if(slowUpdateRounte != null)
            StopCoroutine(slowUpdateRounte);
    }

    IEnumerator SlowUpdate() {
        while(gameObject.activeInHierarchy) {

            deltaTimeList[id] = Time.deltaTime;
            id = (++id) % deltaTimeList.Length;
            Time.fixedDeltaTime = Mathf.Clamp(AverageDelta(), fastestTimestep, slowestTimestep);
            yield return new WaitForSecondsRealtime(0.02f);
        }

        slowUpdateRounte = null;
    }

    public float AverageDelta() {
        float averageVelocity = 0;
        if(deltaTimeList.Length > 0) {
            foreach(var deltaTime in deltaTimeList) {
                averageVelocity += deltaTime;
            }
            averageVelocity /= deltaTimeList.Length;
        }

        return averageVelocity;
    }
}
