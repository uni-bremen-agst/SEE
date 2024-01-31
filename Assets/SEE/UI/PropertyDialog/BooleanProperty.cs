using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.UI.PropertyDialog;
using SEE.UI.Tooltip;
using SEE.Utils;
using System;
using TMPro;
using UnityEngine;

public class BooleanProperty : Property<bool>
{
    private const string inputFieldPrefab = "Prefabs/UI/InputFields/BooleanInputField";
    private SwitchManager switchManager;
    private GameObject inputField;
    private GameObject parentOfInputField;
    private Tooltip tooltip;

    private bool savedValue;
    public override bool Value { 
        get => switchManager != null ? switchManager.isOn : savedValue; 
        set 
        {
            savedValue = value;
            if (switchManager != null)
            {
                switchManager.isOn = value;
                switchManager.UpdateUI();
            }
        }
    }

    protected override void StartDesktop()
    {
        inputField = PrefabInstantiator.InstantiatePrefab(inputFieldPrefab, instantiateInWorldSpace: false);
        switchManager = inputField.transform.Find("Switch").gameObject.MustGetComponent<SwitchManager>();
        switchManager.isOn = savedValue;
        switchManager.UpdateUI();
        
        if (parentOfInputField != null)
        {
            SetParent(parentOfInputField);
        }
        inputField.gameObject.name = Name;

        Transform placeHolder = inputField.transform.Find("Label");
        if (placeHolder.gameObject.TryGetComponentOrLog(out TextMeshProUGUI nameTextMeshPro))
        {
            nameTextMeshPro.text = Name;
        }

        tooltip = gameObject.AddComponent<Tooltip>();
        if (inputField.TryGetComponentOrLog(out PointerHelper pointerHelper))
        {
            pointerHelper.EnterEvent.AddListener(_ => tooltip.Show(Description));
            pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());
        }
    }

    public override void SetParent(GameObject parent)
    {
        if (inputField != null)
        {
            inputField.transform.SetParent(parent.transform);
        }
        else
        {
            /// save for later assignment in <see cref="StartDesktop"/>
            parentOfInputField = parent;
        }
    }
}
