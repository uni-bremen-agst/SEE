using System;
using SEE.Game.UI;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RuntimeSmallEditorButton : PlatformDependentComponent
{
    public const string SMALLWINDOW_PREFAB = RuntimeTabMenu.RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfig_SmallConfigWindow";

    public Action<GameObject> CreateWidget;
    
    private Button button;
    private static GameObject smallEditor;

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
                smallEditor.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => ShowMenu = false);
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

    protected override void StartVR() => StartDesktop();

    protected override void StartTouchGamepad() => StartDesktop();

    public event UnityAction OnShowMenuChanged;
}
