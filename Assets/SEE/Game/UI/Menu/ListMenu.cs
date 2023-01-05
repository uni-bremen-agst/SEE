using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
using UnityEngine.Windows.Speech;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Represents a menu of various actions the user can choose from.
    /// The Menu consists of multiple MenuEntries of the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    /// <seealso cref="MenuEntry"/>
    /// <seealso cref="SimpleMenu"/>
    public abstract class ListMenu<T> : PlatformDependentComponent where T : MenuEntry
    {
        /// <summary>
        /// The title of this menu.
        /// </summary>
        private string title;
        /// <summary>
        /// The title of this menu.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnTitleChanged?.Invoke();
            }
        }

        /// <summary>
        /// The description of this menu.
        /// </summary>
        private string description;
        /// <summary>
        /// The description of this menu.
        /// </summary>
        public string Description
        {
            get => description;
            set
            {
                description = value;
                OnDescriptionChanged?.Invoke();
            }
        }

        /// <summary>
        /// The icon of this menu.
        /// </summary>
        private Sprite icon;

        /// <summary>
        /// The icon of this menu.
        /// </summary>
        public Sprite Icon
        {
            get => icon;
            set
            {
                icon = value;
                OnIconChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// The keyword to close the menu.
        /// </summary>
        private string closeMenuKeyword = "close menu";

        /// <summary>
        /// The keyword to close the menu.
        /// </summary>
        public string CloseMenuKeyword
        {
            get => closeMenuKeyword;
            set
            {
                closeMenuKeyword = value;
                OnCloseMenuCommandChanged?.Invoke();
            }
        }

        /// <summary>
        /// Whether the menu is shown.
        /// </summary>
        private bool showMenu;
        /// <summary>
        /// Whether the menu is shown.
        /// </summary>
        public bool ShowMenu
        {
            get => showMenu;
            set
            {
                showMenu = value;
                OnShowMenuChanged?.Invoke();
            }
        }

        /// <summary>
        /// Toggles the menu.
        /// </summary>
        /// <see cref="ShowMenu"/>
        public void ToggleMenu()
        {
            ShowMenu = !ShowMenu;
        }

        /// <summary>
        /// Whether the menu can be closed by not making any selection.
        /// </summary>
        private bool allowNoSelection = true;
        /// <summary>
        /// Whether the menu can be closed by not making any selection.
        /// </summary>
        public bool AllowNoSelection
        {
            get => allowNoSelection;
            set
            {
                allowNoSelection = value;
                OnAllowNoSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// Whether to hide the menu after selection.
        /// </summary>
        private bool hideAfterSelection = true;
        /// <summary>
        /// Whether to hide the menu after selection.
        /// </summary>
        public bool HideAfterSelection
        {
            get => hideAfterSelection;
            set
            {
                hideAfterSelection = value;
                OnHideAfterSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// The menu entries.
        /// </summary>
        private readonly List<T> entries = new();

        /// <summary>
        /// A read-only wrapper around the menu entries.
        /// </summary>
        /// <see cref="entries"/>
        public IList<T> Entries => entries.AsReadOnly();

        /// <summary>
        /// The keyword listener.
        /// </summary>
        protected KeywordInput KeywordListener;
        
        /// <summary>
        /// The menu game object.
        /// </summary>
        public GameObject Menu { get; protected set; }
        
        /// <summary>
        /// Returns the game object corresponding to a menu entry.
        /// Assumes that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        /// <returns>The game object of the entry.</returns>
        public abstract GameObject EntryGameObject(T entry);

        /// <summary>
        /// Updates the keyword listener.
        /// </summary>
        protected void UpdateKeywordListener()
        {
            // stops if already listening
            if (KeywordListener != null)
            {
                KeywordListener.Unregister(HandleKeyword);
                KeywordListener.Dispose();
                KeywordListener = null;
            }
            if (!ShowMenu) return;
            // starts listening
            KeywordListener = new KeywordInput(GetKeywords().ToArray());
            KeywordListener.Register(HandleKeyword);
            KeywordListener.Start();
        }
    
        /// <summary>
        /// The keywords the menu should listen to.
        /// Appends <see cref="CloseMenuKeyword"/> if <see cref="AllowNoSelection"/> is <code>true</code>.
        /// </summary>
        /// <returns>titles the menu should listen to</returns>
        protected virtual IEnumerable<string> GetKeywords()
        {
            IEnumerable<string> keywords = Entries.Select(entry => entry.Title);
            if (AllowNoSelection) keywords = keywords.Append(CloseMenuKeyword);
            return keywords;
        }

        /// <summary>
        /// Adds a menu entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        public virtual void AddEntry(T entry)
        {
            entries.Add(entry);
            OnEntryAdded?.Invoke(entry);
        }

        /// <summary>
        /// Removes a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry"></param>
        public void RemoveEntry(T entry)
        {
            entries.Remove(entry);
            OnEntryRemoved?.Invoke(entry);
        }

        /// <summary>
        /// Selects a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        public virtual void SelectEntry(T entry)
        {
            entry.DoAction();
            OnEntrySelected?.Invoke(entry);
            if (HideAfterSelection) ShowMenu = false;
        }
    
        /// <summary>
        /// Triggers when a keyword was recognized.
        /// Selects the corresponding entry or executes a special action (e.g. <see cref="CloseMenuKeyword"/>).
        /// </summary>
        /// <param name="args">The phrase recognized.</param>
        /// <see cref="GetKeywords"/>
        protected virtual void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            if (args.text == CloseMenuKeyword)
            {
                ShowMenu = false;
            }
            else
            {
                T entry = entries.Find(entry => entry.Title == args.text);
                SelectEntry(entry);
            }
            OnKeywordRecognized?.Invoke(args.text);
        }

        /// <summary>
        /// Adds listeners for updating the keyword listener.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            // updating the keyword listener
            UpdateKeywordListener();
            // events for the keyword listener
            OnShowMenuChanged += UpdateKeywordListener;
            OnAllowNoSelectionChanged += UpdateKeywordListener;
            OnCloseMenuCommandChanged += UpdateKeywordListener;
            OnEntryAdded += _ => UpdateKeywordListener();
            OnEntryRemoved += _ => UpdateKeywordListener();
        }

        /// <summary>
        /// Updates the component for the current platform.
        /// Destroys the component if the corresponding menu has been destroyed.
        /// </summary>
        protected override void Update()
        {
            if (Menu == null) 
                Destroy(this);
            else 
                base.Update();
        }

        /// <summary>
        /// Destroys the menu when the component has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (Menu!= null) Destroy(Menu);
        }

        /// <summary>
        /// Triggers when <see cref="Title"/> was changed.
        /// </summary>
        public event UnityAction OnTitleChanged;

        /// <summary>
        /// Triggers when <see cref="Description"/> was changed.
        /// </summary>
        public event UnityAction OnDescriptionChanged;
        
        /// <summary>
        /// Triggers when <see cref="Icon"/> was changed.
        /// </summary>
        public event UnityAction OnIconChanged;

        /// <summary>
        /// Triggers when <see cref="ShowMenu"/> was changed.
        /// </summary>
        public event UnityAction OnShowMenuChanged;

        /// <summary>
        /// Triggers when <see cref="AllowNoSelection"/> was changed.
        /// </summary>
        public event UnityAction OnAllowNoSelectionChanged;
        
        /// <summary>
        /// Triggers when <see cref="HideAfterSelection"/> was changed.
        /// </summary>
        public event UnityAction OnHideAfterSelectionChanged;

        /// <summary>
        /// Triggers when <see cref="CloseMenuKeyword"/> was changed
        /// </summary>
        public event UnityAction OnCloseMenuCommandChanged;

        /// <summary>
        /// Triggers when an entry was added. (<see cref="AddEntry"/>)
        /// </summary>
        public event UnityAction<T> OnEntryAdded;

        /// <summary>
        /// Triggers when an entry was removed. (<see cref="RemoveEntry"/>)
        /// </summary>
        public event UnityAction<T> OnEntryRemoved;

        /// <summary>
        /// Triggers when an entry was selected. (<see cref="SelectEntry"/>)
        /// </summary>
        public event UnityAction<T> OnEntrySelected;

        /// <summary>
        /// Triggers when a keyword was recognized by the listener. (<see cref="HandleKeyword"/>)
        /// </summary>
        public event UnityAction<string> OnKeywordRecognized;
    }
}
