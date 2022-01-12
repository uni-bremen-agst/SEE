using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Responsible for the mobile UI for menus.
    /// </summary>
    public class MobileMenu : MonoBehaviour
    {
        /// <summary>
        /// Prefab for the buttons
        /// </summary>
        [SerializeField] GameObject buttonPrefab;

        [SerializeField] Transform menuPanelHorizontal;

        [SerializeField] Transform menuPanelVertical;

        private GameObject[][] buttons = new GameObject[5][];

        private GameObject[] buttonSelect = new GameObject[2];

        private GameObject[] buttonDelete = new GameObject[1];

        private GameObject[] buttonDeleteMulti = new GameObject[3];

        private GameObject[] buttonRotate = new GameObject[5];

        private GameObject[] buttonMove = new GameObject[4];


        // Start is called before the first frame update
        void Start()
        {
            buttonSelect[0] = (GameObject)Instantiate (buttonPrefab);
            buttonSelect[1] = (GameObject)Instantiate (buttonPrefab);

            buttonDelete[0] = (GameObject)Instantiate (buttonPrefab);

            buttonDeleteMulti[0] = (GameObject)Instantiate(buttonPrefab);
            buttonDeleteMulti[1] = (GameObject)Instantiate(buttonPrefab);
            buttonDeleteMulti[2] = (GameObject)Instantiate(buttonPrefab);

            buttonRotate[0] = (GameObject)Instantiate(buttonPrefab);
            buttonRotate[1] = (GameObject)Instantiate(buttonPrefab);
            buttonRotate[2] = (GameObject)Instantiate(buttonPrefab);
            buttonRotate[3] = (GameObject)Instantiate(buttonPrefab);
            buttonRotate[4] = (GameObject)Instantiate(buttonPrefab);

            buttonMove[0] = (GameObject)Instantiate(buttonPrefab);
            buttonMove[1] = (GameObject)Instantiate(buttonPrefab);
            buttonMove[2] = (GameObject)Instantiate(buttonPrefab);
            buttonMove[3] = (GameObject)Instantiate(buttonPrefab);

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
                for (int j = 0; j < buttons[i].Length; j++)
                {
                    if (j == 0)
                    {
                        buttons[i][j].transform.SetParent(menuPanelVertical);
                        //add listener to expand menu
                        buttons[i][j].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => CollapseMenu());
                        int clickedIndex = i;
                        buttons[i][j].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => SortButtons(clickedIndex));
                    }
                    else
                    {
                        buttons[i][j].transform.SetParent(menuPanelHorizontal);
                    }
                }       
            }
            Debug.Log(buttons);
        }

        private void SortButtons(int ClickedIndex)
        {
            for (int i = 1; i < buttons.Length; i++)
            {
                if (i == ClickedIndex)
                {
                    GameObject[] tmpButtons = new GameObject[buttons[i].Length];
                    tmpButtons = buttons[i];
                    buttons[i] = buttons[0];
                    buttons[0] = tmpButtons;

                    for (int j = 1; j < buttons[i].Length; j++)
                    {
                        buttons[i][j].SetActive(false);
                    }
                    for (int j = 1;j < buttons[0].Length; j++)
                    {
                        buttons[0][j].SetActive(true);
                    }
                }
            }
        }

        private void CollapseMenu()
        {
            if (buttons[1][0].activeSelf == true)
            {
                for (int i = 1; i < buttons.Length; i++)
                {
                    buttons[i][0].SetActive(false);
                }
            }
            else
            {
                for (int i = 1; i < buttons.Length; i++)
                {
                    buttons[i][0].SetActive(true);
                }
            }
            
        }

    }
}
