using System;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeSmallEditorButton : PlatformDependentComponent
    {
        public const string SMALLWINDOW_PREFAB =
            RuntimeTabMenu.RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfig_SmallConfigWindow";

        private static GameObject smallEditor;

        private Button button;

        public Action<GameObject> CreateWidget;

        private bool showMenu;

        public bool ShowMenu
        {
            get => showMenu;
            set
            {
                if (value == showMenu) return;
                if (value)
                {
                    smallEditor = PrefabInstantiator.InstantiatePrefab(SMALLWINDOW_PREFAB, Canvas.transform, false);
                    smallEditor.transform.Find("CloseButton").GetComponent<Button>().onClick
                        .AddListener(() => ShowMenu = false);
                    CreateWidget(smallEditor.transform.Find("Content").gameObject);
                }
                else
                {
                    Destroyer.Destroy(smallEditor);
                }

                showMenu = value;
                OnShowMenuChanged?.Invoke();
            }
        }

        protected override void StartDesktop()
        {
            button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => ShowMenu = true);
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        protected override void StartTouchGamepad()
        {
            StartDesktop();
        }

        public event UnityAction OnShowMenuChanged;
    }
}