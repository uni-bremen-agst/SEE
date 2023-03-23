using SEE.Game.UI;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RuntimeSmallEditorButton : PlatformDependentComponent
{
    public const string SMALLWINDOW_PREFAB = RuntimeTabMenu.RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfig_SmallConfigWindow";

    private Button button;
    
    private static GameObject smallEditor;
    private Transform originalParent;
    private int originalSiblingIndex;

    private bool showMenu;
    public bool ShowMenu
    {
        get => showMenu;
        set
        {
            if (value == showMenu) return;
            if (value && originalParent != null) return;
            if (!value && originalParent == null) return;
            if (value)
            {
                originalParent = transform.parent;
                originalSiblingIndex = transform.GetSiblingIndex();
                
                smallEditor = PrefabInstantiator.InstantiatePrefab(SMALLWINDOW_PREFAB, Canvas.transform, false);
                smallEditor.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => ShowMenu = false);
                transform.SetParent(smallEditor.transform.Find("Content"));
            }
            else
            {
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(originalSiblingIndex);
                Destroyer.Destroy(smallEditor);
                originalParent = null;
            }
            button.enabled = !value;
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
