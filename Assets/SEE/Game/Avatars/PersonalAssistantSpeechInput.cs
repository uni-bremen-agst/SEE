using SEE.Controls;
using SEE.GO;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using SEE.UI;
using SEE.UI.Notification;
using UnityEngine;
using UnityEngine.Windows.Speech;
using Sirenix.OdinInspector;

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

        /// <summary>
        /// The OpenAI API key to use for ChatGPT.
        /// </summary>
        [ShowIf("UseChatGPT"), Tooltip("The OpenAI API key to use for ChatGPT.")]
        public string OpenAiApiKey = "";

        /// <summary>
        /// Path to the SRGS grammar file. The grammar is expected to define the
        /// following semantics: help, time, about.
        /// </summary>
        [HideIf("UseChatGPT"), ShowInInspector, Tooltip("Path to the SRGS grammar file defining the speech input language."), HideReferenceObjectPicker]
        public DataPath GrammarFilePath;

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

        /// <summary>
        /// Whether the personal assistant is currently listening for speech input.
        /// </summary>
        private bool currentlyListening = true;

        /// <summary>
        /// The history of the ChatGPT conversation.
        /// At the start, this only consists of the prompt.
        /// </summary>
        private readonly IList<Message> chatGptHistory = new List<Message>
        {
            new(Role.System, string.Format(prompt, DateTime.Now.Year))
        };

        /// <summary>
        /// The prompt to use for ChatGPT.
        /// Note that the string should be formatted with the current year.
        /// </summary>
        private const string prompt = "You are the digital assistant for SEE, which stands for "
            + "Software Engineering Experience. You are also named SEE yourself. "
            + "You are helpful, concise and friendly. You will not hallucinate features that don't exist.\n"
            + "SEE let's you visualize your software as code cities in 3D, using the Unity game engine. "
            + "SEE is developed by the AG Softwaretechnik at the University of Bremen, led by Rainer Koschke.\n\n"
            + "The hierarchical decomposition of a program forms a tree. "
            + "The leaves of this tree are visualized as blocks where "
            + "different metrics can be used to determine the width, height, "
            + "depth, and color of the blocks. "
            + "Inner nodes of this tree can be visualized as nested circles or rectangles "
            + "depending on the layout you choose. "
            + "Dependencies can be depicted by connecting edges between blocks. \n\n"
            + "Knowledge Cutoff: 2021. Current year: {0}\n";

        /// <summary>
        /// Sets up the grammar <see cref="input"/> and registers the callback
        /// <see cref="OnPhraseRecognized(PhraseRecognizedEventArgs)"/>.
        /// </summary>
        private void Start()
        {
            // TODO: Rather than this boolean, SEE should react to the "Ok, SEE" keyword and then listen for
            //       ChatGPT input next.
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
                Debug.LogError($"Grammar file {GrammarFilePath.Path} for speech recognition does not exist.\n");
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
                Debug.LogWarning("OpenAI API key is not defined.\n");
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
            return true;
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
                            // removed at this moment so there is no confusion with the conversational interface
                            // data, interact, time, about, goodBye
                            /*
                            case "data":
                                brain.Overview();
                                break;
                            case "interact":
                                brain.Interaction();
                                break;
                            case "time":
                                brain.CurrentTime();
                                break;
                            case "about":
                                brain.About();
                                break;
                            case "goodBye":
                                brain.GoodBye();
                                break;
                            case "project":
                                brain.Project();
                                break;*/
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
                chatGptHistory.Add(new Message(Role.User, text));
                SendChatMessage(new ChatRequest(chatGptHistory, "gpt-3.5-turbo")).Forget();
            }
            return;

            async UniTaskVoid SendChatMessage(ChatRequest request)
            {
                string message;
                using (LoadingSpinner.ShowIndeterminate("ChatGPT is thinking about what you said..."))
                {
                    ChatResponse result = await openAiClient.ChatEndpoint.GetCompletionAsync(request);
                    message = result.FirstChoice.Message.Content.ToString();
                    chatGptHistory.Add(new Message(Role.Assistant, message));
                }
                // We need to stop listening before we start speaking, else we will hear our own voice.
                StopListening();
                brain.Say(message, StartListening);
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
        /// Stops listening to the user.
        /// </summary>
        private void StopListening()
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
                currentlyListening = false;
            }
        }

        /// <summary>
        /// Starts listening to the user.
        /// </summary>
        private void StartListening()
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
                currentlyListening = true;
            }
        }

        /// <summary>
        /// Re-starts <see cref="input"/>.
        /// </summary>
        private void OnEnable()
        {
            StartListening();
        }

        /// <summary>
        /// Stops <see cref="input"/>.
        /// </summary>
        private void OnDisable()
        {
            StopListening();
        }

        private void Update()
        {
            // Change so there is no confusion with the conversational interface
            /*
            if (SEEInput.ToggleVoiceControl() && input != null)
            {
                if (currentlyListening)
                {
                    ShowNotification.Info("Stopped listening", "Disabled voice input.", 5f);
                    StopListening();
                }
                else
                {
                    ShowNotification.Info("Started listening", "Enabled voice input.", 5f);
                    StartListening();
                }
            }*/
        }
    }
}
