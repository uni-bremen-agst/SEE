using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.UI.PopupMenu;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.ConsoleWindow
{
    /// <summary>
    /// Represents a movable, scrollable console window.
    /// Displays text outputs, provides search and filter options and has an input field.
    /// <para>
    /// The console messages can be categorized by channels and levels:
    /// <list type="bullet">
    /// <item><description>Each channel has a fontawesome icon, which is displayed in front of each console message.</description></item>
    /// <item><description>Each level (in every channel) specifies the color which is used for the message backgrounds.</description></item>
    /// <item><description>The channels and levels be used to filter specific messages.</description></item>
    /// <item><description>By default only the channel `User Input` with one level `Log` exists.</description></item>
    /// </list>
    /// </para>
    /// <seealso cref="Channel"/>
    /// <seealso cref="ChannelLevel"/>
    /// </summary>
    public class ConsoleWindow : BaseWindow
    {
        /// <summary>
        /// The user submitted the input.
        /// </summary>
        public static event UnityAction<string> OnInputSubmit;

        /// <summary>
        /// The user changed the input.
        /// </summary>
        public static event UnityAction<string> OnInputChanged;

        /// <summary>
        /// The window prefab.
        /// </summary>
        private const string windowPrefab = "Prefabs/UI/ConsoleWindow/ConsoleWindow";

        /// <summary>
        /// The prefab for each message.
        /// </summary>
        private const string itemPrefab = "Prefabs/UI/ConsoleWindow/ConsoleWindowItem";

        /// <summary>
        /// The number of spaces to use for tabs.
        /// <seealso cref="tabReplacement"/>.
        /// </summary>
        private const int tabSize = 4;

        /// <summary>
        /// The replacement for tabs in console messages.
        /// Tabs are replaced with spaces for two reasons:
        /// (1) TeshMeshPro doesn't work well with tabs. (weird spacing)
        /// (2) The search field doesn't allow tabs.
        /// <seealso cref="tabSize"/>
        /// </summary>
        private static readonly string tabReplacement = new(' ', tabSize);

        /// <summary>
        /// All console messages.
        /// </summary>
        private static List<Message> messages = new();

        /// <summary>
        /// The messages got cleared.
        /// </summary>
        private static event Action MessagesCleared;

        /// <summary>
        /// A message was added.
        /// </summary>
        private static event Action MessageAdded;

        /// <summary>
        /// A message was changed.
        /// </summary>
        private static event Action MessageChanged;

        /// <summary>
        /// A channel or level was changed.
        /// </summary>
        private static event Action ChannelChanged;

        /// <summary>
        /// The messages were cleared.
        /// </summary>
        private bool messagesCleared;

        /// <summary>
        /// A message was added.
        /// </summary>
        private bool messageAdded;

        /// <summary>
        /// A message was changed.
        /// </summary>
        private bool messageChanged;

        /// <summary>
        /// A channel or level was changed.
        /// </summary>
        private bool channelChanged;

        /// <summary>
        /// Container for all messages.
        /// </summary>
        private Transform items;

        /// <summary>
        /// The search field.
        /// </summary>
        private TMP_InputField searchField;

        /// <summary>
        /// The popup menu.
        /// </summary>
        private PopupMenu.PopupMenu popupMenu;

        /// <summary>
        /// The button to open the search options.
        /// </summary>
        private ButtonManagerBasic searchOptionsButton;

        /// <summary>
        /// The button to open the filter options.
        /// </summary>
        private ButtonManagerBasic filterButton;

        /// <summary>
        /// The button to clear all messages.
        /// </summary>
        private ButtonManagerBasic clearButton;

        /// <summary>
        /// The input field.
        /// </summary>
        private TMP_InputField inputField;

        /// <summary>
        /// Whether the search is case sensitive.
        /// </summary>
        private bool matchCase = true;

        /// <summary>
        /// Whether the search must match the full message or only a part of the message.
        /// </summary>
        private bool fullMatch = false;

        /// <summary>
        /// The channels and their levels.
        /// </summary>
        private static Dictionary<string, Channel> channels = new() {
            {"User Input", new Channel("User Input", '\uf007', new () {
                {"Log", new ChannelLevel("Log", Color.gray.Darker(), true)},
            })},
        };

        /// <summary>
        /// The default channel for messages.
        /// </summary>
        public static string DefaultChannel = "";

        /// <summary>
        /// The default level for messages.
        /// </summary>
        public static string DefaultChannelLevel = "";

        /// <summary>
        /// Adds a console message.
        /// 
        /// <para>
        /// Uses <see cref="DefaultChannel"/> and <see cref="DefaultChannelLevel"/> as default if these parameters are omitted.
        /// </para>
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="level">The level.</param>
        public static void AddMessage(string text, string channel = null, string level = null)
        {
            channel ??= DefaultChannel;
            level ??= DefaultChannelLevel;

            if (!channels.TryGetValue(channel, out Channel c))
            {
                Debug.LogWarning($"Channel {channel} doesn't exist.");
            }
            else if (!c.Levels.ContainsKey(level))
            {
                Debug.LogWarning($"Level {level} doesn't exist for channel {channel}.\n\t{text}");
            }

            text = text.Replace("\t", tabReplacement);
            int? appendTo = AppendTo(channel, level);
            if (appendTo is null)
            {
                messages.Add(new(channel, level, text));
                MessageAdded?.Invoke();
            }
            else
            {
                messages[appendTo.Value].Text += text;
                MessageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Clears all messages.
        /// </summary>
        public static void ClearMessages()
        {
            messages.Clear();
            MessagesCleared?.Invoke();
        }

        /// <summary>
        /// Adds a new channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="icon">The channel icon.</param>
        public static void AddChannel(string channel, char icon)
        {
            channels[channel] = new Channel(channel, icon);
            ChannelChanged?.Invoke();
        }

        /// <summary>
        /// Adds a level to a channel.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="level">The level name.</param>
        /// <param name="color">The level color.</param>
        public static void AddChannelLevel(string channel, string level, Color color)
        {
            if (channels.TryGetValue(channel, out Channel c))
            {
                c.Levels[level] = new(level, color, true);
                ChannelChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Channel {channel} doesn't exist");
            }
        }

        /// <summary>
        /// Sets whether a level inside a channel is enabled.
        /// </summary>
        /// <param name="channel">The channel name.</param>
        /// <param name="level">The level inside the channel.</param>
        /// <param name="enabled">Whether the level is enabled.</param>
        public static void SetChannelLevelEnabled(string channel, string level, bool enabled)
        {
            if (channels.TryGetValue(channel, out Channel c))
            {
                if (c.Levels.TryGetValue(level, out ChannelLevel l))
                {
                    l.enabled = enabled;
                    ChannelChanged?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"Level {level} doesn't exist for channel {channel}.");
                }
            }
            else
            {
                Debug.LogWarning($"Channel {channel} doesn't exist.");
            }
        }


        /// <summary>
        /// Returns the message index to which a new message should be appended.
        /// -1 if a new message should be created.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="level">The level.</param>
        /// <returns>The message index where the message should be appended. (null if new message should be created)</returns>
        private static int? AppendTo(string channel, string level)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
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
            return null;
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        protected override void Start()
        {
            Title ??= "Console";
            base.Start();
            MessageAdded += OnMessageAdded;
            MessageChanged += OnMessageChanged;
            MessagesCleared += OnMessagesCleared;
            ChannelChanged += OnChannelChanged;
        }

        /// <summary>
        /// Initializes the component for the desktop platform.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(windowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            // destroys dummy messages
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
                inputField.DeactivateInputField();
                inputField.text = "";
                inputField.ActivateInputField();
                AddMessage(text + "\n", "User Input", "Log");
                OnInputSubmit?.Invoke(text);
            });
        }

        /// <summary>
        /// Updates the displayed messages.
        /// </summary>
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
                    CreateMessageItem(messages[i]);
                }
            }
            else if (messageChanged)
            {
                messageChanged = false;
                for (int i = items.childCount; i < messages.Count; i++)
                {
                    UpdateItem(i);
                }
            }
            else if (channelChanged)
            {
                channelChanged = false;
                UpdateFilters();
            }
        }

        /// <summary>
        /// Removes listeners.
        /// </summary>
        private void OnDestroy()
        {
            MessageAdded -= OnMessageAdded;
            MessageChanged -= OnMessageChanged;
            MessagesCleared -= OnMessagesCleared;
            ChannelChanged -= OnChannelChanged;
        }

        /// <summary>
        /// Listens to <see cref="MessageAdded"/>.
        /// </summary>
        private void OnMessageAdded()
        {
            messageAdded = true;
        }

        /// <summary>
        /// Listens to <see cref="MessagesCleared"/>
        /// </summary>
        private void OnMessagesCleared()
        {
            messagesCleared = true;
        }

        /// <summary>
        /// Listens to <see cref="MessageChanged"/>.
        /// </summary>
        private void OnMessageChanged()
        {
            messageChanged = true;
        }

        /// <summary>
        /// Listens to <see cref="ChannelChanged"/>.
        /// </summary>
        private void OnChannelChanged()
        {
            channelChanged = true;
        }

        /// <summary>
        /// Creates a message item.
        /// </summary>
        /// <param name="message">The message.</param>
        private void CreateMessageItem(Message message)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(itemPrefab, items, false);

            Channel channel = channels.GetValueOrDefault(message.Channel);
            Color color = channel?.Levels.GetValueOrDefault(message.Level)?.Color ?? Color.white;
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

        /// <summary>
        /// Updates a message item.
        /// </summary>
        /// <param name="i">The message index.</param>
        private void UpdateItem(int i)
        {
            GameObject item = items.GetChild(i).gameObject;
            Message message = messages[i];
            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.SetText(message.Text);
            UpdateFilter(message, item);
        }

        /// <summary>
        /// Updates the filters.
        /// </summary>
        private void UpdateFilters()
        {
            for (int i = 0; i < items.childCount; i++)
            {
                UpdateFilter(messages[i], items.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Updates the filter for a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="item">The message item.</param>
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

        /// <summary>
        /// Shows the search options.
        /// </summary>
        /// <param name="refresh"></param>
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

        /// <summary>
        /// Shows the filter options.
        /// </summary>
        /// <param name="refresh"></param>
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
            // Nothing needs to be done
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

        /// <summary>
        /// Container for a console message.
        /// </summary>
        private class Message
        {
            /// <summary>
            /// The message channel.
            /// </summary>
            public readonly string Channel;

            /// <summary>
            /// The message level.
            /// </summary>
            public readonly string Level;

            /// <summary>
            /// The text.
            /// </summary>
            public string Text;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="channel">The channel.</param>
            /// <param name="level">The level.</param>
            /// <param name="text">The text.</param>
            public Message(string channel, string level, string text)
            {
                Channel = channel;
                Level = level;
                Text = text;
            }
        }

        /// <summary>
        /// Container for a channel.
        /// </summary>
        private class Channel
        {
            /// <summary>
            /// The name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The channel icon.
            /// </summary>
            public readonly char Icon;

            /// <summary>
            /// The channel levels.
            /// </summary>
            public readonly Dictionary<string, ChannelLevel> Levels;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="icon">The icon.</param>
            /// <param name="levels">The levels.</param>
            public Channel(string name, char icon, Dictionary<string, ChannelLevel> levels = null)
            {
                this.Name = name;
                this.Icon = icon;
                this.Levels = levels ?? new();
            }
        }

        /// <summary>
        /// Container for a channel level.
        /// </summary>
        private class ChannelLevel
        {
            /// <summary>
            /// The level name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The level color.
            /// </summary>
            public readonly Color Color;

            /// <summary>
            /// Whether the channel is enabled.
            /// </summary>
            public bool enabled;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="color">The color.</param>
            /// <param name="enabled">Whether is it enabled.</param>
            public ChannelLevel(string name, Color color, bool enabled)
            {
                this.Name = name;
                this.Color = color;
                this.enabled = enabled;
            }
        }
    }
}