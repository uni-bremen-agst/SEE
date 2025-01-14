using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SEE.Game.Avatars.PersonalAssistantSpeechInput;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SEE.Game.Avatars;
using Sirenix.Utilities;

namespace Assets.SEE.Tools.Chatbot
{
    // A struct to help in creating the Json object to be sent to the rasa server
    public class PostMessageJson
    {
        public string message;
        public string sender;
    }

    internal class ChatbotDialogHandler : MonoBehaviour
    {
        private const string rasa_url = "http://localhost:5005/webhooks/rest/webhook";

        /// <summary>
        /// Sends the user text to rasa
        /// </summary>
        /// <param name="userMessage"></param>
        /// <returns></returns>
        public static IEnumerator SendMessageToRasa(string userMessage)
        {
            Debug.Log("Input text: " + userMessage);

            // Create a json object from user message
            PostMessageJson postMessage = new PostMessageJson
            {
                sender = "user",
                message = userMessage
            };

            string jsonBody = JsonUtility.ToJson(postMessage);
            Debug.Log("User json : " + jsonBody);

            // Create a post request with the data to send to Rasa server
            UnityWebRequest request = new UnityWebRequest(rasa_url, "POST");
            byte[] rawBody = new System.Text.UTF8Encoding().GetBytes(jsonBody);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(rawBody);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string jsonResponse = request.downloadHandler.text;
            UnityEngine.Debug.Log("Response: " + jsonResponse);
            // WE could use Rasa response to speak - not used
            if (!jsonResponse.IsNullOrWhitespace())
            {
                PersonalAssistantBrain.Instance.Say(ExtractTextFromJson(jsonResponse));
            }

        }
        /// <summary>
        /// Helper method to extract the incoming - not used
        /// </summary>
        /// <param name="jsonResponse"></param>
        /// <returns></returns>
        private static string ExtractTextFromJson(string jsonResponse)
        {
            StringBuilder concatenatedText = new StringBuilder();
            string[] items = jsonResponse.Split(new string[] { "},{" }, System.StringSplitOptions.None);

            foreach (var item in items)
            {
                int textIndex = item.IndexOf("\"text\":\"") + "\"text\":\"".Length;
                if (textIndex > "\"text\":\"".Length - 1)
                {
                    int endIndex = item.IndexOf("\"", textIndex);
                    string text = item.Substring(textIndex, endIndex - textIndex);
                    concatenatedText.Append(text + " ");
                }
            }

            return concatenatedText.ToString().Trim();
        }


    }
}
