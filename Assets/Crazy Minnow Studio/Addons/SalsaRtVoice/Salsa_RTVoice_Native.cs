using UnityEngine;
using System;
using System.Collections.Generic;
using Crosstales.RTVoice;
using Random = UnityEngine.Random;

namespace CrazyMinnow.SALSA.RTVoice
{
    /// <summary>
    /// SALSA / RT-Voice EXAMPLE integration for implementations using RT-Voice in native mode. You may use and
    /// modify this script or create your own script to match your personal design requirements. This is only one
    /// example of how to accomplish this integration.
    ///
    /// This script subscribes to the OnSpeakNativeCurrentViseme event, and categorizes
    /// the 21 SAPI visemes into SALSA's four mouth shapes.
    ///
    /// There are 21 SAPI visemes:
    /// 	0 = rest
    /// 	1 = ax, ah, uh
    /// 	2 = aa
    /// 	3 = ao
    /// 	4 = ey, eh, ae
    /// 	5 = er
    /// 	6 = y, iy, ih, ix
    /// 	7 = w, uw, u
    /// 	8 = ow
    /// 	9 = aw
    /// 	10 = oy
    /// 	11 = ay
    /// 	12 = h
    /// 	13 = r
    /// 	14 = l
    /// 	15 = s, z, ts
    /// 	16 = sh, ch, jh, zh
    /// 	17 = th, dh
    /// 	18 = f, v
    /// 	19 = d, t, dx, n
    /// 	20 = k, g, ng
    /// 	21 = p, b, m
    ///
    /// Sort the 21 SAPI visemes into SALSA's four shapes
    ///     sayRest     0, 17, 18, 21
    ///     saySmall    6, 12, 13, 16, 19, 20
    ///     sayMedium   4, 11, 14, 15
    ///     sayLarge    1, 2, 3, 5, 7, 8, 9, 10
    /// </summary>
    [AddComponentMenu("Crazy Minnow Studio/SALSA LipSync/Add-ons/Salsa_RTVoice_Native")]
    public class Salsa_RTVoice_Native : MonoBehaviour
    {
        public string speakText = "This is a test using SALSA with RT-Voice in native mode"; // Text to pass to SpeakNative
        public bool speak; // Speak trigger for editor testing
        public bool usePreferredSalsaSettings = true;
        public VisemeSet visemeSet = VisemeSet.Seven;

        public enum VisemeSet
        {
            Three,
            Seven
        }

        public Salsa salsa; // Salsa3D component
        private int currentViseme = 0; // current viseme index
        private List<List<int>> salsaVisemeOptions = new List<List<int>>();
        private float[] ranges;

        // Uid must be set with the correct id from Speaker SpeakNative in order to filter out unwanted speakers.
        private string uid;

        /// <summary>
        /// Get the Salsa component and add the viseme collections to the salsaVisemeOptions collection.
        /// </summary>
        void Awake()
        {
            // NOTE: the following setup the viseme matches from the returned OnSpeakNativeCurrentViseme event data.
            // If you are using more (or less) visemes, you will need to adjust these mappings based on the RT-Voice
            // documentation and the comments above.
            // NOTE: The following are applied as silence/rest by not allocating them to a viseme:
            switch (visemeSet)
            {
                case VisemeSet.Three:
                    // 3-viseme setup
                    // 0, 17, 18, 21 = silence/rest/not-assigned
                    salsaVisemeOptions.Add(new List<int> {6, 12, 13, 16, 19, 20}); // small
                    salsaVisemeOptions.Add(new List<int> {4, 11, 14, 15}); // medium
                    salsaVisemeOptions.Add(new List<int> {1, 2, 3, 5, 7, 8, 9, 10}); // large
                    break;
                case VisemeSet.Seven:
                    // 7-viseme setup
                    salsaVisemeOptions.Add(new List<int> { 3, 7, 16 });  // w
                    salsaVisemeOptions.Add(new List<int> { 4, 19, 20 });  // t
                    salsaVisemeOptions.Add(new List<int> { 18 });  // f
                    salsaVisemeOptions.Add(new List<int> { 12, 13, 14, 17 });  // th
                    salsaVisemeOptions.Add(new List<int> { 1, 2, 11 });  // ow
                    salsaVisemeOptions.Add(new List<int> { 5, 6, 15 });  // ee
                    salsaVisemeOptions.Add(new List<int> { 8, 9, 10 });  // oo
                    break;
            }


            if (!salsa)
                salsa = GetComponent<Salsa>();

        }

        private void Start()
        {
            if (salsa)
            {
                // Edit these values to your design preferences.
                if (usePreferredSalsaSettings)
                {
                    salsa.DistributeTriggers(LerpEasings.EasingType.Linear);
                    salsa.useAdvDyn = true;
                    salsa.advDynPrimaryBias = 0.6f;
                    salsa.useAdvDynJitter = true;
                    salsa.advDynJitterAmount = 0.1f;
                    salsa.advDynJitterProb = 0.2f;
                    salsa.loCutoff = 0.0f;
                    salsa.hiCutoff = 1.0f;
                    salsa.useExternalAnalysis = true;
                    salsa.audioUpdateDelay = 0.08f;
                    salsa.useTimingsOverride = true;
                    salsa.globalDurON = 0.1f;
                    salsa.globalNuanceBalance = -0.5f;
                    salsa.audioUpdateDelay = salsa.globalDurON + salsa.globalDurON * salsa.globalNuanceBalance;
                    salsa.globalDurOffBalance = 0.13f;
                    salsa.globalDurOFF = Mathf.Max(salsa.globalDurON + salsa.globalDurON * salsa.globalDurOffBalance, 0.0f);
                }

                // Creates a mapping of upper/lower trigger bounds for SALSA visemes. During allocation in the event
                // callback, a random analysis value is created to take advantage of SALSA's Advanced Dynamics.
                ranges = new float[salsa.visemes.Count];
                for (int i = 0; i < ranges.Length; i++)
                {
                    if (i == salsa.visemes.Count - 1)
                        ranges[i] = 1.0f - salsa.visemes[i].trigger;
                    else
                        ranges[i] = salsa.visemes[i + 1].trigger - salsa.visemes[i].trigger; // upper bound - lower bound
                }

            }
            else
                Debug.LogWarning("SalsaTextSync requires a link to SALSA for operation.");
        }

        /// <summary>
        /// Subscrive to the OnSpeakNativeCurrentViseme event OnEnable
        /// </summary>
        void OnEnable()
        {
            Speaker.Instance.OnSpeakCurrentViseme += Speaker_OnSpeakCurrentViseme;
        }

        /// <summary>
        /// Unsubscrive from the OnSpeakCurrentViseme event OnDisable
        /// </summary>
        void OnDisable()
        {
            if (Speaker.Instance != null)
                Speaker.Instance.OnSpeakCurrentViseme -= Speaker_OnSpeakCurrentViseme;
        }

        /// <summary>
        /// Listenter for the OnSpeakCurrentViseme event.
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="rtViseme"></param>
        public void Speaker_OnSpeakCurrentViseme(Crosstales.RTVoice.Model.Wrapper wrapper, string rtViseme)
        {
            // check to confirm this speaker is speaking
            if (wrapper.Uid != uid) return;

            if (rtViseme == "")
                currentViseme = 0;
            else
                currentViseme = Convert.ToInt32(rtViseme);


            // Set SALSA analysis value based on the current viseme index, ensuring an appropriate viseme
            // is configured on SALSA so we don't overrun the viseme count.
            if (salsa)
            {
                for (int i = 0; i < salsaVisemeOptions.Count && i < salsa.visemes.Count; i++)
                {
                    if (salsaVisemeOptions[i].Contains(currentViseme))
                    {
                        salsa.analysisValue = salsa.visemes[i].trigger + Random.Range(0.001f, ranges[i]);

                        return;
                    }
                }

                // catch all...rt-viseme not allocated = SILENCE/REST
                salsa.analysisValue = 0.0f;
            }
        }

        /// <summary>
        /// This is ONLY used for testing and can be deleted in an implementation where you
        /// make your own call's to [Speaker.SpeakNative]. Click [Speak] in this inspector
        /// to demonstrate send the [speakText] to the [Speaker.SpeakNative] RT-Voice method.
        /// </summary>
        private void LateUpdate()
        {
            if (speak)
            {
                speak = false;
                Speak(speakText);
            }
        }

        /// <summary>
        /// Calls Speaker SpeakNative and sets the local uid field from the Speaker SpeakNative return so this script can filter out unwanted speakers.
        /// </summary>
        /// <param name="speakString"></param>
        public void Speak(string speakString)
        {
            uid = Speaker.Instance.SpeakNative(speakString, Speaker.Instance.VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender.MALE, "en"), 1.0f);
        }
    }
}