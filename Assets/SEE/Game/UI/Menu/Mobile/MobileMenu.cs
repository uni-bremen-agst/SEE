using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using UnityEngine.Events;
using UnityEditor;
using SEE.Utils;

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
        private const string MOBLIE_MENU_PREFAB = "Prefabs/UI/MobileMenu";

        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added for each menu entry in <see cref="entries"/>.
        /// </summary>
        private const string ICON_BUTTON_PREFAB = "Prefabs/UI/IconButton";

        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added for each menu entry in <see cref="entries"/>.
        /// </summary>
        private const string TEXT_BUTTON_PREFAB = "Prefabs/UI/TextButton";

        /// <summary>
        /// The GameObject which has the three panels attached
        /// </summary>
        private GameObject MobileMenuGameObject;

        private GameObject[][] buttons = new GameObject[5][];

        private bool expanded = false;

        protected override void StartMobile()
        {
            initialiseMobileMenu(); 
        }

        /// <summary>
        /// Initialises the mobile Menu with all the icon buttons
        /// </summary>
        protected void initialiseMobileMenu()
        {
            #region set up buttons
            GameObject[] buttonSelect = new GameObject[2];
            GameObject[] buttonDelete = new GameObject[1];
            GameObject[] buttonDeleteMulti = new GameObject[3];
            GameObject[] buttonRotate = new GameObject[5];
            GameObject[] buttonMove = new GameObject[4];

            MobileMenuGameObject = PrefabInstantiator.InstantiatePrefab(MOBLIE_MENU_PREFAB, Canvas.transform, false);
            GameObject iconButton = PrefabInstantiator.InstantiatePrefab(ICON_BUTTON_PREFAB, EntryList.transform, false);
            GameObject textButton = PrefabInstantiator.InstantiatePrefab(TEXT_BUTTON_PREFAB, EntryList.transform, false);



            Sprite refreshSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Refresh.png", typeof(Sprite));
            Sprite checkSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Check Bold.png", typeof(Sprite));
            Sprite cancelSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Cancel.png", typeof(Sprite));
            Sprite trashSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Common/Trash.png", typeof(Sprite));
            Sprite minusSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Minus.png", typeof(Sprite));
            Sprite moveSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Demo/Icons/Horizontal Selector.png", typeof(Sprite));

            buttonSelect[0] = iconButton;
            buttonSelect[1] = iconButton;
            buttonSelect[1].GetComponent<ButtonManagerBasicIcon>().buttonIcon = cancelSprite;

            buttonDelete[0] = iconButton;
            buttonDelete[0].GetComponent<ButtonManagerBasicIcon>().buttonIcon = trashSprite;

            buttonDeleteMulti[0] = iconButton;
            buttonDeleteMulti[0].GetComponent<ButtonManagerBasicIcon>().buttonIcon = minusSprite;
            buttonDeleteMulti[1] = iconButton;
            buttonDeleteMulti[1].GetComponent<ButtonManagerBasicIcon>().buttonIcon = cancelSprite;
            buttonDeleteMulti[2] = iconButton;
            buttonDeleteMulti[2].GetComponent<ButtonManagerBasicIcon>().buttonIcon = checkSprite;

            buttonRotate[0] = iconButton;
            buttonRotate[0].GetComponent<ButtonManagerBasicIcon>().buttonIcon = refreshSprite;
            buttonRotate[1] = textButton;
            buttonRotate[1].GetComponent<ButtonManagerBasic>().buttonText = "n";
            buttonRotate[2] = textButton;
            buttonRotate[3] = iconButton;
            buttonRotate[4] = iconButton;

            buttonMove[0] = iconButton;
            buttonMove[0].GetComponent <ButtonManagerBasicIcon>().buttonIcon= moveSprite;
            buttonMove[1] = textButton;
            buttonMove[1].GetComponent<ButtonManagerBasic>().buttonText = "n";
            buttonMove[2] = textButton;
            buttonMove[3] = textButton;
            buttonMove[3].GetComponent<ButtonManagerBasic>().buttonText = "8";

            buttons[0] = buttonSelect;
            buttons[1] = buttonDelete;
            buttons[2] = buttonDeleteMulti;
            buttons[3] = buttonRotate;
            buttons[4] = buttonMove;



            //initially set all buttons but the first ones inactive
            for (int i = 1; i < buttons.Length; i++)
            {
                for (int j = 0; j < buttons[i].Length; j++)
                {
                    buttons[i][j].SetActive(false);
                }
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                //add listener to expand menu
                int clickedIndex = i;
                buttons[i][0].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => selectMode(clickedIndex));
                for (int j = 0; j < buttons[i].Length; j++)
                {
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
            #endregion
        }

        protected void addButtons(IEnumerable<T> buttonEntries)
        {
            for(int i = 0; i >)
        }

        private void selectMode(int ClickedIndex)
        {
            if (expanded)
            {
                //set inactive first for right order 
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
                //finally set the selected button active
                for (int k = 0; k < buttons[ClickedIndex].Length; k++)
                {
                    //set parent to null to keep right order 
                    buttons[ClickedIndex][k].transform.SetParent(null);
                    buttons[ClickedIndex][k].transform.SetParent(menuPanelHorizontal);
                    buttons[ClickedIndex][k].SetActive(true);
                }
            expanded = false;
            }
            else
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i][0].SetActive(true);
                }
                expanded = true;
            }
        }

    }
}