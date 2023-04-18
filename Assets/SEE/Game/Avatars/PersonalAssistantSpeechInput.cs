using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using SEE.Game.UI.Notification;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A component to be attached to the personal assistant to enable speech
    /// recognition to control it.
    /// </summary>
    public class PersonalAssistantSpeechInput : MonoBehaviour
    {
        
        /// <summary>
        /// Whether to use ChatGPT to answer all queries.
        /// If this is set to true, the grammar file is ignored, and we will listen to all input.
        /// </summary>
        [Tooltip("Whether to use ChatGPT to answer all queries.")]
        public bool UseChatGPT = true;
        
        // TODO: Keyboard shortcut to disable this temporarily.

        /// <summary>
        /// The OpenAI API key to use for ChatGPT.
        /// </summary>
        public string OpenAiApiKey = "";
        
        /// <summary>
        /// Path to the SRGS grammar file. The grammar is expected to define the
        /// following semantics: help, time, about.
        /// </summary>
        [Tooltip("Path to the SRGS grammar file defining the speech input language.")]
        public FilePath GrammarFilePath;

        /// <summary>
        /// The grammar recognizer used to interpret the speech input.
        /// </summary>
        private SpeechInput input;

        /// <summary>
        /// The brain of the personal assistant.
        /// </summary>
        private PersonalAssistantBrain brain;
        
        /// <summary>
        /// The OpenAI client to use for ChatGPT.
        /// </summary>
        private OpenAIClient openAiClient;

        private const string PROMPT = "You are the digital assistant for SEE, which stands for "
            + "Software Engineering Experience. You are also named SEE yourself. "
            + "You are helpful, concise and friendly. YOU MUST NOT HALLUCINATE FEATURES OF SEE WHICH DO NOT EXIST! "
            + "\nHere is a general description of SEE:\n"
            + "SEE let's you visualize your software as code cities in 3D, using the Unity game engine. "
            + "SEE is developed by the AG Softwaretechnik at the University of Bremen, led by Rainer Koschke. "
            + "Many students have worked on SEE as part of a bachelor or master thesis, "
            + "or as part of a bachelor project. "
            + "SEE runs on traditional desktop platforms as well as on Virtual Reality headsets.\n\n"
            + "The hierarchical decomposition of a program forms a tree. "
            + "The leaves of this tree are visualized as blocks where "
            + "different metrics can be used to determine the width, height, "
            + "depth, and color of the blocks. "
            + "Inner nodes of this tree can be visualized as nested circles or rectangles "
            + "depending on the layout you choose. "
            + "Dependencies can be depicted by connecting edges between blocks. \n\n"
            + "Knowledge Cutoff: 2021. It is now 2023. Today is the Bachelor Project Day at the University of Bremen, "
            + "where SEE is presented to other students. ";

        /// <summary>
        /// Sets up the grammar <see cref="input"/> and registers the callback
        /// <see cref="OnPhraseRecognized(PhraseRecognizedEventArgs)"/>.
        /// </summary>
        private void Start()
        {
            if (!UseChatGPT)
            {
                if (!InitializeGrammarInput())
                {
                    enabled = false;
                    return;
                }
            }
            else
            {
                if (!InitializeDictationInput())
                {
                    enabled = false;
                    return;
                }
            }
            
            input.Start();
            if (!gameObject.TryGetComponentOrLog(out brain))
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Initializes <see cref="input"/> with the grammar file specified in <see cref="GrammarFilePath"/>.
        /// We will only react to certain keyword phrases.
        /// </summary>
        /// <returns>Whether the initialization was successful.</returns>
        private bool InitializeGrammarInput()
        {
            if (string.IsNullOrEmpty(GrammarFilePath.Path))
            {
                Debug.LogError("Grammar file for speech recognition is not defined.\n");
                return false;
            }

            if (!File.Exists(GrammarFilePath.Path))
            {
                Debug.LogError($"Grammar file {GrammarFilePath} for speech recognition does not exist.\n");
                return false;
            }
            
            GrammarInput grammarInput;
            input = grammarInput = new GrammarInput(GrammarFilePath.Path);
            grammarInput.Register(OnPhraseRecognized);
            return true;
        }

        /// <summary>
        /// Initializes <see cref="input"/> with the ChatGPT API.
        /// </summary>
        /// <returns>Whether the initialization was successful.</returns>
        private bool InitializeDictationInput()
        {
            if (string.IsNullOrEmpty(OpenAiApiKey))
            {
                Debug.LogError("OpenAI API key is not defined.\n");
                return false;
            }
            
            openAiClient = new OpenAIClient(OpenAiApiKey);
            if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
            {
                // Disable phrase recognition system first if it is running.
                PhraseRecognitionSystem.Shutdown();
            }
            DictationInput dictationInput;
            input = dictationInput = new DictationInput();
            dictationInput.Register(OnDictationResult);
            SayHello().Forget();
            return true;
        }

        private async UniTaskVoid SayHello()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));
            OnDictationResult("Hi SEE! How are you today?", ConfidenceLevel.High);
        }

        /// <summary>
        /// Callback that is called by the <see cref="input"/> recognizer when a
        /// sentence was recognized.
        /// </summary>
        /// <param name="args">details about the recognized sentence</param>
        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            // Debug.Log($"Detected phrase '{args.text}' with confidence {args.confidence}\n");
            SemanticMeaning[] meanings = args.semanticMeanings;
            if (meanings != null)
            {
                foreach (SemanticMeaning meaning in meanings)
                {
                    // Debug.Log($"Meaning: {meaning.key} => {ToString(meaning.values)}\n");
                    foreach (string value in meaning.values)
                    {
                        switch (value)
                        {
                            // data, interact, time, about, goodBye
                            case "data": brain.Overview();
                                break;
                            case "interact": brain.Interaction();
                                break;
                            case "time": brain.CurrentTime();
                                break;
                            case "about": brain.About();
                                break;
                            case "goodBye": brain.GoodBye();
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback that is called by the <see cref="input"/> recognizer when a
        /// free-form sentence was recognized.
        /// This will make an API request to the ChatGPT API.
        /// </summary>
        /// <param name="text">The recognized text.</param>
        /// <param name="confidence">The confidence level of the recognition.</param>
        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log($"Detected phrase '{text}' with confidence {confidence}\n");
            if (confidence != ConfidenceLevel.Rejected)
            {
                // TODO: Remember and include conversation history here.
                List<Message> chatPrompts = new()
                {
                    new Message(Role.System, PROMPT),
                    new Message(Role.User, text)
                };

                Notification notification = ShowNotification.Info("Thinking...", 
                                                                  "Please wait while I think about what you said...");
                SendChatMessage(new ChatRequest(chatPrompts, "gpt-3.5-turbo"), notification).Forget();
            }
            
            async UniTaskVoid SendChatMessage(ChatRequest request, Notification notification)
            {
                ChatResponse result = await openAiClient.ChatEndpoint.GetCompletionAsync(request);
                notification.Close();
                brain.Say(result.FirstChoice.Message.Content);
            }
        }

        /// <summary>
        /// Shuts down <see cref="input"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        private void OnApplicationQuit()
        {
            OnDisable();
            input?.Dispose();
        }

        /// <summary>
        /// Re-starts <see cref="input"/>.
        /// </summary>
        private void OnEnable()
        {
            if (input != null)
            {
                input.Start();
                if (UseChatGPT && input is DictationInput dictationInput)
                {
                    dictationInput.Register(OnDictationResult);
                }
                else if (input is GrammarInput grammarInput)
                {
                    grammarInput.Register(OnPhraseRecognized);
                }
            }
        }

        /// <summary>
        /// Stops <see cref="input"/>.
        /// </summary>
        private void OnDisable()
        {
            if (input != null)
            {
                input.Stop();
                if (UseChatGPT && input is DictationInput dictationInput)
                {
                    dictationInput.Unregister(OnDictationResult);
                }
                else if (input is GrammarInput grammarInput)
                {
                    grammarInput.Unregister(OnPhraseRecognized);
                }
            }
        }
    }
}
