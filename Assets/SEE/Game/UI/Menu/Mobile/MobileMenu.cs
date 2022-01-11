using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;

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

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i][0].transform.parent = menuPanelVertical;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
