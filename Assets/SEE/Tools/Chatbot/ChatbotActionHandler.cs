using UnityEngine;
using System.Net;
using System.Threading;
using System.Text;
using SEE.Controls;
using SEE.Controls.KeyActions;
using SEE.Controls.VoiceActions;
using SEE.Game.Avatars;
using static SEE.Game.Avatars.PersonalAssistantSpeechInput;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using SEE.Controls.Actions;
using SEE.UI.StateIndicator;
using SEE.GO.Menu;
using SEE.Game.City;
using SEE.Game;
using SEE.GO;
using System;
using SEE.DataModel.DG;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Assets.SEE.Tools.Knowledgebase;
using System.Threading.Tasks;
using Sirenix.Serialization;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using System.Linq;
using Utilities.WebRequestRest;

namespace Assets.SEE.Tools.Chatbot
{

    public class ChatbotActionHandler : MonoBehaviour
    {
        private HttpListener _httpListener;
        private Thread _listenerThread;
        private bool _isRunning = false;
        private bool changeMode = false;

        /// <summary>
        /// The brain of the personal assistant.
        /// </summary>
        private PersonalAssistantBrain brain;

        void Start()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:5001/");
            _httpListener.Start();
            _isRunning = true;

            _listenerThread = new Thread(HandleActionRequests);
            _listenerThread.Start();
            Debug.Log("HTTP Server started.");
        }

        void Update()
        {
        }

        /// <summary>
        /// Terminates the application (exits the game).
        /// </summary>
        private static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        /// <summary>
        /// Waits for shutting down SEE, so the assistent has time to say goodbye
        /// </summary>
        /// <returns></returns>
        IEnumerator ExitAfterSpeech()
        {
            PersonalAssistantBrain.Instance.Say("Good bye!");
            yield return new WaitForSeconds(3f);
            ExitGame();
        }

        void OnApplicationQuit()
        {
            StopServer();
        }

        private void StopServer()
        {
            _isRunning = false;
            if (_httpListener != null)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _listenerThread.Abort();
            }
            Debug.Log("HTTP Server stopped.");
        }

        /// <summary>
        /// This Methods handles incoming triggers from Rasa
        /// </summary>
        private async void HandleActionRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    var request = context.Request;
                    var response = context.Response;
                    string responseString = "";

                    string requestKey = request.HttpMethod + request.Url.AbsolutePath;

                    switch (requestKey)
                    {
                        case "POST/run-method":
                            string requestBody;
                            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                requestBody = reader.ReadToEnd();
                            }

                            // Parse JSON into a JObject
                            JObject json = JObject.Parse(requestBody);

                            // Access the properties dynamically
                            string intent = json["intent"]?.ToString();
                            string subject = json["entities"]?["subject"]?.ToString();
                            string place = json["entities"]?["place"]?.ToString();
                            string metric = json["entities"]?["metric"]?.ToString();
                            string operation = json["entities"]?["operation"]?.ToString();

                            // Process request
                            Debug.Log("Received request: " + requestBody);

                            KnowledgebaseQueryHandler knowledgebaseQueryHandler = new KnowledgebaseQueryHandler();
                            string query = await knowledgebaseQueryHandler.CreateQueryAsync(json);
                            if (query != null)
                            {
                                Debug.Log("Query ist nicht null");
                                List<Knowledgebase.QueryInfo> nodeInfoList = await knowledgebaseQueryHandler.RunQueryAsync(json["intent"].ToString(), query);
                                Debug.Log("Log nodeinfo count: " + nodeInfoList.Count);
                                if (nodeInfoList.Count < 1)
                                {
                                    // Handle the case where no records are returned
                                    Debug.LogError("No records");

                                    UnityDispatcher.Enqueue(() =>
                                    {
                                        PersonalAssistantBrain.Instance.Say("I couldn't find any entries for your query.");
                                    });
                                }
                                else
                                {
                                    string[] nodeIds = new string[nodeInfoList.Count];
                                    Debug.Log(nodeInfoList[0]);
                                    if (intent == "QueryMetricInSubject")
                                    {
                                        responseString = $"The {subject} {nodeInfoList[0].Sourcename} has the {operation} {metric} with a value of {nodeInfoList[0].Metric}.";
                                        nodeIds[0] = nodeInfoList[0].Id;
                                        highLightNodes(nodeIds);

                                        UnityDispatcher.Enqueue(() =>
                                        {
                                            PersonalAssistantBrain.Instance.Say(responseString);
                                        });
                                    }
                                    if (intent == "QueryMetricInPlace")
                                    {
                                        responseString = $"{nodeInfoList[0].Sourcename} has a {metric} of {nodeInfoList[0].Metric}.";
                                        nodeIds[0] = nodeInfoList[0].Id;
                                        highLightNodes(nodeIds);
                                    }
                                    UnityDispatcher.Enqueue(() =>
                                    {
                                        PersonalAssistantBrain.Instance.Say(responseString);
                                    });
                                }
                            }


                            ;
                            response.StatusCode = 200;
                            response.Close();
                            break;
                        case "POST/Help":
                            responseString = "Here is your help menu";
                            UnityDispatcher.Enqueue(() =>
                            {
                                PersonalAssistantBrain.Instance.Say(responseString);
                            });

                            response.StatusCode = 200;
                            response.Close();
                            VoiceBindings.SetVoiceActionState(VoiceAction.Help, true);
                            break;

                        case "POST/toggleMenu":
                            response.StatusCode = 200;
                            response.Close();
                            VoiceBindings.SetVoiceActionState(VoiceAction.ToggleMenu, true);
                            break;

                        case "POST/toggleSettings":
                            response.StatusCode = 200;
                            response.Close();
                            VoiceBindings.SetVoiceActionState(VoiceAction.ToggleSettings, true);
                            break;

                        case "POST/toggleMirror":
                            {
                                response.StatusCode = 200;
                                response.Close();
                                VoiceBindings.SetVoiceActionState(VoiceAction.ToggleMirror, true);
                                break;
                            }
                        case "POST/toggleBrowser":
                            response.StatusCode = 200;
                            response.Close();
                            UnityDispatcher.Enqueue(() =>
                            {
                                PersonalAssistantBrain.Instance.Say("I toggled the browser for you.");
                            });
                            VoiceBindings.SetVoiceActionState(VoiceAction.ToggleBrowser, true);
                            break;

                        case "POST/Undo":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.Undo, true);
                                break;
                            }
                        case "POST/Redo":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.Redo, true);
                                break;
                            }
                        case "POST/ConfigMenu":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.ConfigMenu, true);
                                break;
                            }

                        case "POST/ToggleEdges":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.ToggleEdges, true);
                                break;
                            }

                        case "POST/CancelAction":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.Cancel, true);
                                break;
                            }

                        case "POST/Pointing":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.Pointing, true);
                                break;
                            }
                        case "POST/MoveAction":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent

                                //changeMode = true;
                                UnityDispatcher.Enqueue(() => {
                                    PlayerMenu playerMenu = FindObjectOfType<PlayerMenu>();
                                    ActionStateIndicator indicator = playerMenu.indicator;

                                    ActionStateType action = ActionStateType.GetActionStateTypeByName("Move");
                                    GlobalActionHistory.Execute(action);
                                    indicator.ChangeState(action.Name, action.Color);
                                    PlayerMenu.SetPlayerMenu(action.Name); // FIXME: not working right now
                                } );

                                break;
                            }

                        // ADD MORE ACTIONS HERE......
                        //
                        //
                        // ........................

                        case "POST/DrawShapeAction":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent

                                //changeMode = true;
                                UnityDispatcher.Enqueue(() => {
                                    PlayerMenu playerMenu = FindObjectOfType<PlayerMenu>();
                                    ActionStateIndicator indicator = playerMenu.indicator;

                                    ActionStateType action = ActionStateType.GetActionStateTypeByName("Draw Shape");
                                    GlobalActionHistory.Execute(action);
                                    indicator.ChangeState(action.Name, action.Color);
                                    PlayerMenu.SetPlayerMenu(action.Name); // FIXME: not working right now
                                });

                                break;
                            }

                        case "POST/toggleContextMenu":
                            {
                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                VoiceBindings.SetVoiceActionState(VoiceAction.OpenContextMenu, true);
                                break;
                            }
                        case "POST/CountIn":
                            {
                                string requestBodyForShowCity;
                                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                                {
                                    requestBodyForShowCity = reader.ReadToEnd();
                                }

                                // Parse JSON into a JObject
                                JObject jsonForShowCity = JObject.Parse(requestBodyForShowCity);


                                string searchQuery = $"MATCH(n{{`Source.Name`:'{jsonForShowCity["entities"]?["place"]}'}}) " +
                                                    "MATCH(n) - [:HIERARCHY_PARENT_OF *]->(c: Class)" +
                                                     "RETURN COUNT(c) AS totalClasses";

                                Debug.Log(searchQuery);
                                KnowledgebaseQueryHandler knowledgebaseQueryHandlerShow = new KnowledgebaseQueryHandler();
                                List<Knowledgebase.QueryInfo> queryResult = await knowledgebaseQueryHandlerShow.RunQueryAsync("countIn", searchQuery);


                                responseString = $"There are {queryResult[0].NumberOfClasses} classes in this package";

                                UnityDispatcher.Enqueue(() =>
                                {
                                    PersonalAssistantBrain.Instance.Say(responseString);
                                });

                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                break;
                            }

                        case "POST/ShowInCity":
                            {
                                string requestBodyForShowCity;
                                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                                {
                                    requestBodyForShowCity = reader.ReadToEnd();
                                }

                                // Parse JSON into a JObject
                                JObject jsonForShowCity = JObject.Parse(requestBodyForShowCity);

                                string searchQuery = $"MATCH(n{{`Source.Name`:'{jsonForShowCity["entities"]?["place"]}'}}) " +
                                                    "RETURN n.`Source.Name` AS Source_Name, n.id AS id";

                                Debug.Log(searchQuery);
                                KnowledgebaseQueryHandler knowledgebaseQueryHandlerShow = new KnowledgebaseQueryHandler();
                                List<Knowledgebase.QueryInfo> queryResult = await knowledgebaseQueryHandlerShow.RunQueryAsync("findPlace", searchQuery);

                                Debug.Log(queryResult[0]);

                                responseString = $"Highlighted";
                                string[] nodeIdToHighlight = new string[queryResult.Count];
                                nodeIdToHighlight[0] = queryResult[0].Id;
                                highLightNodes(nodeIdToHighlight);


                                response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent
                                break;
                            }

                        case "POST/quit":
                            // Respond to the quit request
                            responseString = "Shutting down the server";
                            Debug.Log("Server will stop");
                            //StopServer();
                            UnityDispatcher.Enqueue(() =>
                            {
                                StartCoroutine(ExitAfterSpeech());
                            });


                            break;

                        case "POST/Test":
                            {

                             // This can be used for testing purpose
                            response.StatusCode = 200;
                                response.Close();  // Close the response to ensure it is sent

                                break;
                            }
                        default:
                            // Handle unsupported routes or methods
                            responseString = "Sorry i dont get it.";
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            response.Close();
                            break;
                    }

                }
                catch (HttpListenerException ex)
                {
                    if (_isRunning)
                    {
                        Debug.LogError("HTTP Listener Exception: " + ex.Message);
                    }
                }

            }
        }

        private void highLightNodes(string[] nodeIDs)
        {
            Debug.LogError("Test erfolgreich!");

            UnityDispatcher.Enqueue(() =>
            {
                GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
                // We will search in each code city.
                foreach (GameObject cityObject in cities)
                    if (cityObject.TryGetComponentOrLog(out AbstractSEECity city))
                    {
                        // but only search in Graph/Tables that are loaded
                        if (city.LoadedGraph == null)
                        {
                            continue;
                        }
                        else
                        {
                            for (int i = 0; i < nodeIDs.Length; i++)
                            {
                                Node foundNode = city.LoadedGraph.GetNode(nodeIDs[i]);
                                GameObject nodeGameObject = GraphElementIDMap.Find(foundNode.ID, mustFindElement: true);
                                nodeGameObject.Operator().Highlight(duration: 10);
                            }
                        }
                    }

                PersonalAssistantBrain.Instance.Say("The node is highlighted in the City. Its blinking for 10 seconds.");

            });
        }
    }
}
