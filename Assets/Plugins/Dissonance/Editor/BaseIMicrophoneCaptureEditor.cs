using System;
using System.Collections.Generic;
using Dissonance.Audio.Capture;
using UnityEditor;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

namespace Dissonance.Editor
{
    public abstract class BaseIMicrophoneCaptureEditor<T>
        : UnityEditor.Editor
        where T : UnityEngine.Object, IMicrophoneCapture
    {
        #region fields and properties
        private Texture2D _logo;

        private readonly VUMeter _micMeter = new VUMeter();

        private DissonanceComms _comms;
        #endregion

        #region initialisation
        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }
        #endregion

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        [CanBeNull]
        private DissonanceComms FindComms()
        {
            if (!_comms)
            {
                var tgt = (MonoBehaviour)target;
                _comms = tgt.GetComponent<DissonanceComms>();
            }

            if (!_comms)
                _comms = FindObjectOfType<DissonanceComms>();

            return _comms;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            var capture = (T)target;
            DrawAmplitudeGui();
        }

        private void DrawAmplitudeGui()
        {
            var comms = FindComms();
            if (Application.isPlaying && comms != null)
            {
                var player = comms.FindPlayer(comms.LocalPlayerName);
                _micMeter.DrawInspectorGui(player == null ? 0 : player.Amplitude, player == null);
            }
        }

        protected void DrawMicSelectorGui([NotNull] T capture)
        {
            var comms = FindComms();
            if (comms == null)
            {
                EditorGUILayout.HelpBox("Cannot find DissonanceComms component in scene (required to configure microphone)", MessageType.Error);
                return;
            }

            string inputString;
            using (new EditorGUILayout.HorizontalScope())
            {
                //Allow the user to type an arbitrary input string
                inputString = EditorGUILayout.DelayedTextField("Microphone Device Name", comms.MicrophoneName ?? "(Use System Default)");
            }

            // Use the device list from the capture instance if possible. Otherwise use default Unity device list
            var devices = new List<string> { "(Use System Default)" };
            var dl = capture as IMicrophoneDeviceList;
            if (dl != null)
            {
                dl.GetDevices(devices);
            }
            else
            {
                devices.AddRange(Microphone.devices);
            }

            // Show buttons, one for each devices
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Input Devices", EditorStyles.boldLabel);

                foreach (var device in devices)
                    if (GUILayout.Button(device))
                        inputString = device;
            }

            //If the name is any of these special strings, default it back to null
            var nulls = new[] {
                "null", "(null)",
                "default", "(default)", "none default", "none (default)", "(use system default)",
                "none", "(none)"
            };
            if (string.IsNullOrEmpty(inputString) || nulls.Contains(inputString, StringComparer.InvariantCultureIgnoreCase))
                inputString = null;

            if (comms.MicrophoneName != inputString)
            {
                capture.ChangeWithUndo(
                    "Changed Dissonance Microphone",
                    inputString,
                    comms.MicrophoneName,
                    a => comms.MicrophoneName = a
                );
            }
        }
    }
}
