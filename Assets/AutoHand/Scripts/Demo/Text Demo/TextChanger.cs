using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

namespace Autohand.Demo{
    public class TextChanger : MonoBehaviour{
        public TMPro.TextMeshPro text;
        Coroutine changing;
        Coroutine hide;
        
        public void UpdateText(string newText, float upTime) {

        }

        public void UpdateText(string newText) {

        }

        IEnumerator ChangeText(float seconds, string newText) {
            //float totalTime = 1f;
            //var timePassed = 0f;
            //text.text = newText;
            //text.alpha = 0;

            //while(timePassed <= totalTime) {
            //    text.alpha = (timePassed/totalTime);
            //    timePassed += Time.deltaTime;
            //    if(totalTime >= timePassed)
            //        text.alpha = 1;
            //    yield return new WaitForFixedUpdate();
            //}

            //yield return new WaitForSeconds(seconds);

            //totalTime = 2f;
            //timePassed = 0f;
            //while(timePassed <= totalTime) {
            //    text.alpha = 1-(timePassed/totalTime);
            //    timePassed += Time.deltaTime;
            //    if(totalTime >= timePassed)
            //        text.alpha = 0;
            //    yield return new WaitForFixedUpdate();
            //}

            yield return new WaitForFixedUpdate();
            text.text = "";
        }

        private void OnDestroy() {
            text.text = "";
        }
    }
}
