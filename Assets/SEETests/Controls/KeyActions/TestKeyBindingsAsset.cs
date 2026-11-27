using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.KeyActions
{
    /// <summary>Checks the shipped keyboard action contract without loading user preferences.</summary>
    internal class TestKeyBindingsAsset
    {
        /// <summary>Loads the same resource requested by the production binding loader.</summary>
        private static InputActionAsset LoadAsset()
        {
            InputActionAsset asset = Resources.Load<InputActionAsset>("SEE Keyboard Actions");
            Assert.That(asset, Is.Not.Null, "The production Resources path must resolve.");
            return asset;
        }

        /// <summary>Checks resource import and the map used by SEE.</summary>
        [Test]
        public void TestResourceAndMap()
        {
            Assert.That(LoadAsset().FindActionMap("SEE"), Is.Not.Null);
        }

        /// <summary>Checks every available registered action without initializing persisted bindings.</summary>
        [Test]
        public void TestKeyActionsExist()
        {
            InputActionMap map = LoadAsset().FindActionMap("SEE", true);
            foreach (KeyAction action in Enum.GetValues(typeof(KeyAction)))
            {
                Assert.That(map.FindAction(action.ToString()), Is.Not.Null, action.ToString());
            }
        }

        /// <summary>Checks the frozen default bindings, including direct keys and player menu digits.</summary>
        /// <param name="name">Stable action name.</param>
        /// <param name="path">Expected default keyboard control.</param>
        [TestCase("Help", "<Keyboard>/h")]
        [TestCase("ToggleMenu", "<Keyboard>/space")]
        [TestCase("ToggleSettings", "<Keyboard>/pause")]
        [TestCase("ConfigMenu", "<Keyboard>/k")]
        [TestCase("ToggleBrowser", "<Keyboard>/f4")]
        [TestCase("ToggleMirror", "<Keyboard>/f8")]
        [TestCase("Undo", "<Keyboard>/z")]
        [TestCase("Redo", "<Keyboard>/y")]
        [TestCase("CloseNotifications", "<Keyboard>/x")]
        [TestCase("ToggleFaceCam", "<Keyboard>/i")]
        [TestCase("ToggleFaceCamPosition", "<Keyboard>/f3")]
        [TestCase("ToggleVoiceControl", "<Keyboard>/period")]
        [TestCase("OpenSnapshotWindow", "<Keyboard>/f7")]
        [TestCase("BoostCameraSpeed", "<Keyboard>/leftShift")]
        [TestCase("MoveForward", "<Keyboard>/w")]
        [TestCase("MoveBackward", "<Keyboard>/s")]
        [TestCase("MoveRight", "<Keyboard>/d")]
        [TestCase("MoveLeft", "<Keyboard>/a")]
        [TestCase("MoveUp", "<Keyboard>/q")]
        [TestCase("MoveDown", "<Keyboard>/e")]
        [TestCase("ToggleCharts", "<Keyboard>/m")]
        [TestCase("ToggleMetricHoveringSelection", "<Keyboard>/n")]
        [TestCase("ToggleHolisticMetricsMenu", "<Keyboard>/c")]
        [TestCase("TreeView", "<Keyboard>/tab")]
        [TestCase("ToggleEdges", "<Keyboard>/v")]
        [TestCase("Unselect", "<Keyboard>/u")]
        [TestCase("Cancel", "<Keyboard>/escape")]
        [TestCase("Reset", "<Keyboard>/r")]
        [TestCase("ZoomInto", "<Keyboard>/g")]
        [TestCase("Snap", "<Keyboard>/leftAlt")]
        [TestCase("DragHovered", "<Keyboard>/leftCtrl")]
        [TestCase("ToggleCameraLock", "<Keyboard>/l")]
        [TestCase("Pointing", "<Keyboard>/p")]
        [TestCase("HandAnimations", "<Keyboard>/f6")]
        [TestCase("SetMarker", "<Keyboard>/insert")]
        [TestCase("DeleteMarker", "<Keyboard>/delete")]
        [TestCase("ToggleEvolutionCanvases", "<Keyboard>/t")]
        [TestCase("Previous", "<Keyboard>/leftArrow")]
        [TestCase("Next", "<Keyboard>/rightArrow")]
        [TestCase("ToggleAutoPlay", "<Keyboard>/f9")]
        [TestCase("IncreaseAnimationSpeed", "<Keyboard>/upArrow")]
        [TestCase("DecreaseAnimationSpeed", "<Keyboard>/downArrow")]
        [TestCase("ToggleExecutionOrder", "<Keyboard>/o")]
        [TestCase("ExecuteToBreakpoint", "<Keyboard>/b")]
        [TestCase("FirstStatement", "<Keyboard>/home")]
        [TestCase("ShowWindowMenu", "<Keyboard>/f1")]
        [TestCase("ToggleTextChat", "<Keyboard>/f2")]
        [TestCase("ToggleVoiceChat", "<Keyboard>/f5")]
        [TestCase("PartUndo", "<Keyboard>/capsLock")]
        [TestCase("MoveObjectUp", "<Keyboard>/numpad8")]
        [TestCase("MoveObjectDown", "<Keyboard>/numpad2")]
        [TestCase("MoveObjectLeft", "<Keyboard>/numpad4")]
        [TestCase("MoveObjectRight", "<Keyboard>/numpad6")]
        [TestCase("MoveObjectForward", "<Keyboard>/numpad9")]
        [TestCase("MoveObjectBackward", "<Keyboard>/numpad3")]
        [TestCase("DrawableManagerView", "<Keyboard>/numpad0")]
        [TestCase("SavePathPosition", "<Keyboard>/f11")]
        [TestCase("TogglePathPlaying", "<Keyboard>/f12")]
        [TestCase("ToggleEchoFace", "<Keyboard>/f")]
        [TestCase("DirectLeftControl", "<Keyboard>/leftCtrl")]
        [TestCase("DirectRightControl", "<Keyboard>/rightCtrl")]
        [TestCase("DirectLeftShift", "<Keyboard>/leftShift")]
        [TestCase("DirectRightShift", "<Keyboard>/rightShift")]
        [TestCase("DirectEscape", "<Keyboard>/escape")]
        [TestCase("PlayerMenuDigit0", "<Keyboard>/0")]
        [TestCase("PlayerMenuDigit1", "<Keyboard>/1")]
        [TestCase("PlayerMenuDigit2", "<Keyboard>/2")]
        [TestCase("PlayerMenuDigit3", "<Keyboard>/3")]
        [TestCase("PlayerMenuDigit4", "<Keyboard>/4")]
        [TestCase("PlayerMenuDigit5", "<Keyboard>/5")]
        [TestCase("PlayerMenuDigit6", "<Keyboard>/6")]
        [TestCase("PlayerMenuDigit7", "<Keyboard>/7")]
        [TestCase("PlayerMenuDigit8", "<Keyboard>/8")]
        [TestCase("PlayerMenuDigit9", "<Keyboard>/9")]
        public void TestDefaultBinding(string name, string path)
        {
            InputAction action = LoadAsset().FindActionMap("SEE", true).FindAction(name, true);
            Assert.That(action.bindings.Count, Is.EqualTo(1), name);
            InputBinding binding = action.bindings[0];
            Assert.That(binding.isComposite || binding.isPartOfComposite, Is.False, name);
            Assert.That(binding.path, Is.EqualTo(path), name);
        }

        /// <summary>Registered shortcuts must be unique; direct polling actions intentionally share keys.</summary>
        [Test]
        public void TestNoDuplicateRegisteredDefaults()
        {
            InputActionMap map = LoadAsset().FindActionMap("SEE", true);
            string[] paths = Enum.GetValues(typeof(KeyAction)).Cast<KeyAction>()
                .SelectMany(key => map.FindAction(key.ToString(), true).bindings)
                .Select(binding => binding.path.ToLowerInvariant()).ToArray();
            Assert.That(paths, Is.Unique);
        }
    }
}
