using System;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Dissonance.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Dissonance
{
    /// <summary>
    /// Controls the chat log (in which the messages are shown) for the Dissonance
    /// text chat.
    ///
    /// It must be attached to a game object that has a direct child
    /// named <see cref="canvasName"/> holding the canvas where the chat
    /// is shown. That game object should also have a <see cref="ChatInputController"/>
    /// component attached to it. That game object should have a direct child
    /// named <see cref="chatBoxName"/> holding the chat log."/>
    ///
    /// </summary>
    /// <remarks>This code stems from a Dissonance demo and was then
    /// adapted to our needs.</remarks>
    public class ChatLogController : ChatController
    {
        #region fields and properties
        /// <summary>
        /// The game object representing the text chat log. It is instantiated
        /// from the prefab "LogTextPrototype".
        /// </summary>
        private GameObject textPrototype;

        /// <summary>
        /// The name of the game object representing the chat box. It must be a direct
        /// child of the <see cref="canvas"/> game object.
        /// </summary>
        private const string chatBoxName = "ChatBox";

        /// <summary>
        /// The game object named <see cref="chatBoxName"/> representing the chat box.
        /// </summary>
        private GameObject chatBox;

        /// <summary>
        /// The canvas group attached to the game object named <see cref="chatBoxName"/>.
        /// </summary>
        private CanvasGroup canvasGroup;

        /// <summary>
        /// The point on the y-axis where the chat log should stop growing.
        /// </summary>
        private float heightLimit;

        /// <summary>
        /// The queue of chat log entries. Each entry is a chat line.
        /// </summary>
        private readonly Queue<ChatLogEntry> logEntries = new();

        /// <summary>
        /// If true, the chat log will be shown for a while even if no new messages arrive.
        /// </summary>
        public bool ForceShow { get; set; }

        /// <summary>
        /// The time when the chat log should start fading out.
        /// </summary>
        private DateTime fadeOutStartTime;

        #endregion

        /// <summary>
        /// Inializes <see cref="textPrototype"/>, <see cref="canvasGroup"/>.
        /// Subscribes <see cref="OnMessageReceived"/> at <see cref="Comms.Text.MessageReceived"/>.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            const string logTextPrototypePrefab = "LogTextPrototype";
            textPrototype = Resources.Load<GameObject>(logTextPrototypePrefab);
            if (textPrototype == null)
            {
                Debug.LogError($"Could not find {logTextPrototypePrefab} in Resources.\n");
                enabled = false;
                return;
            }

            if (canvas == null)
            {
                Debug.LogError("Canvas not available.\n");
                enabled = false;
                return;
            }

            chatBox = canvas.transform.Find(chatBoxName)?.gameObject;
            if (chatBox == null)
            {
                Debug.LogError($"Could not find chat box named {chatBoxName}.\n");
                enabled = false;
                return;
            }
            canvasGroup = chatBox.GetComponent<CanvasGroup>();

            heightLimit = chatBox.GetComponent<RectTransform>().rect.height - 20;

            // Subscribe to receive Dissonance comms text messages.
            if (Comms != null)
            {
                Comms.Text.MessageReceived += OnMessageReceived;
            }
        }

        /// <summary>
        /// Prints the <paramref name="message"/> received in the chat log.
        /// This gets called by Dissonance whenever a text message arrives.
        /// </summary>
        /// <param name="message">The message received.</param>
        private void OnMessageReceived(TextMessage message)
        {
            // Ignore your own messages coming back from the server.
            if (Comms != null && message.Sender == Comms.LocalPlayerName)
            {
                return;
            }

            // Decide what we're going to print.
            string msg = string.Format("{0} ({1}): {2}",
                message.Sender[..message.Sender.Length],
                message.RecipientType == ChannelType.Room ? message.Recipient : "Whisper",
                message.Message
            );

            AddMessage(msg, new Color(0.19f, 0.19f, 0.19f));
        }

        /// <summary>
        /// Adds a message to the chat log.
        /// </summary>
        /// <param name="message">The message to be added.</param>
        /// <param name="color">The color in which the message should be printed.</param>
        public void AddMessage(string message, Color color)
        {
            EnableCanvas(true);

            // Instantiate the text prototype.
            GameObject obj = Instantiate(textPrototype, chatBox.transform);

            // Put text into the object.
            Text txt = obj.GetComponent<Text>();
            txt.text = message;
            txt.color = color;

            // Preferred height will be the height it wants to be (e.g. including extra height because of wraparound).
            // Directly set the height to that height to ensure all text is seen.
            RectTransform rectTransform = txt.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, txt.preferredHeight);
            rectTransform.anchoredPosition += new Vector2(0, 3);

            // Save the item in the queue of log entries.
            logEntries.Enqueue(new ChatLogEntry(txt));

            // Bump all items up by the appropriate amount.
            foreach (ChatLogEntry chatLogEntry in logEntries)
            {
                chatLogEntry.Transform.anchoredPosition += new Vector2(0, txt.preferredHeight);

                // If this item is over the height limit fade it out and remove it from the log when faded out.
                if (chatLogEntry.Transform.anchoredPosition.y > heightLimit && !chatLogEntry.IsTransitioningOut)
                {
                    chatLogEntry.FadeOut();
                }
            }

            // Make sure the UI is visible for some time.
            ShowFor(TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// Fades out the chat log if the time has come and removes entries that
        /// are totally faded out.
        /// </summary>
        public void Update()
        {
            if (ForceShow)
            {
                // A new cycle of showing the chat log has started.
                canvasGroup.alpha = 1;
                fadeOutStartTime = DateTime.UtcNow + TimeSpan.FromSeconds(4);
            }
            else if (fadeOutStartTime < DateTime.UtcNow)
            {
                // If now is beyond fadeOutStartTime, fade out the chat log.
                float time = (float)(DateTime.UtcNow - fadeOutStartTime).TotalSeconds / 2;
                if (time > 0)
                {
                    float alpha = (2 - Mathf.Clamp(time, 0, 2)) / 2;
                    canvasGroup.alpha = alpha;
                    // If the chat log is almost invisible, disable the canvas altogether.
                    if (alpha <= 0.05)
                    {
                        EnableCanvas(false);
                    }
                }
            }
            else if (fadeOutStartTime > DateTime.UtcNow)
            {
                canvasGroup.alpha = 1;
            }

            // Remove entries which no longer fit into the chat log and are fully faded out.
            while (logEntries.Count > 0 && logEntries.Peek().IsTransitionComplete)
            {
                Destroy(logEntries.Dequeue().Object);
            }

            // Update animations on fading chat lines.
            foreach (ChatLogEntry chatLogEntry in logEntries.TakeWhile(x => x.IsTransitioningOut))
            {
                chatLogEntry.Update();
            }
        }

        /// <summary>
        /// Shows the chat log for the given amount of <paramref name="time"/>.
        /// More technically, it sets the time when the chat log should start fading out.
        /// </summary>
        /// <param name="time">The time from now on until the chat log should fade out.</param>
        public void ShowFor(TimeSpan time)
        {
            fadeOutStartTime = DateTime.UtcNow + time;
        }

        /// <summary>
        /// Represents a chat log entry.
        /// </summary>
        /// <remarks>There are two kinds of fading: The fading of the chat log as a whole
        /// including all its entries and the fading of the top-most entries that must
        /// give way to newer entries. The fading here relates to the latter.</remarks>
        private class ChatLogEntry
        {
            /// <summary>
            /// The printed text in the chat log.
            /// </summary>
            private readonly Text txt;

            /// <summary>
            /// The <see cref="RectTransform"/> of the text in the chat log.
            /// </summary>
            public RectTransform Transform { get; private set; }

            /// <summary>
            /// The game object holding the text in the chat log.
            /// </summary>
            public GameObject Object { get => txt.gameObject; }

            /// <summary>
            /// The progress of the transition of fading out the chat log entry.
            /// It is a value between 0 and 1. 0 means the chat log entry is fully visible,
            /// and 1 means it is fully invisible.
            /// </summary>
            private float transitionProgress;

            /// <summary>
            /// True if the chat log entry is transitioning out, that is, if the
            /// chat log is full and this entry must be removed. This value is
            /// not reset to false after the transition is complete because it
            /// will be destroyed by <see cref="ChatLogController.Update"/> then.
            /// </summary>
            public bool IsTransitioningOut { get; private set; }

            /// <summary>
            /// True if the transition of fading out the chat log entry is complete.
            /// </summary>
            public bool IsTransitionComplete { get; private set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="txt">The printed text of the chat log entry.</param>
            public ChatLogEntry(Text txt)
            {
                this.txt = txt;
                Transform = txt.rectTransform;
            }

            /// <summary>
            /// Starts the transition of fading out the chat log entry.
            /// It is called when the chat log is full and this entry must be
            /// removed from the chat log.
            /// </summary>
            public void FadeOut()
            {
                IsTransitioningOut = true;
            }

            /// <summary>
            /// If the chat log entry is transitioning out, updates the transition.
            /// If the transition is complete, sets <see cref="IsTransitionComplete"/>
            /// to true.
            /// </summary>
            public void Update()
            {
                if (IsTransitioningOut)
                {
                    Color baseColor = txt.color;

                    transitionProgress = Mathf.Clamp(transitionProgress + Time.unscaledDeltaTime, 0, 1);
                    txt.color = Color.Lerp(
                        new Color(baseColor.r, baseColor.g, baseColor.b, 1),
                        new Color(baseColor.r, baseColor.g, baseColor.b, 0),
                        transitionProgress
                    );

                    if (transitionProgress >= 1)
                    {
                        IsTransitionComplete = true;
                    }
                }
            }
        }
    }
}
