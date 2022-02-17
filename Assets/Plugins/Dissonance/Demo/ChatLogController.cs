using System;
using System.Collections.Generic;
using Dissonance.Networking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Dissonance.Demo
{
    public class ChatLogController
        : MonoBehaviour
    {
        #region fields and properties
        public DissonanceComms Comms;

        private GameObject _textPrototype;
        private CanvasGroup _canvas;

        private float _heightLimit;
        private readonly Queue<ChatLogEntry> _entries = new Queue<ChatLogEntry>();

        public bool ForceShow { get; set; }
        private DateTime _fadeOutStartTime;
        #endregion

        public void Start ()
        {
            Comms = Comms ?? FindObjectOfType<DissonanceComms>();

            _textPrototype = Resources.Load<GameObject>("LogTextPrototype");
            _canvas = GetComponent<CanvasGroup>();

            _heightLimit = gameObject.GetComponent<RectTransform>().rect.height - 20;

            //Subscribe to receive Dissonance comms text messages
            if (Comms != null)
                Comms.Text.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        /// This gets called by Dissonance whenever a text message arrives
        /// </summary>
        /// <param name="message"></param>
        private void OnMessageReceived(TextMessage message)
        {
            //Ignore your own messages coming back from the server
            if (Comms != null && message.Sender == Comms.LocalPlayerName)
                return;

            //Decide what we're going to print
            var msg = string.Format("{0} ({1}): {2}",
                message.Sender.Substring(0, Math.Min(8, message.Sender.Length)),
                message.RecipientType == ChannelType.Room ? message.Recipient : "Whisper",
                message.Message
            );

            AddMessage(msg, new Color(0.19f, 0.19f, 0.19f));
        }

        public void AddMessage(string message, Color color)
        {
            //Instantiate the text prototype
            //This cast is required on Unity 5.4! There it return an object
            var obj = (GameObject)Instantiate(_textPrototype, gameObject.transform);

            //Put text into the object
            var txt = obj.GetComponent<Text>();
            txt.text = message;
            txt.color = color;

            //Preferred height will be the height it wants to be (e.g. including extra height because of wraparound)
            //Directly set the height to that height to ensure all text is seen
            txt.GetComponent<RectTransform>().sizeDelta = new Vector2(0, txt.preferredHeight);
            txt.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 3);

            //Save the item in the queue of log entries
            _entries.Enqueue(new ChatLogEntry(txt));

            //Bump all items up by the appropriate amount
            foreach (var chatLogEntry in _entries)
            {
                chatLogEntry.Transform.anchoredPosition += new Vector2(0, txt.preferredHeight);

                //If this item is over the height limit fade it out
                if (chatLogEntry.Transform.anchoredPosition.y > _heightLimit && !chatLogEntry.IsTransitioningOut)
                    chatLogEntry.FadeOut();
            }

            //Make sure the UI is visible for some time
            ShowFor(TimeSpan.FromSeconds(3));
        }

        public void Update()
        {
            //Fade the entire chat UI if not force show
            if (ForceShow)
            {
                _canvas.alpha = 1;
                _fadeOutStartTime = DateTime.UtcNow + TimeSpan.FromSeconds(4);
            }
            else if (_fadeOutStartTime < DateTime.UtcNow)
            {
                var time = (float)(DateTime.UtcNow - _fadeOutStartTime).TotalSeconds / 2;
                if (time > 0)
                {
                    var alpha = (2 - Mathf.Clamp(time, 0, 2)) / 2;
                    _canvas.alpha = alpha;
                }
            }
            else if (_fadeOutStartTime > DateTime.UtcNow)
            {
                _canvas.alpha = 1;
            }

            //Remove entries which are totally faded out
            while (_entries.Count > 0 && _entries.Peek().IsTransitionComplete)
            {
                Destroy(_entries.Dequeue().Object);
            }

            //Update animations on fading chat lines
            foreach (var chatLogEntry in _entries)
            {
                if (chatLogEntry.IsTransitioningOut)
                    chatLogEntry.Update();
                else
                    break;
            }
        }

        public void ShowFor(TimeSpan time)
        {
            _fadeOutStartTime = DateTime.UtcNow + time;
        }

        private class ChatLogEntry
        {
            private readonly Text _txt;

            private readonly RectTransform _transform;
            [NotNull] public RectTransform Transform { get { return _transform; } }

            [NotNull] public GameObject Object { get { return _txt.gameObject; } }

            private float _transitionProgress;
            public bool IsTransitioningOut { get; private set; }
            public bool IsTransitionComplete { get; private set; }

            public ChatLogEntry([NotNull] Text txt)
            {
                _txt = txt;
                _transform = txt.rectTransform;
            }

            public void FadeOut()
            {
                IsTransitioningOut = true;
            }

            public void Update()
            {
                if (IsTransitioningOut)
                {
                    var baseColor = _txt.color;

                    _transitionProgress = Mathf.Clamp(_transitionProgress + Time.unscaledDeltaTime, 0, 1);
                    _txt.color = Color.Lerp(
                        new Color(baseColor.r, baseColor.g, baseColor.b, 1),
                        new Color(baseColor.r, baseColor.g, baseColor.b, 0),
                        _transitionProgress
                    );

                    if (_transitionProgress >= 1)
                        IsTransitionComplete = true;
                }
            }
        }
    }
}
