using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    // ReSharper disable once InconsistentNaming
    public class VUMeter
    {
        private float _smooth;
        private float _maxVolume = 0.01f;
        private float _peak;

        private readonly Stopwatch _time;

        public VUMeter()
        {
            _time = new Stopwatch();
        }

        public void DrawInspectorGui(float amplitude, bool clear)
        {
            // Keep track of time between calls
            var dt = (float)_time.Elapsed.TotalSeconds;
            _time.Reset();
            _time.Start();

            // Smooth out the signal slightly by mixing it with the previous value
            _smooth = amplitude * 0.3f + _smooth * 0.7f;

            // Stretch the meter to the largest signal ever encountered
            _maxVolume = Mathf.Max(_smooth, _maxVolume);

            // Reset if necessary
            if (clear)
            {
                _smooth = 0;
                _maxVolume = 0;
                _peak = 0;
            }

            // Update peak volume
            if (_smooth > _peak)
                _peak = _smooth;
            else
                _peak -= dt * _maxVolume * 0.5f;
            _peak = Mathf.Clamp01(_peak);

            // Draw background
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.DrawRect(rect, Color.gray);

            // Draw markers
            for (var i = 1; i < 101; i++)
            {
                var v = i / 100f;
                var xv = rect.width * v / _maxVolume;

                if (xv < rect.width - 1)
                    EditorGUI.DrawRect(new Rect(rect.xMin + xv - 1, rect.yMin + 1, 2, rect.height - 2), new Color(0.1f, 0.1f, 0.1f, 0.25f));
            }

            // Draw current amplitude indicator
            var c = Color.HSVToRGB(Mathf.Lerp(0.5f, 0, _smooth / _maxVolume), 0.8f, 0.8f);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width * _smooth / _maxVolume, rect.height), c);
            
            // Draw peak amplitude indicator
            var x = rect.width * _peak / _maxVolume;
            EditorGUI.DrawRect(new Rect(rect.xMin + x - 1, rect.yMin, 2, rect.height), Color.red);

            // Draw dbfs
            var dbfs = Helpers.ToDecibels(_maxVolume);
            EditorGUI.LabelField(new Rect(rect.xMax - 72, rect.yMin, 72, rect.height), string.Format("{0:0.0} dBFS", dbfs));
        }
    }
}
