using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class ScaleHighlight : MonoBehaviour{
        public Vector3 highlighScale;
        public Vector3 normalScale;

        public void Highlight() {
            transform.localScale = highlighScale;
        }

        public void HighlightStop() {
            transform.localScale = normalScale;
        }
    }
}
