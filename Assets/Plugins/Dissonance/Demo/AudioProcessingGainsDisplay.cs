using System;
using UnityEngine;

namespace Dissonance.Demo
{
    public class AudioProcessingGainsDisplay
        : MonoBehaviour
    {
        private readonly float[] _gains = new float[22];

        private AudioProcessingTestSetup _processor;

        public RectTransform[] Bars;
        private RectTransform _self;

        private void Start()
        {
            _processor = GetComponentInParent<AudioProcessingTestSetup>();
            _self = GetComponent<RectTransform>();
        }

        private void Update()
        {
            var gcount = _processor.GetGains(_gains);

            for (var i = 0; i < Bars.Length; i++)
            {
                var v = i >= gcount ? 0 : _gains[i];
                var sz = Bars[i].sizeDelta;
                sz.y = _self.rect.height * v;
                Bars[i].sizeDelta = sz;
            }
        }
    }
}
