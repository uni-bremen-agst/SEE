using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public class BlinkEffect : MonoBehaviour
    {
        public GameObject line;
        private bool loopOn;
        new LineRenderer renderer;
        public BlinkEffect(GameObject line)
        {
            this.line = line;
        }
        // Use this for initialization
        /*
        public void Start()
        {
            if (renderer == null)
            {
                renderer = line.GetComponent<LineRenderer>();
            }
            loopOn = true;
            StartCoroutine(Blink());
        }*/

        IEnumerator Blink()
        {
            while (loopOn)
            {
                renderer.enabled = false;
                yield return new WaitForSeconds(0.2f);
                renderer.enabled = true;
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void Deactivate()
        {
            loopOn = false;
            renderer.enabled = true;
        }

        public void LoopReverse()
        {
            loopOn = !loopOn;
            if (loopOn)
            {
                Activate();
            } else
            {
                Deactivate();
            }
        }

        public bool GetLoopStatus()
        {
            return loopOn;
        }

        public void Activate()
        {
            // Start();
            if (renderer == null)
            {
                renderer = line.GetComponent<LineRenderer>();
            }
            loopOn = true;
            StartCoroutine(Blink());
        }
    }
}