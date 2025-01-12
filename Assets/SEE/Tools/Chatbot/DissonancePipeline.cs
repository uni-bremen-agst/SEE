using Dissonance;
using Michsky.UI.ModernUIPack;
using NAudio.Wave;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphSearch;
using SEE.Game;
using SEE.GO;
using SEE.UI.Notification;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using Whisper;
using SEE.Game.City;
using SEE.Game.Avatars;

namespace Assets.SEE.Tools.Chatbot
{

    public class DissonancePipeline : BaseMicrophoneSubscriber
    {
        static int counter = 0;
        [SerializeField]
        static bool IsListening = false;
        static string OutputFilePath = @"C:\Users\Sarah\Documents\Bachelorarbeit"; //only used for testing
        static string filename = "/audio";
        static string fileExtension = ".wav";
        static float[] audioData;
        static WaveFileWriter _waveWriter;

        static int sampleRate;
        static int channel;
        private List<float> _audioData = new List<float>();
        private int _sampleRate = 44100;  // Default sample rate
        private AudioClip _audioClip;

        // Max duration of the audio clip (in seconds)
        private float _maxClipLength = 10f;

        WhisperManager whisperManager;
        // Flag to indicate if recording is in progress

        [SerializeField] bool evaluationRecord = false; // just for evaluation or debugging helpful

        private void Start()
        {
            // Subscribe to the recorded audio stream
            FindObjectOfType<DissonanceComms>().SubscribeToRecordedAudio(this);
            whisperManager = FindAnyObjectByType<WhisperManager>();
        }
        Graph graph = new Graph();

        private void Update()
        {
            base.Update(); // Ensures the base class `Update()` is called

            if (SEEInput.ToggleVoiceControl())
            {
                if (!IsListening)
                {
                    // Start recording when the input is held down
                    ShowNotification.Info("Started listening", "Recording...", 5f);
                    StartListen();
                }
            }
            else
            {
                if (IsListening)
                {
                    // Stop recording when the input is released
                    ShowNotification.Info("Stopped listening", "Saved file.", 5f);
                    StopListen();
                }
            }

            }


        public void StartListen()
        {
            // Set the recording flag to true
            IsListening = true;

            // Reset the current audio data buffer
            _audioData.Clear();

        }


        public async void StopListen()
        {
            // Set the recording flag to false
            IsListening = false;

            if (evaluationRecord)
            {
                // Dispose of the WaveFileWriter when done recording
                _waveWriter?.Dispose();
                _waveWriter = null;
            }

            // Generate the AudioClip from the recorded data
            AudioClip recordedClip = GetAudioClip();

            // Optionally, handle saving or playing the clip here
            if (recordedClip != null)
            {
                Debug.Log("Recording stopped, audio clip created");

                // Just for debbuging
                //AudioSource.PlayClipAtPoint(recordedClip, Vector3.zero);
                UnityDispatcher.Enqueue(() =>
                {
                    PersonalAssistantBrain.Instance.Say("Let's see how i can help you. Give me some time.");
                });
                // provide recording to whisper
                var whisperResult = await whisperManager.GetTextAsync(recordedClip);

                Debug.LogError(whisperResult.Result);
                StartCoroutine(ChatbotDialogHandler.SendMessageToRasa(whisperResult.Result));
            }
        }

        protected override void ProcessAudio(ArraySegment<float> data)
        {
            // Only process audio data if we are currently recording
            if (IsListening)
            {
                // Append the incoming data to the audio buffer
                _audioData.AddRange(data);

                // Limit the size of the audio data to prevent overflow (e.g. limit to max duration)
                int maxSamples = (int)(_maxClipLength * _sampleRate);
                if (_audioData.Count > maxSamples)
                {
                    _audioData.RemoveRange(0, _audioData.Count - maxSamples);
                }

                if (evaluationRecord && _waveWriter != null)
                {

                    audioData = data.ToArray();
                    _waveWriter.WriteSamples(data.Array, data.Offset, data.Count);
                }
            }

        }

        protected override void ResetAudioStream(WaveFormat waveFormat)
        {
            // Handle format changes, reset any audio processing if needed
            _sampleRate = waveFormat.SampleRate;
            _audioData.Clear();


            // Create a new WaveFileWriter with the new format
            if (evaluationRecord)
            {
                // Dispose of the previous writer if it exists
                _waveWriter?.Dispose();
                _waveWriter = new WaveFileWriter(OutputFilePath + filename + counter + fileExtension, waveFormat);
            }
        }


        /// <summary>
        /// Gives access to a audioclip, needed for whisper later on
        /// </summary>
        /// <returns></returns>
        public AudioClip GetAudioClip()
        {
            if (_audioData.Count == 0)
            {
                Debug.LogWarning("No audio data available");
                return null;
            }

            // Create an AudioClip from the stored audio data
            _audioClip = AudioClip.Create("MicrophoneClip", _audioData.Count, 1, _sampleRate, false);

            // Copy the float data into the AudioClip
            _audioClip.SetData(_audioData.ToArray(), 0);

            return _audioClip;
        }

        private void OnDestroy()
        {
            StopListen();
        }

    }

}
