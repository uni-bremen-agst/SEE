using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using System;

public class QuickMenu : MonoBehaviour
{
    /// <summary>
    /// Prefab for the buttons
    /// </summary>
    [SerializeField] GameObject iconButtonPrefab;

    [SerializeField] Transform menuPanelHorizontal;

    private GameObject[] buttons = new GameObject[6];

    private bool expanded = false;

    // Start is called before the first frame update
    void Start()
    {
        #region set up buttons

        Sprite redoSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Refresh.png", typeof(Sprite));
        Sprite undoSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Refresh.png", typeof(Sprite));
        Sprite cameraLockSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Common/Lock.png", typeof(Sprite));
        Sprite cameraUnlockSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Common/Lock Open.png", typeof(Sprite));
        Sprite rerotateSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Refresh.png", typeof(Sprite));
        Sprite recenterSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Demo/Icons/Button.png", typeof(Sprite));
        Sprite arrowLeftSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Modern UI Pack/Textures/Icon/Navigation/Arrow Bold.png", typeof(Sprite));
        Sprite arrowRightSprite = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/Icons/Arrow Bold Right.png", typeof(Sprite));

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = (GameObject)Instantiate(iconButtonPrefab);
        }

        buttons[0].GetComponent<ButtonManagerBasicIcon>().buttonIcon = redoSprite;
        buttons[1].GetComponent<ButtonManagerBasicIcon>().buttonIcon = undoSprite;
        buttons[2].GetComponent<ButtonManagerBasicIcon>().buttonIcon = cameraUnlockSprite;
        buttons[3].GetComponent<ButtonManagerBasicIcon>().buttonIcon = rerotateSprite;
        buttons[4].GetComponent<ButtonManagerBasicIcon>().buttonIcon = recenterSprite;
        buttons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = arrowLeftSprite;

        for (int i = 0;i < buttons.Length;i++)
        {
            buttons[i].transform.SetParent(menuPanelHorizontal);
            buttons[i].SetActive(false);
        }

        buttons[5].SetActive(true);
        buttons[5].GetComponent<ButtonManagerBasicIcon>().clickEvent.AddListener(() => expandButton(arrowLeftSprite, arrowRightSprite));
        #endregion
    }

    private void expandButton(Sprite left, Sprite right)
    {
        for (int i = 0; i < buttons.Length -1; ++i)
        {
            if(expanded)
            {
                buttons[i].SetActive(false);
            }
            else
            {
                buttons[i].SetActive(true);
            }
        }
        if (expanded)
        {
            buttons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = right;
            expanded = false;
        }
        else
        {
            buttons[5].GetComponent<ButtonManagerBasicIcon>().buttonIcon = left;
            expanded = true;
        }
    }
}
