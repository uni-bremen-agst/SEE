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

namespace SEE.UI.Window.ConsoleWindow
{
    public class ConsoleWindow : BaseWindow
    {
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

        private bool matchCase = true;
        private bool fullMatch = false;
        private Dictionary<MessageSource, Dictionary<MessageLevel, bool>> showSourceWithLevel = new() {
            {MessageSource.Adapter, new() {
                {MessageLevel.Log, false },
                {MessageLevel.Warning, true },
                {MessageLevel.Error, true },
            }},
            {MessageSource.Debugee, new() {
                {MessageLevel.Log, true },
                {MessageLevel.Warning, true },
                {MessageLevel.Error, true },
            }}
        };

        public void AddMessage(string text, MessageSource source = MessageSource.Adapter, MessageLevel level = MessageLevel.Log)
        {
            text = text.Replace("\t", tabReplacement);
            int appendTo = AppendTo(source, level);
            if (appendTo == -1)
            {
                messages.Add(new(text, source, level));
                messageAdded = true;
            } else
            {
                messages[appendTo].Text += text;
                messageAppended = true;
            }
        }

        private int AppendTo(MessageSource source, MessageLevel level)
        {
            for (int i=messages.Count-1; i>=0; i--)
            {
                Message message = messages[i];
                if (message.Source == source && message.Level == level)
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
            Title = "Console";
            base.Start();
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(windowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");

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

            OnComponentInitialized += () =>
            {
                foreach (Transform child in items)
                {
                    Destroyer.Destroy(child.gameObject);
                }
            };
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

            var color = message.Level switch
            {
                MessageLevel.Error => Color.red.Lighter(),
                MessageLevel.Warning => Color.yellow.Lighter(),
                MessageLevel.Log => Color.gray.Lighter(),
                _ => Color.white
            };

            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = message.Text;
            textMesh.color = color.IdealTextColor();

            TextMeshProUGUI iconMesh = item.transform.Find("Foreground/Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            iconMesh.text = message.Source switch
            {
                MessageSource.Adapter => "\uf188",
                MessageSource.Debugee => "\uf135",
                _ => ""
            };
            iconMesh.color = color.IdealTextColor().Lighter();

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
            if (!showSourceWithLevel[message.Source][message.Level])
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

            foreach (var source in showSourceWithLevel)
            {
                popupMenu.AddEntry(new PopupMenuHeading(source.Key.ToString()));
                foreach (var level in source.Value)
                {
                    popupMenu.AddEntry(new PopupMenuAction(level.Key.ToString(), () =>
                    {
                        showSourceWithLevel[source.Key][level.Key] = !level.Value;
                        UpdateFilters();
                        ShowFilterPopup(true);
                    }, level.Value ? Icons.CheckedCheckbox : Icons.EmptyCheckbox, false));
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
            public string Text;
            public MessageSource Source;
            public MessageLevel Level;
            public Message(string text, MessageSource source, MessageLevel level)
            {
                this.Text = text;
                this.Source = source;
                this.Level = level;
            }


        }
        public enum MessageLevel
        {
            Log = 0,
            Warning = 1,
            Error = 2,
        }

        public enum MessageSource
        {
            Adapter,
            Debugee
        }


    }
}