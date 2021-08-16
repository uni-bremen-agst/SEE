package com.crosstales.RTVoice;

//region Imports

import android.annotation.TargetApi;
import android.content.Context;
import android.os.AsyncTask;
import android.os.Build;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.speech.tts.Voice;
import android.util.Log;

import java.io.File;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Locale;
import java.util.Set;
import java.util.List;
import java.util.ArrayList;

//endregion

/**
 * RTVoiceAndroidBridge.java
 * Version 2021.2.5
 * 
 * Acts as a handler for all TTS functions called by RT-Voice on Android.
 *
 * © 2016-2021 crosstales LLC (https://www.crosstales.com)
 */
public class RTVoiceAndroidBridge {

    //region Variables

    public static final String VERSION = "2021.2.0";

    //Context to instantiate TTS engine
    private static Context appContext;

    //TTS object
    private static TextToSpeech tts;
    
    private static Set<Voice> voices;
    
    //TTS engine is initialized
    private static boolean initialized;

    //TTS engine is currently busy
    private static boolean working = false;

    //pathname of the generated WAV file
    private static String outputFile;

    // Volume for native speaking
    private static float nativeVolume = 1f;

    // Set of all available Locales (SDK < 21)
    private static Set<Locale> locales;

    // Tag for the logs
    private static final String TAG = "RTVoiceAndroidBridge";

    private static final boolean DEBUG = false; //Change to enable debug logs

    //endregion

    //region Constructor

    /**
     * Constructor for the RTVoiceAndroidBridge class.
     * The appContext must contain the application context so we can initialize the TTS engine.
     *
     * @param appContext Application context of the Unity application
     */
    public RTVoiceAndroidBridge(Object appContext) {

        RTVoiceAndroidBridge.initialized = false;
        RTVoiceAndroidBridge.appContext = (Context) appContext;

        if (DEBUG)
            Log.d(TAG, "Constructor called!");

        //tts = createTTS();
    }

    //endregion

    //region Public Methods

    public static boolean isSSMLSupported() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.M;
    }
    
    /**
     * Checks if the TTS engine is currently busy by calling the boolean "working".
     *
     * Returns immediately
     *
     * @return the boolean signifying if the engine is busy or not
     */
    public static boolean isWorking() {
        return working;
    }

    /**
     * Checks if the engine has been instantiated by calling the boolean "initialized".
     *
     * Returns immediately
     *
     * @return the boolean signifying if the engine has been instantiated or not
     */
    public static boolean isInitialized() {
        return initialized;
    }

    /**
     * If the TTS engine is instantiated, shut it down and set boolean "initialized" to false.
     * Log the result.
     *
     * Logs after the TTS engine has been shut down or immediately,
     * if the TTS engine is not instantiated.
     */
    public static void Shutdown() {
        if (tts != null) {
            tts.shutdown();
            initialized = false;

            if (DEBUG) 
                Log.d(TAG, "TTS engine shutdown complete!");
        } else {
            Log.w(TAG, "tts is null!");
        }
    }

    /**
     * Starts the private task "speakNative".
     *
     * This method generates multiple logs in Log.d regarding its current state.
     *
     * @param speechText the text that is supposed to be read.
     * @param rate       the rate at which the text is supposed to be read.
     * @param pitch      the pitch that gets applied to the Locale/Voice reading the text.
     * @param inpVolume  the volume that gets applied to the Locale/Voice reading the text.
     * @param voiceName  the name of the Locale/Voice reading the text.
     */
    public static void SpeakNative(String speechText, float rate, float pitch, float inpVolume, String voiceName) {
        if (DEBUG)
            Log.d(TAG, "SpeakNative called!");

        working = true;

        if (tts != null && initialized) {
            if (DEBUG) 
                Log.d(TAG, "TTS engine initialized!");

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                Voice voiceResult = null;

                if (voiceName != null) {
                    for (Voice voice : voices) {
                        if (voice != null && voiceName.equals((voice.getName()))) {
                            voiceResult = voice;

                            break;
                        }
                    }
                }

                if (voiceResult == null)
                    voiceResult = tts.getDefaultVoice();

                if (voiceResult == null) {
                    tts.setLanguage(getLocaleFromString(voiceName));
                } else {
                    tts.setVoice(voiceResult);
                }
            } else {
                tts.setLanguage(getLocaleFromString(voiceName));
            }

            tts.setSpeechRate(rate);
            tts.setPitch(pitch);
            nativeVolume = inpVolume;

            speakNative(speechText);
        } else {
            Log.e(TAG, "TTS-system not initialized!");
        }
    }

    /**
     * Checks if the TTS engine is busy. If it's busy, stop the engine.
     *
     * This method generates a log in Log.d on call and on exit.
     */
    public static void StopNative() {
        if (DEBUG)
            Log.d(TAG, "RTVoiceAndroidBridge: StopNative called!");

        if (!(tts == null)) {
            tts.stop();

            if (DEBUG) 
                Log.d(TAG, "RTVoiceAndroidBridge: TTS engine stopped!");
        } else {
            Log.w(TAG, "Can't stop the TTS engine as there is no instance of it.");
        }
    }

    /**
     * Generates audio and starts the private task "generateAudio".
     *
     * This method generates multiple logs in Log.d regarding its current state.
     *
     * @param speechText the text that is supposed to be read.
     * @param rate       the rate at which the text is supposed to be read.
     * @param pitch      the pitch that gets applied to the Locale/Voice reading the text.
     * @param voiceName  the name of the Locale/Voice that is supposed to read the text.
     * @param outputFile the target path
     * @return String with the .wav-File path
     */
    public static String Speak(String speechText, float rate, float pitch, String voiceName, String outputFile) {
        if (DEBUG)
            Log.d(TAG, "Speak called!");

        working = true;
        String result = null;

        if (tts != null && initialized) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                Voice voiceResult = null;

                if (voiceName != null) {
                    for (Voice voice : voices) {
                        if (voice != null && voiceName.equals((voice.getName()))) {
                            voiceResult = voice;

                            break;
                        }
                    }
                }

                if (voiceResult == null)
                    voiceResult = tts.getDefaultVoice();

                if (voiceResult == null) {
                    tts.setLanguage(getLocaleFromString(voiceName));
                } else {
                    tts.setVoice(voiceResult);
                }
            } else {
                tts.setLanguage(getLocaleFromString(voiceName));
            }

            tts.setSpeechRate(rate);
            tts.setPitch(pitch);
            RTVoiceAndroidBridge.outputFile = outputFile;
            result = generateAudio(speechText);
        } else {
            Log.e(TAG, "TTS-system not initialized!");
        }

        return result;
    }

    /**
     * Checks if the TTS engine is initialized:
     * - if SDK >= Lollipop:
     * Looks for installed voices on the Android device and use their names to generate a for RTVoice readable list.
     * - if SDK < Lollipop:
     * Looks for installed locales on the Android device, check each if they have an available voice to them and use their names and languages to generate a for RTVoice readable list.
     *
     * It returns a String array when the tasks are done, not immediately.
     *
     * @return String[] with the available voices/locales
     */
    public static String[] GetVoices() {
        String[] result = null;

        if (tts != null && initialized) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                List<String> list = new ArrayList<String>();

                for (Voice voice : voices) {
                    if (voice != null) {
                        //if (voice.getName().length() >= 5) {
                        //    list.add(voice.getName() + ";" + voice.getName().substring(0, 5));
                        //} else {
                            list.add(voice.getName() + ";" + voice.getLocale().toString());
                        //}
                    }
                }
                
                result = list.toArray(new String[0]);
            } else {
                result = getVoices();
            }
        } else {
            Log.e(TAG, "TTS-system not initialized!");
        }

        return result;
    }

    /**
     * Returns the available TTS engines.
     *
     * @return String[] with the available TTS engines
     */
    public static String[] GetEngines() {
        String[] result = null;

        if (tts != null && initialized) {
            List<TextToSpeech.EngineInfo> engines = tts.getEngines();

            result = new String[engines.size()];
            int zz = 0;

            for (TextToSpeech.EngineInfo engine : engines) {
                result[zz] = engine.name + ";" + engine.label;
                zz++;
            }
        } else {
            Log.e(TAG, "TTS-system not initialized!");
        }

        return result;
    }


    /**
     * Set a specific TTS engine.
     *
     * @param engine TTS engine to be used
     * @return String[] with the available TTS engines
     */
    public static void SetupEngine(String engine) {
        tts = createTTS(engine);
    }

    //endregion

    //region Private Methods

    private static TextToSpeech createTTS(String engine) {
        return new TextToSpeech(RTVoiceAndroidBridge.appContext, new TextToSpeech.OnInitListener() {
            public void onInit(int status) {
                //DEBUG
                if (status == TextToSpeech.SUCCESS) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS successfully executed!");

                    tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
                        @Override
                        public void onStart(String s) {
                            if (DEBUG) 
                                Log.d(TAG, "TTS: Starting Utterance");
                            working = true; //reassure it's still true
                        }

                        @Override
                        public void onDone(String s) {
                            if (DEBUG) 
                                Log.d(TAG, "TTS: Utterance completed");
                            working = false;
                        }

                        @Override
                        public void onError(String s) {
                            if (DEBUG) 
                                Log.d(TAG, "TTS: A error occurred.");
                            working = false;
                        }
                    });

                    voices = tts.getVoices();
                    initialized = true;

                } else {
                    Log.e(TAG, "Error Code " + status + "");
                }
                if (status == TextToSpeech.ERROR_NETWORK) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS encountered a network problem!");
                }
                if (status == TextToSpeech.ERROR_NETWORK_TIMEOUT) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS encountered a network timeout!");
                }
                if (status == TextToSpeech.ERROR_NOT_INSTALLED_YET) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS doesn't have the requested voice data!");
                }
                if (status == TextToSpeech.ERROR_OUTPUT) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS encountered an error with the output device/file!");
                }
                if (status == TextToSpeech.ERROR_SERVICE) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS encountered a service error!");
                }
                if (status == TextToSpeech.LANG_MISSING_DATA) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS error: Language data is missing!");
                }
                if (status == TextToSpeech.LANG_NOT_SUPPORTED) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS error: Chosen language is not supported!");
                }
                if (status == TextToSpeech.ERROR_INVALID_REQUEST) {
                    if (DEBUG)
                        Log.d(TAG, "Error Code " + status + ": TTS error: Invalid request!");
                }
            }
        }, engine);
    }

    private static void fillLocales() {
        locales = new HashSet<>();
        Locale[] allLocales = Locale.getAvailableLocales();

        boolean hasVariant;
        boolean hasCountry;
        int res;
        boolean isLocaleSupported;

        for (Locale currentLocale : allLocales) {
            try {
                res = tts.isLanguageAvailable(currentLocale);
                hasVariant = (null != currentLocale.getVariant() && currentLocale.getVariant().length() > 0);
                hasCountry = (null != currentLocale.getCountry() && currentLocale.getCountry().length() > 0);

                isLocaleSupported =
                        (!hasVariant && !hasCountry && res == TextToSpeech.LANG_AVAILABLE ||
                                !hasVariant && hasCountry && res == TextToSpeech.LANG_COUNTRY_AVAILABLE ||
                                res == TextToSpeech.LANG_COUNTRY_VAR_AVAILABLE) && currentLocale.toString().length() == 5;

                if (isLocaleSupported)
                    locales.add(currentLocale);
            } catch (Exception ex) {
                Log.e(TAG, "Error checking if language is available for TTS (currentLocale=" + currentLocale + "): " + ex.getClass().getSimpleName() + "-" + ex.getMessage());
            }
        }
    }

    private static String[] getVoices() {
        if (locales == null)
            fillLocales();

        String[] result = new String[locales.size()];
        int zz = 0;
        
        for (Locale currentLocale : locales) {
            result[zz] = currentLocale.getDisplayName() + ";" + currentLocale.toString();

            zz++;
        }

        return result;
    }

    private static String generateAudio(String SpeechText) {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                new AsyncTtf().execute(SpeechText);
            } else {
                new AsyncTtfDeprecated().execute(SpeechText);
            }
        } catch (Exception ex) {
            Log.e(TAG, "Error generating audio file: " + ex.getClass().getSimpleName() + "-" + ex.getMessage());
        }

        return outputFile;
    }

    private static void speakNative(String SpeechText) {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                new AsyncTts().execute(SpeechText);
            } else {
                new AsyncTtsDeprecated().execute(SpeechText);
            }
        } catch (Exception ex) {
            Log.e(TAG, "Error speaking native: " + ex.getClass().getSimpleName() + "-" + ex.getMessage());
        }
    }

    private static Locale getLocaleFromString(String localeName) {
        if (locales == null)
            fillLocales();

        Locale result = null;
        
        for (Locale locale : locales) {
            if (locale.getDisplayName().equals((localeName))) {
                result = locale;

                break;
            }
        }

        if (result == null)
            result = Locale.getDefault();

        return result;
    }

    //endregion

    //region Private Tasks

    @SuppressWarnings("deprecation")
    private static class AsyncTtfDeprecated extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... params) {
            String text = params[0];
            HashMap<String, String> myHashRender = new HashMap<>();
            myHashRender.put(TextToSpeech.Engine.KEY_PARAM_UTTERANCE_ID, text);
            tts.synthesizeToFile(text, myHashRender, outputFile);

            working = true; //reassure it's still true

            return null;
        }

    }

    @TargetApi(Build.VERSION_CODES.LOLLIPOP)
    private static class AsyncTtf extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... params) {
            String text = params[0];
            String utteranceId = Integer.toString(this.hashCode());
            File destFile = new File(outputFile);
            tts.synthesizeToFile(text, null, destFile, utteranceId);

            working = true; //reassure it's still true

            return null;
        }
    }

    @SuppressWarnings("deprecation")
    private static class AsyncTtsDeprecated extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... params) {
            String text = params[0];
            HashMap<String, String> myHashRender = new HashMap<>();
            myHashRender.put(TextToSpeech.Engine.KEY_PARAM_UTTERANCE_ID, text);
            myHashRender.put(TextToSpeech.Engine.KEY_PARAM_VOLUME, Float.toString(nativeVolume));
            tts.speak(text, TextToSpeech.QUEUE_FLUSH, myHashRender);

            working = true; //reassure it's still true

            return null;
        }
    }

    @TargetApi(Build.VERSION_CODES.LOLLIPOP)
    private static class AsyncTts extends AsyncTask<String, Void, Void> {
        @Override
        protected Void doInBackground(String... params) {
            String text = params[0];
            String utteranceId = Integer.toString(this.hashCode());
            Bundle speakParams = new Bundle();
            speakParams.putFloat(TextToSpeech.Engine.KEY_PARAM_VOLUME, nativeVolume);
            tts.speak(text, TextToSpeech.QUEUE_FLUSH, speakParams, utteranceId);

            working = true; //reassure it's still true

            return null;
        }
    }

    //endregion

}
