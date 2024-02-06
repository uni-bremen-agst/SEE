using Cysharp.Threading.Tasks;
using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.UI.PopupMenu;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.ConsoleWindow
{
    public class ConsoleWindow : BaseWindow
    {
        public event UnityAction<string> OnInputSubmit;
        public event UnityAction<string> OnInputChanged;

        private const string windowPrefab = "Prefabs/UI/ConsoleWindow/ConsoleView";
        private const string itemPrefab = "Prefabs/UI/ConsoleWindow/ConsoleViewItem";
        /// <summary>
        /// The number of spaces to use for tabs.
        /// </summary>
        private const int tabSize = 4;
        /// <summary>
        /// The replacement for tabs in console messages.
        /// Tabs are replaced with spaces for two reasons:
        /// (1) TeshMeshPro doesn't work well with tabs. (weird spacing)
        /// (2) The search field doesn't allow tabs.
        /// </summary>
        private static readonly string tabReplacement = new(' ', tabSize);

        private List<Message> messages = new List<Message>();

        private bool messagesCleared;
        private bool messageAdded;
        private bool messageAppended;

        private Transform items;
        private TMP_InputField searchField;
        private PopupMenu.PopupMenu popupMenu;
        private ButtonManagerBasic searchOptionsButton;
        private ButtonManagerBasic filterButton;
        private ButtonManagerBasic clearButton;
        private TMP_InputField inputField;

        private bool matchCase = true;
        private bool fullMatch = false;

        private Dictionary<string, Channel> channels = new() {
            {"User Input", new Channel("User Input", '\uf007', new () {
                {"Log", new ChannelLevel("Log", Color.gray.Darker(), true)},
            })},
        };
        public string DefaultChannel = "";
        public string DefaultChannelLevel = "";

        public void AddChannel(string channel, char icon)
        {
            channels[channel] = new Channel(channel, icon);
        }

        public void AddChannelLevel(string channel, string level, Color color)
        {
            channels[channel].Levels[level] = new(level, color, true);
        }

        public void SetChannelLevelEnabled(string channel, string level, bool enabled)
        {
            if (channels.TryGetValue(channel, out Channel c)) {
                if (c.Levels.TryGetValue(level, out ChannelLevel l))
                {
                    l.enabled = enabled;
                    if (HasStarted) UpdateFilters();
                }
            }
        }

        public void AddMessage(string text, string channel = null, string level = null)
        {
            channel ??= DefaultChannel;
            level ??= DefaultChannelLevel;

            text = text.Replace("\t", tabReplacement);
            int appendTo = AppendTo(channel, level);
            if (appendTo == -1)
            {
                messages.Add(new(channel, level, text));
                messageAdded = true;
            } else
            {
                messages[appendTo].Text += text;
                messageAppended = true;
            }
        }

        private int AppendTo(string channel, string level)
        {
            for (int i=messages.Count-1; i>=0; i--)
            {
                Message message = messages[i];
                if (message.Channel == channel && message.Level == level)
                {
                    if (!message.Text.EndsWith('\n'))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public void ClearMessages()
        {
            messages.Clear();
            messagesCleared = true;
        }

        protected override void Start()
        {
            Title ??= "Console";
            base.Start();
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(windowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            foreach (Transform child in items)
            {
                Destroyer.Destroy(child.gameObject);
            }

            searchField = root.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.onValueChanged.AddListener(_ => UpdateFilters());

            popupMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();

            searchOptionsButton = root.Find("Search/SearchOptions").gameObject.MustGetComponent<ButtonManagerBasic>();
            searchOptionsButton.clickEvent.AddListener(() => ShowSearchOptionsPopup());

            filterButton = root.Find("Search/Filter").gameObject.MustGetComponent<ButtonManagerBasic>();
            filterButton.clickEvent.AddListener(() => ShowFilterPopup());

            clearButton = root.Find("Search/Clear").gameObject.MustGetComponent<ButtonManagerBasic>();
            clearButton.clickEvent.AddListener(ClearMessages);

            inputField = root.Find("InputField").gameObject.MustGetComponent<TMP_InputField>();
            inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            inputField.onValueChanged.AddListener(text => OnInputChanged?.Invoke(text));
            inputField.onSubmit.AddListener(text =>
            {
                Debug.Log($"Submit: {text}");
                AddMessage(text + "\n", "User Input", "Log");
                OnInputSubmit?.Invoke(text);
                inputField.DeactivateInputField();
                inputField.text = "";
                inputField.ActivateInputField();
            });
        }

        protected override void Update()
        {
            base.Update();
            // destroys message items after a clear
            if (messagesCleared)
            {
                messagesCleared = false;
                foreach (Transform child in items)
                {
                    Destroyer.Destroy(child.gameObject);
                }
            }
            else if (messageAdded)
            {
                messageAdded = false;
                for (int i = items.childCount; i < messages.Count; i++)
                {
                    AddItem(messages[i]);
                }
            }
            else if (messageAppended)
            {
                messageAppended = false;
                for (int i = items.childCount; i < messages.Count; i++)
                {
                    UpdateItem(i);
                }
            }
        }

        private void AddItem(Message message)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(itemPrefab, items, false);

            Channel channel = channels.ContainsKey(message.Channel) ? channels[message.Channel] : null;
            Color color = channel?.Levels[message.Level].Color ?? Color.white;
            char icon = channel?.Icon ?? '\u003f';

            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = message.Text;
            textMesh.color = color.IdealTextColor();

            TextMeshProUGUI iconMesh = item.transform.Find("Foreground/Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            iconMesh.text = icon.ToString();
            iconMesh.color = color.IdealTextColor();

            item.transform.Find("Background").GetComponent<UIGradient>().EffectGradient.SetKeys(
                new Color[] { color, color.Darker(0.3f) }.ToGradientColorKeys().ToArray(), 
                new GradientAlphaKey[] { new(1, 0), new(1, 1) });

            UpdateFilter(message, item);
        }

        private void UpdateItem(int i)
        {
            GameObject item = items.GetChild(i).gameObject;
            Message message = messages[i];
            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.SetText(message.Text);
            UpdateFilter(message, item);
        }

        private void UpdateFilters()
        {
            for (int i = 0; i < items.childCount; i++)
            {
                UpdateFilter(messages[i], items.GetChild(i).gameObject);
            }
        }

        private void UpdateFilter(Message message, GameObject item)
        {
            item.SetActive(true);

            string text = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>().text;
            if (!text.Contains(searchField.text, matchCase ? 0 : StringComparison.OrdinalIgnoreCase))
            {
                item.SetActive(false);
            }
            if (fullMatch && text.Length != searchField.text.Length)
            {
                item.SetActive(false);
            }
            if (!channels[message.Channel].Levels[message.Level].enabled)
            {
                item.SetActive(false);
            }
        }

        private void ShowSearchOptionsPopup(bool refresh = false)
        {
            popupMenu.ClearEntries();

            popupMenu.AddEntry(new PopupMenuAction("Match Case", () =>
            {
                matchCase = !matchCase;
                ShowSearchOptionsPopup(true);
                UpdateFilters();
            }, matchCase ? Icons.CheckedCheckbox : Icons.EmptyCheckbox, false));
            popupMenu.AddEntry(new PopupMenuAction("Full Match", () =>
            {
                fullMatch = !fullMatch;
                ShowSearchOptionsPopup(true);
                UpdateFilters();
            }, fullMatch ? Icons.CheckedCheckbox : Icons.EmptyCheckbox, false));

            if (!refresh)
            {
                popupMenu.MoveTo(searchOptionsButton.transform.position);
                popupMenu.ShowMenuAsync().Forget();
            }
        }

        private void ShowFilterPopup(bool refresh = false)
        {
            popupMenu.ClearEntries();

            foreach (Channel channel in channels.Values)
            {
                popupMenu.AddEntry(new PopupMenuHeading(channel.Name));
                foreach (ChannelLevel level in channel.Levels.Values)
                {
                    popupMenu.AddEntry(new PopupMenuAction(level.Name, () =>
                    {
                        level.enabled = !level.enabled;
                        UpdateFilters();
                        ShowFilterPopup(true);
                    }, level.enabled ? Icons.CheckedCheckbox : Icons.EmptyCheckbox, false));
                }
            }
            if (!refresh)
            {
                popupMenu.MoveTo(filterButton.transform.position);
                popupMenu.ShowMenuAsync().Forget();
            }
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
            public readonly string Channel;
            public readonly string Level;
            public string Text;

            public Message(string channel, string level, string text)
            {
                Channel = channel;
                Level = level;
                Text = text;
            }
        }

        private class Channel
        {
            public readonly string Name;
            public readonly char Icon;
            public readonly Dictionary<string, ChannelLevel> Levels;

            public Channel(string name, char icon, Dictionary<string, ChannelLevel> levels = null)
            {
                this.Name = name;
                this.Icon = icon;
                this.Levels = levels ?? new();
            }
        }

        private class ChannelLevel
        {
            public readonly string Name;
            public readonly Color Color;
            public bool enabled;

            public ChannelLevel(string name, Color color, bool enabled)
            {
                this.Name = name;
                this.Color = color;
                this.enabled = enabled;
            }
        }
    }
}