using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;

namespace SEE.UI.Menu
{
    /// <summary>
    /// A platform dependent menu.
    /// Contains a title, description, icon and listens to specific keywords.
    /// </summary>
    public partial class SimpleMenu<T> : PlatformDependentComponent where T : MenuEntry
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
        /// The keyword listener.
        /// </summary>
        protected KeywordInput KeywordListener;

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
        /// Destroying the component also destroys the menu.
        /// Lets <see cref="KeywordListener"/> stop listening.
        /// </summary>
        /// <remarks>Called by Unity when this object is destroyed.</remarks>
        protected override void OnDestroy()
        {
            KeywordListener?.Stop();
            KeywordListener?.Dispose();
            KeywordListener = null;

            base.OnDestroy();
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
                if (showMenu != value)
                {
                    showMenu = value;
                    OnShowMenuChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// The parent of the menu.
        /// Uses <see cref="Canvas"/> by default.
        /// </summary>
        private Transform parent;

        /// <summary>
        /// The parent of the menu.
        /// Uses <see cref="Canvas"/> by default.
        /// </summary>
        public Transform Parent
        {
            get => parent != null ? parent : Canvas.transform;
            set
            {
                if (parent != value)
                {
                    parent = value;
                    OnParentChanged?.Invoke();
                }
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
        /// Updates the keyword listener.
        /// </summary>
        protected virtual void UpdateKeywordListener()
        {
            // stops if already listening
            if (KeywordListener != null)
            {
                KeywordListener.Unregister(HandleKeyword);
                KeywordListener.Dispose();
                KeywordListener = null;
            }
            if (!ShowMenu)
            {
                return;
            }
            // starts listening
            KeywordListener = new KeywordInput(GetKeywords().ToArray());
            KeywordListener.Register(HandleKeyword);
            KeywordListener.Start();
        }

        /// <summary>
        /// The keywords the menu should listen to.
        /// </summary>
        /// <returns>titles the menu should listen to</returns>
        protected virtual IEnumerable<string> GetKeywords()
        {
            IEnumerable<string> keywords = Enumerable.Empty<string>();
            if (CloseMenuKeyword != null)
            {
                keywords = keywords.Append(CloseMenuKeyword);
            }
            return keywords;
        }

        /// <summary>
        /// Triggers when a keyword was recognized.
        /// Executes a special action depending on the keyword.
        /// </summary>
        /// <param name="args">The phrase recognized.</param>
        /// <see cref="GetKeywords"/>
        /// <see cref="CloseMenuKeyword"/>
        protected virtual void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            if (args.text == CloseMenuKeyword)
            {
                ShowMenu = false;
            }
            OnKeywordRecognized?.Invoke(args.text);
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
        /// Triggers when <see cref="CloseMenuKeyword"/> was changed.
        /// </summary>
        public event UnityAction OnCloseMenuCommandChanged;

        /// <summary>
        /// Triggers when <see cref="Parent"/> was changed.
        /// </summary>
        public event UnityAction OnParentChanged;

        /// <summary>
        /// Triggers when a keyword was recognized by the listener. (<see cref="HandleKeyword"/>)
        /// </summary>
        public event UnityAction<string> OnKeywordRecognized;
    }
}
