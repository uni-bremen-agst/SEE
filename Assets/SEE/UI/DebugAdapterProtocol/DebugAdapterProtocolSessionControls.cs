using DG.Tweening;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.UI;

namespace SEE.UI.DebugAdapterProtocol
{
    public class DebugAdapterProtocolSessionControls : PlatformDependentComponent
    {
        public event UnityAction OnTerminalButtonClicked;

        private const string DebugControlsPrefab = "Prefabs/UI/DebugAdapterProtocolSessionControls";

        private GameObject controls;

        protected override void StartDesktop()
        {
            controls = PrefabInstantiator.InstantiatePrefab(DebugControlsPrefab, Canvas.transform, false);
            controls.transform.position = new Vector3(Screen.width / 2, Screen.height / 4, 0);

            Button terminalButton = controls.transform.Find("Terminal").gameObject.AddOrGetComponent<Button>();
            terminalButton.onClick.AddListener(() => OnTerminalButtonClicked?.Invoke());

            OnComponentInitialized += () =>
            {
                Window.BaseWindow window = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer].ActiveWindow;
            };
        }
    }
}