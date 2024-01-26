using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.UI.Window.ConsoleWindow
{
    public class ConsoleWindow : BaseWindow
    {
        private const string windowPrefab = "Prefabs/UI/ConsoleWindow/ConsoleView";
        private const string itemPrefab = "Prefabs/UI/ConsoleWindow/ConsoleViewItem";

        private List<Message> messages = new List<Message>();
        private bool messagesCleared;
        private bool messageAdded;

        private Transform items;

        public void AddMessage(string text, string source = "\uf188", MessageLevel level = MessageLevel.Log)
        {
            messages.Add(new(text, source, level));
            messageAdded = true;
        }

        public void ClearMessages()
        {
            messages.Clear();
            messagesCleared = true;
        }

        protected override void Start()
        {
            Title = "Console";
            base.Start();
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(windowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            messagesCleared = true;
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            if (messagesCleared)
            {
                messagesCleared = false;
                foreach (Transform child in items)
                {
                    Destroyer.Destroy(child.gameObject);
                }
                return;
            }
            if (messageAdded)
            {
                messageAdded = false;
                for (int i=items.childCount; i<messages.Count; i++)
                {
                    AddItem(messages[i]);
                }
            }
        }

        private void AddItem(Message message)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(itemPrefab, items, false);

            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = message.Text;
            TextMeshProUGUI iconMesh = item.transform.Find("Foreground/Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            iconMesh.text = message.Icon;

        }

        public override void RebuildLayout()
        {
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new System.NotImplementedException();
        }

        private class Message
        {
            public string Text;
            public string Icon;
            public MessageLevel Level;
            public Message(string Text, string Icon, MessageLevel Level)
            {
                this.Text = Text;
                this.Icon = Icon;
                this.Level = Level;
            }
        }
        public enum MessageLevel
        {
            Log = 0,
            Warning = 1,
            Error = 2,
        }
    }
}