using UnityEngine;
using System;
using System.Collections;
using SEE.GO;
using SEE.Utils;
using Michsky.UI.ModernUIPack;
using SEE.Game.Avatars;
using TMPro;

namespace SEE.UI
{
    /// <summary>
    /// Contains all important functions for the actions responsible for hand animations.
    /// Also contains the functions for creation of the UI element with instructions for hand calibration.
    /// <summary>
    internal class HandAnimationsActions : PlatformDependentComponent
    {
       /// <summary>
        /// Path to the HelpSystemEntrySpace prefab. It is a panel in which
        /// the instruction will be placed. This prefab is used to make UI elements look uniform.
        /// </summary>
        private const string instructionsSpacePrefab = "Prefabs/UI/HelpSystemEntrySpace";

        /// <summary>
        /// Path to the prefab which contains space for a text and a picture, as well as a
        /// close button.
        /// </summary>
        private const string instructionsPrefab = "Prefabs/UI/InstructionsForHandAnimations";

        /// <summary>
        /// The path name of the game object holding the TextMeshPro component in which the
        /// instructions are printed textually.
        /// </summary>
        private const string textFieldPath = "Content/TextField";

        /// <summary>
        /// The GameObject representing the instructions.
        /// It will be instantiated from the prefab <see cref="instructionsPrefab"/>.
        /// It will be a child of <see cref="instructionsSpace"/>.
        /// </summary>
        private GameObject instructions;

        /// <summary>
        /// The GameObject which contains the instructions.
        /// It will be instantiated from the prefab <see cref="instructionsSpacePrefab"/>.
        /// It will be a child of <see cref="PlatformDependentComponent.Canvas"/>.
        /// </summary>
        private GameObject instructionsSpace;

        /// <summary>
        /// The text inside of the instructions.
        /// </summary>
        private TextMeshProUGUI text;

        /// <summary>
        /// The menu that needs to be reset after after closing the instructions.
        /// </summary>
        private HandAnimationsMenu menu;

        /// <summary>
        /// The game object representing the countdown.
        /// It will be instantiated from the prefab <see cref="countdownPrefab"/>.
        /// </summary>
        public GameObject Countdown;

        /// <summary>
        /// The text inside of the countdown-GameObject.
        /// </summary>
        private const string countdownPrefab = "Prefabs/UI/Countdown";

        /// <summary>
        /// The <see cref="BodyAnimator"/> attached to this avatar.
        /// </summary>
        private BodyAnimator bodyAnimator;

        /// <summary>
        /// Indicated whether to display instructions before activating hand animations.
        /// </summary>
        private bool isFirstActivationOfHandAnimations = true;

        /// <summary>
        /// Indicates whether to start the animations.
        /// </summary>
        public bool ShouldStartAnimations = false;

        /// <summary>
        /// Sets the <see cref="instructionsSpace"/> and <see cref="instructions"/>.
        /// <summary>
        protected override void StartDesktop()
        {
            bodyAnimator = GetComponentInParent<BodyAnimator>();
            if (instructionsSpace == null)
            {
                instructionsSpace = PrefabInstantiator.InstantiatePrefab(instructionsSpacePrefab, Canvas.transform, false);
                instructions = PrefabInstantiator.InstantiatePrefab(instructionsPrefab, instructionsSpace.transform, false);
            }
            instructionsSpace.SetActive(false);
            instructions.SetActive(false);
        }

        /// <summary>
        /// Initializes the <see cref="menu"/>.
        /// <summary>
        public void Initialize(HandAnimationsMenu handAnimationsMenu)
        {
            menu = handAnimationsMenu;
        }

        /// <summary>
        /// Creates the instructions panel and adds a listener to the "Close"-button.
        /// The instructions text will be read aloud by <see cref="PersonalAssistantBrain"/>.
        /// <summary>
        public void CreateInstructions()
        {
            if (instructionsSpace == null)
            {
                instructionsSpace = PrefabInstantiator.InstantiatePrefab(instructionsSpacePrefab, Canvas.transform, false);
                instructions = PrefabInstantiator.InstantiatePrefab(instructionsPrefab, instructionsSpace.transform, false);
            }
            instructionsSpace.SetActive(true);
            instructions.SetActive(true);

            instructionsSpace.transform.localScale = new Vector3(1.7f, 1.7f);
            RectTransform dynamicPanel = instructionsSpace.transform.GetChild(2).GetComponent<RectTransform>();
            dynamicPanel.sizeDelta = new Vector2(550, 425);
            instructions.transform.Find(textFieldPath).gameObject.TryGetComponentOrLog(out text);
            text.fontSize = 18;
            PersonalAssistantBrain.Instance?.Say(text.text);
            {
                instructions.transform.Find("Buttons/Content/Close").gameObject.TryGetComponentOrLog(out ButtonManagerWithIcon manager);
                manager.clickEvent.AddListener(Close);
            }
        }

        /// <summary>
        /// Closes the instructions and resets the HandAnimationsMenu to the start.
        /// If the instructions were displayed before the first activation of hand animations,
        /// starts the animations.
        /// </summary>
        public void Close()
        {
            PersonalAssistantBrain.Instance?.Stop();
            menu.Reset();
            Destroyer.Destroy(instructionsSpace);
            if (ShouldStartAnimations)
            {
                HandleInstructionsClosed();
            }
        }

        /// <summary>
        /// Starts a countdown after which the user's starting hand positions will be
        /// recalibrated for better hand animations. If hand animations are disabled, 
        /// shows a warning instead. 
        /// </summary>
        public void Recalibrate()
        {
            if (Countdown == null)
            {
                Countdown = CreateCountdown();
            }

            if (bodyAnimator.IsUsingHandAnimations)
            {
                Countdown.SetActive(true);
                StartCoroutine(StartRecallibrationWithDelay());
            }
            else
            {
                StartCoroutine(ShowRecalibrationBlockedWarning());
            }
        }

        /// <summary>
        /// Displays a warning message informing the user that re-calibration
        /// cannot be performed while animations are disabled. 
        /// </summary>
        public IEnumerator ShowRecalibrationBlockedWarning()
        {
            GameObject textField = Countdown.transform.Find(textFieldPath).gameObject;
            textField.GetComponent<TextMeshProUGUI>().fontSize = 60;
            textField.GetComponent<TextMeshProUGUI>().text = "Re-calibration \n is unavailable \n while animations \n are disabled.";
            Countdown.SetActive(true);
            yield return new WaitForSeconds(3f);
            Countdown.SetActive(false);
        }

        /// <summary>
        /// Creates a countdown for recalibration.
        /// </summary>
        /// <returns>GameObject for the countdown.</returns>
        public GameObject CreateCountdown()
        {
            return PrefabInstantiator.InstantiatePrefab(countdownPrefab, Canvas.transform, false);
        }

        /// <summary>
        /// Starts recalibration after the countdown.
        /// </summary>
        public IEnumerator StartRecallibrationWithDelay()
        {
            GameObject textField = Countdown.transform.Find(textFieldPath).gameObject;
            textField.GetComponent<TextMeshProUGUI>().fontSize = 400;

            for (int i = 3; i > 0; i--)
            {
                textField.GetComponent<TextMeshProUGUI>().text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitForSeconds(1f);
            textField.GetComponent<TextMeshProUGUI>().fontSize = 115;
            textField.GetComponent<TextMeshProUGUI>().text = "Finished!";
            yield return new WaitForSeconds(2f);
            Countdown.SetActive(false);
            bodyAnimator.IsRecalibrationNeeded = true;
        }

        /// <summary>
        /// Starts hand animations after the instructions panel was closed.
        /// </summary>
        private void HandleInstructionsClosed()
        {
            bodyAnimator.ToggleHandAnimations();
            ShouldStartAnimations = false;
            StartCoroutine(WaitForHandsInTheStartPosition());
        }

        /// <summary>
        /// If it's the first activation of hand animations, waits untill the avatar's
        /// hands are in the starting position and then indicates that the user
        /// can move their hands freely.
        /// </summary>
        private IEnumerator WaitForHandsInTheStartPosition()
        {
            yield return new WaitUntil(() => bodyAnimator.HandsAnimator.StartHandsPositionReached);

            Countdown = CreateCountdown();
            Countdown.SetActive(true);
            GameObject textField = Countdown.transform.Find(textFieldPath).gameObject;
            textField.GetComponent<TextMeshProUGUI>().fontSize = 115;
            textField.GetComponent<TextMeshProUGUI>().text = "Finished!";
            yield return new WaitForSeconds(2f);
            Countdown.SetActive(false);
        }

        /// <summary>
        /// Activates the avatar's hand animations.
        /// If this is the first time the animations are activated, displays instructions beforehand.
        /// <summary>
        public void ToggleHandAnimations()
        {
            if (isFirstActivationOfHandAnimations)
            {
                ShouldStartAnimations = true;
                CreateInstructions();
                isFirstActivationOfHandAnimations = false;
            }
            else
            {
                bodyAnimator.ToggleHandAnimations();
            }
        }
    }
}
