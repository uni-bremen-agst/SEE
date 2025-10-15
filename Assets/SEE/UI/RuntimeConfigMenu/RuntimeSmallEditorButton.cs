using System;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Settings can be displayed in a small editor menu.
    /// The menu hovers above the scene so that the city is completely visible
    /// while changing the setting.
    /// </summary>
    public class RuntimeSmallEditorButton : PlatformDependentComponent
    {
        /// <summary>
        /// The prefab for small editor window.
        /// </summary>
        private const string smallWindowPrefab =
            RuntimeTabMenu.RuntimeConfigPrefabFolder + "RuntimeConfig_SmallConfigWindow";

        /// <summary>
        /// The editor window.
        /// </summary>
        public GameObject SmallEditor { get; private set; }

        /// <summary>
        /// The button for opening the editor window.
        /// </summary>
        private Button button;

        /// <summary>
        /// The action that creates the widget.
        /// </summary>
        public Action<GameObject> CreateWidget;

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
                if (value == showMenu)
                {
                    return;
                }

                if (value)
                {
                    SmallEditor = PrefabInstantiator.InstantiatePrefab(smallWindowPrefab, Canvas.transform, false);
                    SmallEditor.transform.Find("CloseButton").GetComponent<Button>().onClick
                               .AddListener(() => ShowMenu = false);
                    CreateWidget(SmallEditor.transform.Find("Content").gameObject);
                }
                else
                {
                    Destroyer.Destroy(SmallEditor);
                }

                showMenu = value;
                OnShowMenuChanged?.Invoke();
            }
        }

        /// <summary>
        /// Initiates the listeners.
        /// </summary>
        protected override void StartDesktop()
        {
            button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => ShowMenu = true);
        }

        /// <summary>
        /// Initiates the listeners.
        /// <see cref="StartDesktop" />
        /// </summary>
        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Initiates the listeners.
        /// <see cref="StartDesktop" />
        /// </summary>
        protected override void StartTouchGamepad()
        {
            StartDesktop();
        }

        /// <summary>
        /// Triggers when <see cref="ShowMenu" /> is changed.
        /// </summary>
        public event UnityAction OnShowMenuChanged;
    }
}
