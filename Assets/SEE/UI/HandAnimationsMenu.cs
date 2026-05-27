using System.Collections.Generic;
using SEE.UI.Menu;
using SEE.Utils;
using SEE.Controls;
using UnityEngine;
using SEE.Game.Avatars;

namespace SEE.UI
{
    /// <summary>
    /// The menu responsible for hand animations actions (as for now).
    /// </summary>
    public class HandAnimationsMenu : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action from.
        /// </summary>
        private NestedListMenu menu;

        /// <summary>
        /// The <see cref="HandAnimationActions"/> component attached to this Desktop Player
        /// that is responsible for hand animations actions.
        /// </summary>
        private HandAnimationsActions actionsForHandAnimations;

        /// <summary>
        /// Creates the <see cref="menu"/> and initializes the <see cref="bodyAnimator"/>.
        /// </summary>
        private void Start()
        {
            menu = CreateMenu(gameObject);
        }

        /// <summary>
        /// Creates the <see cref="menu"/> and initializes the <see cref="actionsForHandAnimations"/>.
        /// </summary>
        /// <returns>The created menu.</returns>
        private NestedListMenu CreateMenu(GameObject attachTo = null)
        {
            GameObject handAnimationsMenuGO = attachTo;
            actionsForHandAnimations = handAnimationsMenuGO.AddComponent<HandAnimationsActions>();
            actionsForHandAnimations.Initialize(this);

            NestedListMenu mainMenu = gameObject.AddComponent<NestedListMenu>();
            mainMenu.Title = "Hand Animations Menu";
            mainMenu.Description = "Menu responsible for hand animations actoins (as for now).";
            IList<MenuEntry> entries = ActionEntries();
            mainMenu.AddEntries(entries);
            mainMenu.HideAfterSelection = true;
            return mainMenu;
        }

        /// <summary>
        /// Builds inner nodes of the menu.
        /// </summary>
        /// <returns>MenuEntries for the menu.</returns>
        private IList<MenuEntry> ActionEntries()
        {
            List<MenuEntry> menuEntries = new()
            {
                new NestedMenuEntry<MenuEntry>(innerEntries: HandAnimationsEntries(),
                                    title: "Hand Animations",
                                    description: " ",
                                    entryColor: Color.yellow,
                                    icon: '+')
            };

            return menuEntries;
        }

        /// <summary>
        /// Builds inner entries for actions responsible for hand animations.
        /// </summary>
        /// <returns>MenuEntries for hand animations.</returns>
        private IList<MenuEntry> HandAnimationsEntries()
        {
            Color color = Color.blue;

            return new List<MenuEntry>{

                new(SelectAction: ToggleHandAnimations,
                    Title: "Toggle Hand Animations",
                    Description: "Turns on/off hand animations with camera feed.",
                    EntryColor: NextColor(),
                    Icon: ' '),

                new(SelectAction: Recalibrate,
                Title: "Re-calibrate Animations",
                Description: "Recalibrate starting hand coordinates of the user for hand animations",
                EntryColor: NextColor(),
                Icon: ' '),

                new(SelectAction: ShowInstructionsForHandAnimations,
                Title: "Instructions for calibration",
                Description: "How to re-callibrate hand coordinates for better animations",
                EntryColor: NextColor(),
                Icon: ' '),
            };

            Color NextColor()
            {
                Color result = color;
                color = color.Lighter();
                return result;
            }
        }

        /// <summary>
        /// Toggles between using hand animations with MediaPipe and not using them.
        /// </summary>
        private void ToggleHandAnimations()
        {
            actionsForHandAnimations.ToggleHandAnimations();
        }

        /// <summary>
        /// Recalibrates the user's starting hand positions for better hand animations.
        /// </summary>
        private void Recalibrate()
        {
            actionsForHandAnimations.Recalibrate();
        }

        /// <summary>
        /// Shows instructions for the user's initial hand position when activating animations and re-calibration.
        /// </summary>
        private void ShowInstructionsForHandAnimations()
        {
            actionsForHandAnimations.CreateInstructions();
        }

        /// <summary>
        /// Resets the menu after closing instructions.
        /// </summary>
        public void Reset()
        {
            menu.ResetToBase();
        }

        /// <summary>
        /// Displays the menu if the user presses the corresponding button on the keyboard.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleHandAnimations())
            {
                if (!menu.ShowMenu)
                {
                    menu.ShowMenu = true;
                }
            }
        }
    }
}
