using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using UnityEngine.Assertions;
using SEE.Controls.Actions;
using System.Linq;
using SEE.Controls;
using UnityEngine.UI;
using System;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Responsible for the mobile UI for menus.
    /// </summary>
    public partial class SimpleMenu<T>
    {
        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added as a child to the <see cref="Canvas"/> if it doesn't exist yet.
        /// </summary>
        private const string MOBLIE_MENU_PREFAB = "Prefabs/UI/Mobile Menu";

        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added for each menu entry in <see cref="entries"/>.
        /// </summary>
        private const string ICON_BUTTON_PREFAB = "Prefabs/UI/IconButton";

        /// <summary>
        /// The GameObject which has the three panels attached
        /// </summary>
        private GameObject MobileMenuGameObject;

        /// <summary>
        /// Multidimensional array for the buttons in the mobile menu on the right screen side 
        /// </summary>
        private GameObject[][] buttons = new GameObject[5][];

        /// <summary>
        /// Array for the quick menu on the left side of the mobile device
        /// </summary>
        private GameObject[] quickButtons = new GameObject[6];

        /// <summary>
        /// Whether the menu on the left is expanded or not 
        /// </summary>
        private bool QuickMenuShown = false;
        
        /// <summary>
        /// Whether the menu on the right is expanded or not 
        /// </summary>
        private bool MobileMenuShown = false;

        /// <summary>
        /// Vertical menu panel on the right
        /// </summary>
        private Transform menuPanelVertical;

        /// <summary>
        /// Horizontal panel on the right
        /// </summary>
        private Transform menuPanelHorizontal;

        /// <summary>
        /// Panel on the left top side 
        /// </summary>
        private Transform quickMenuPanel;


        protected override void StartMobile()
        {
            InitializeMobileMenu(); 
        }

        protected override void UpdateMobile()
        {
            if (MenuShown != CurrentMenuShown)
            {
                if (MenuShown)
                {
                    // Move window to the top of the hierarchy (which, confusingly, is actually at the bottom)
                    // so that this menu is rendered over any other potentially existing menu on the UI canvas
                    MenuGameObject.transform.SetAsLastSibling();
                    if (Manager)
                    {
                        Manager.OpenWindow();
                    }
                }
                else
                {
                    if (Manager)
                    {
                        Manager.CloseWindow();
                    }
                }
                CurrentMenuShown = MenuShown;
            }
        }

        /// <summary>
        /// Initializes the mobile Menu with all the icon buttons
        /// </summary>
        protected void InitializeMobileMenu()
        {
            #region set up buttons
            // for the entry menu the entries count is 3 (Host, Client, Settings),
            // therefore the menu need to be set up in the desktop way
            if (Entries.Count < 18) 
            {
                SetUpDesktopWindow();
                SetUpDesktopContent();
            }
            // count == 21 -> represents all entries in the mobile menu. The following set up depends on 
            // a correct count and order of the entries
            else
            {
                MenuShown = false;
                MobileMenuGameObject = PrefabInstantiator.InstantiatePrefab(MOBLIE_MENU_PREFAB, Canvas.transform, false);

                menuPanelVertical = MobileMenuGameObject.transform.Find("Vertical Panel");
                menuPanelHorizontal = MobileMenuGameObject.transform.Find("Horizontal Panel");
                quickMenuPanel = MobileMenuGameObject.transform.Find("Left Panel");

                Assert.IsTrue(ActionStateType.MobileMenuTypes.Count == 18);

                AddMobileButtons(Entries);

                for (int i = 0; i < buttons.Length; i++)
                {
                    //add listener to expand menu
                    int clickedIndex = i;
                    buttons[i][0].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => SelectMode(clickedIndex));
                    for (int j = 0; j < buttons[i].Length; j++)
                    {
                        if (i > 0)
                        {
                            buttons[i][j].SetActive(false);
                        }
                        if (j == 0 && i > 0)
                        {
                            buttons[i][j].transform.SetParent(menuPanelVertical);
                        }
                        else
                        {
                            buttons[i][j].transform.SetParent(menuPanelHorizontal);
                        }
                    }
                }

                Sprite arrowLeftSprite = Resources.Load<Sprite>("Materials/ModernUIPack/Arrow Bold");
                Sprite arrowRightSprite = Resources.Load<Sprite>("Icons/Arrow Bold Right");

                quickButtons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = arrowLeftSprite;

                // Initially hide the quick menu
                foreach (GameObject btn in quickButtons)
                {
                    btn.SetActive(false);
                }

                // Setting listener to deselect objects 
                buttons[0][1].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => Deselect());

                // Setting listeners for the node interaction buttons, so that it is clear which one is active
                for (int i = 0; i < 4; i++)
                {
                    int buttonIndex = i;
                    buttons[2][buttonIndex].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => ShowNodeActiveNodeInteraction(2, buttonIndex));
                }

                // Setting the listener for the snap rotate mode
                buttons[3][1].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => ToggleSnapMode());
                if (SEEInput.SnapMobile)
                {
                    Button snapButton = buttons[3][1].GetComponent<Button>();
                    MarkButtonActive(snapButton);
                }

                // Setting the listener for the dragging move mode (single object/hole city)
                buttons[4][1].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => ToggleMoveMode());
                if (SEEInput.DragTouched)
                {
                    Button dragButton = buttons[4][1].GetComponent<Button>();
                    MarkButtonActive(dragButton);
                }
                // Setting the listener for the dragging move mode (single object/hole city)
                buttons[4][2].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => ToggleEightDirectionMode());
                if (SEEInput.EightDirectionMode)
                {
                    Button dragButton = buttons[4][2].GetComponent<Button>();
                    MarkButtonActive(dragButton);
                }

                // Adding quick menu button listeners
                quickButtons[0].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                    => TriggerRedo());
                quickButtons[1].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                     => TriggerUndo());
                quickButtons[3].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                      => RotateAction.ResetRotate());
                quickButtons[4].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(()
                      => MoveAction.ResetCityPosition());
                quickButtons[5].SetActive(true);
                quickButtons[5].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() 
                    => ExpandButton(arrowLeftSprite, arrowRightSprite));
            }
            #endregion
        }

        /// <summary>
        /// Adds the given <paramref name="buttonEntries"> as buttons to the mobile Menu.
        /// The entries are to be expected in order such as declared.
        /// </summary>
        /// <param name="buttonEntries">The entries to add to the menu in an ordered
        /// IEnumerable</param> 
        protected void AddMobileButtons(IEnumerable<T> buttonEntries)
        {
            GameObject[] selectButtons = new GameObject[2];
            GameObject[] deleteButton = new GameObject[1];
            GameObject[] nodeInteractionButtons = new GameObject[4];
            GameObject[] rotateButtons = new GameObject[2];
            GameObject[] moveButtons = new GameObject[3];
            int count = 0;
            int selectCount = 0;
            int nodeCount = 0;
            int rotateCount = 0;
            int moveCount = 0;
            int quickButtonCount = 0;
            foreach (T entry in buttonEntries)
            {
                // The count 0 represents the select mode button
                if (count == 0)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    selectButtons[selectCount] = iconButton;
                    selectButtons[selectCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    selectButtons[selectCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    selectButtons[selectCount].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => OnEntrySelected(entry));
                    count++;
                    selectCount++;
                }
                // The count smaller than 2 marks the select button group.
                else if (count < 2)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    selectButtons[selectCount] = iconButton;
                    selectButtons[selectCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    selectButtons[selectCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    count++;
                    selectCount++;
                }
                // The count 2 marks the delete Button.
                else if (count == 2)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelVertical, false);
                    deleteButton[0] = iconButton;
                    deleteButton[0].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    deleteButton[0].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    deleteButton[0].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => OnEntrySelected(entry));
                    count++;
                }
                // The count smaller 7 marks the node interaction button group.
                else if (count < 7)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    nodeInteractionButtons[nodeCount] = iconButton;
                    nodeInteractionButtons[nodeCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    nodeInteractionButtons[nodeCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    nodeInteractionButtons[nodeCount].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => OnEntrySelected(entry));
                    nodeCount++;
                    count++;
                }
                // The count 7 represents the rotate mode button
                else if(count == 7)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    rotateButtons[rotateCount] = iconButton;
                    rotateButtons[rotateCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    rotateButtons[rotateCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    rotateButtons[rotateCount].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => OnEntrySelected(entry));
                    rotateCount++;
                    count++;
                }
                // The count smaller 9 marks the rotate button group.
                else if (count < 9)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    rotateButtons[rotateCount] = iconButton;
                    rotateButtons[rotateCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    rotateButtons[rotateCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    rotateCount++;
                    count++;
                }
                // The count 9 represents the move mode button
                else if (count == 9)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    moveButtons[moveCount] = iconButton;
                    moveButtons[moveCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    moveButtons[moveCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    moveButtons[moveCount].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => OnEntrySelected(entry));
                    moveCount++;
                    count++;
                }
                // The count smaller 12 marks the move button group.
                else if (count < 12)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, menuPanelHorizontal, false);
                    moveButtons[moveCount] = iconButton;
                    moveButtons[moveCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    moveButtons[moveCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    moveCount++;
                    count++;
                }
                // The count smaller 18 marks the quick menu button group.
                else if (count < 18)
                {
                    GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, quickMenuPanel, false);
                    quickButtons[quickButtonCount] = iconButton;
                    quickButtons[quickButtonCount].GetComponent<ButtonManagerBasicIcon>().name = entry.Title;
                    quickButtons[quickButtonCount].GetComponent<ButtonManagerBasicIcon>().buttonIcon = entry.Icon;
                    count++;
                    quickButtonCount++;
                }
            }
            buttons[0] = selectButtons;
            buttons[1] = deleteButton;
            buttons[2] = nodeInteractionButtons;
            buttons[3] = rotateButtons;
            buttons[4] = moveButtons;
            
        }

        /// <summary>
        /// Selects the clicked button by its <paramref name="ClickedIndex"> and moves it to the top
        /// </summary>
        /// <param name="ClickedIndex">Index of the clicked button</param>
        private void SelectMode(int ClickedIndex)
        {
            if (MobileMenuShown)
            {
                // Set inactive first for right order. 
                for (int i = 0; i < buttons.Length; i++)
                {
                    for (int j = 0; j < buttons[i].Length; j++)
                    {
                            if (j == 0)
                            {
                                //set parent to null to keep right order 
                                buttons[i][j].transform.SetParent(null);
                                buttons[i][j].SetActive(false);
                                buttons[i][j].transform.SetParent(menuPanelVertical);
                            }
                            else
                            {
                                buttons[i][j].SetActive(false);
                            }
                    }
                }
                // Finally set the selected button active.
                for (int k = 0; k < buttons[ClickedIndex].Length; k++)
                {
                    //set parent to null to keep right order 
                    buttons[ClickedIndex][k].transform.SetParent(null);
                    buttons[ClickedIndex][k].transform.SetParent(menuPanelHorizontal);
                    buttons[ClickedIndex][k].SetActive(true);
                }
                MobileMenuShown = false;
            }
            else
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i][0].SetActive(true);
                }
                MobileMenuShown = true;
            }
            // Set node interaction buttons back to white
            if (ClickedIndex != 2)
            {
                for (int i = 0; i < buttons[2].Length; i++)
                {
                    Button button = buttons[2][i].GetComponent<Button>();
                    MarkButtonInactive(button);
                }
            }
        }

        /// <summary>
        /// Expands/minimizes the quick menu on the left top side 
        /// </summary>
        /// <param name="left">Arrow Sprite to the left</param>
        /// <param name="right">Arrow Sprite to the right</param>
        private void ExpandButton(Sprite left, Sprite right)
        {
            for (int i = 0; i < quickButtons.Length - 1; ++i)
            {
                if (QuickMenuShown)
                {
                    quickButtons[i].SetActive(false);
                }
                else
                {
                    quickButtons[i].SetActive(true);
                }
            }
            if (QuickMenuShown)
            {
                quickButtons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = right;
                QuickMenuShown = false;
            }
            else
            {
                quickButtons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = left;
                QuickMenuShown = true;
            }
        }

        /// <summary>
        /// Triggers the Redo Action
        /// </summary>
        private void TriggerRedo()
        {
            GlobalActionHistory.Redo();
        }

        /// <summary>
        /// Triggers the Undo Action
        /// </summary>
        private void TriggerUndo()
        {
            GlobalActionHistory.Undo();

            if (GlobalActionHistory.IsEmpty())
            {
                // We always want to have an action running.
                // The default action will be the first action state type.
                GlobalActionHistory.Execute(ActionStateType.MobileMenuTypes.First());
            }
        }

        /// <summary>
        /// Unselects all objects
        /// </summary>
        private void Deselect()
        {
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Activates/deactivates snap mode for rotation mode.
        /// </summary>
        private void ToggleSnapMode()
        {
            SEEInput.SnapMobile = !SEEInput.SnapMobile;
            
            if (SEEInput.SnapMobile)
            {
                Button snapButton = buttons[3][1].GetComponent<Button>();
                MarkButtonActive(snapButton);
            }
            else
            {
                buttons[3][1].GetComponent<Button>().colors = ColorBlock.defaultColorBlock;
            }
        }

        /// <summary>
        /// Toggles move mode, whether the hole city shall be moved or just the touched object.
        /// </summary>
        private void ToggleMoveMode()
        {
            SEEInput.DragTouched = !SEEInput.DragTouched;

            if (SEEInput.DragTouched)
            {
                Button dragButton = buttons[4][1].GetComponent<Button>();
                MarkButtonActive(dragButton);
            }
            else
            {
                buttons[4][1].GetComponent<Button>().colors = ColorBlock.defaultColorBlock;
            }
        }

        /// <summary>
        /// Toggles the mode how nodes/cities are moved from free mode to eight-directions and back.
        /// </summary>
        private void ToggleEightDirectionMode()
        {
            SEEInput.EightDirectionMode = !SEEInput.EightDirectionMode;

            if (SEEInput.EightDirectionMode)
            {
                Button eightButton = buttons[4][2].GetComponent<Button>();
                MarkButtonActive(eightButton);
            }
            else
            {
                buttons[4][2].GetComponent<Button>().colors = ColorBlock.defaultColorBlock;
            }
        }

        /// <summary>
        /// Marks an button green to signalize that its active
        /// </summary>
        /// <param name="button"></param>
        private void MarkButtonActive(Button button)
        {
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = Color.green;
            button.colors = colorBlock;
        }

        /// <summary>
        /// Marks an button white to signalize that its inactive
        /// </summary>
        /// <param name="button"></param>
        private void MarkButtonInactive(Button button)
        {
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = Color.white;
            button.colors = colorBlock;
        }

        /// <summary>
        /// The method marks the selected button green and the other ones white
        /// </summary>
        /// <param name="buttonNr"></param>
        private void ShowNodeActiveNodeInteraction(int buttonLine, int buttonNr)
        {
            for (int i = 0; i < buttons[2].Length; i++)
            {
                if (i == buttonNr)
                {
                    Button button = buttons[buttonLine][i].GetComponent<Button>();
                    MarkButtonActive(button);
                }
                else
                {
                    Button button = buttons[buttonLine][i].GetComponent<Button>();
                    MarkButtonInactive(button);
                }
            }
        }

    }
}