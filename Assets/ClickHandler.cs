using DynamicPanels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClickHandler : MonoBehaviour
{
    public GameObject woman;
    public GameObject man;
    public Button womanButton;
    public Button manButton;
    public Button hairL;
    public Button hairR;
    public Button skinL;
    public Button SkinR;
    public Button topL;
    public Button topR;
    public Button bottomL;
    public Button bottomR;
    public Button shoeL;
    public Button shoeR;
    public Button apply;
    public GameObject manHair1;
    public GameObject manHair2;
    public GameObject[] hairObjects;
    public GameObject[] manTops;
    public GameObject[] manBottoms;
    public GameObject[] manShoes;
    public GameObject[] womanTops;
    public GameObject[] womanBottoms;
    public GameObject[] womanShoes;
    public Texture womanBodyTexture1;
    public Texture womanBodyTexture2;
    public Texture womanArmTexture1;
    public Texture womanArmTexture2;
    public Texture womanLegTexture1;
    public Texture womanLegTexture2;
    public Texture womanHeadTexture1;
    public Texture womanHeadTexture2;
    public Texture manBodyTexture1;
    public Texture manBodyTexture2;
    public Texture manArmTexture1;
    public Texture manArmTexture2;
    public Texture manLegTexture1;
    public Texture manLegTexture2;
    public Texture manHeadTexture1;
    public Texture manHeadTexture2;
    public Material womanBody;
    public Material womanArm;
    public Material womanLeg;
    public Material womanHead;
    public Material manBody;
    public Material manArm;
    public Material manLeg;
    public Material manHead;


    private int currentIndex = 0;
    private int currentManTopIndex = 0;
    private int currentWomanTopIndex = 0;
    private int currentManBottomIndex = 0;
    private int currentWomanBottomIndex = 0;
    private int currentManShoeIndex = 0;
    private int currentWomanShoeIndex = 0;
    private int currentWomanSkin = 1;
    private int currentManSkin = 1;

    private int loadWomanIndex = 0;
    private int loadWomanHairIndex = 0;
    private int loadWomanTopIndex = 0;
    private int loadWomanBottomIndex = 0;
    private int loadWomanShoeIndex = 0;
    private int loadManIndex = 0;
    private int loadManHairIndex = 0;
    private int loadManTopIndex = 0;
    private int loadManBottomIndex = 0;
    private int loadManShoeIndex = 0;

    void Start()
    {
        if (loadWomanIndex == 1)
        {
            woman.SetActive(true);
            man.SetActive(false);
            foreach (GameObject hairObject in hairObjects)
            {
               hairObject.SetActive(false);
            }
            hairObjects[loadWomanHairIndex].SetActive(true);
            foreach (GameObject womanTop in womanTops)
            {
                womanTop.SetActive(false);
            }
            womanTops[loadWomanTopIndex].SetActive(true);
            foreach (GameObject womanBottom in womanBottoms)
            {
                womanBottom.SetActive(false);
            }
            womanBottoms[loadWomanBottomIndex].SetActive(true);
            foreach (GameObject womanShoe in womanShoes)
            {
                womanShoe.SetActive(false);
            }
            womanShoes[loadWomanShoeIndex].SetActive(true);
        } else
        {
            woman.SetActive(false);
            man.SetActive(true);
            if (loadManHairIndex == 1)
            {
                manHair1.SetActive(true);
                manHair2.SetActive(true);
            } else
            {
                manHair1.SetActive(false);
                manHair2.SetActive(false);
            }
            foreach (GameObject manTop in manTops)
            {
                manTop.SetActive(false);
            }
            manTops[loadManTopIndex].SetActive(true);
            foreach (GameObject manBottom in manBottoms)
            {
                manBottom.SetActive(false);
            }
            manBottoms[loadManBottomIndex].SetActive(true);
            foreach (GameObject manShoe in manShoes)
            {
                manShoe.SetActive(false);
            }
            manShoes[loadManShoeIndex].SetActive(true);
        }

        womanButton.onClick.AddListener(WomanButtonClicked);
        manButton.onClick.AddListener(ManButtonClicked);
        hairL.onClick.AddListener(ChangeHairLeft);
        hairR.onClick.AddListener(ChangeHairRight);
        topL.onClick.AddListener(ChangeTopLeft);
        topR.onClick.AddListener(ChangeTopRight);
        bottomL.onClick.AddListener(ChangeBottomLeft);
        bottomR.onClick.AddListener(ChangeBottomRight);
        shoeL.onClick.AddListener(ChangeShoeLeft);
        shoeR.onClick.AddListener(ChangeShoeRight);
        skinL.onClick.AddListener(ChangeSkin);
        SkinR.onClick.AddListener(ChangeSkin);
        apply.onClick.AddListener(ApplyChanges);
        // Finde das aktive Haar-GameObject und speichere den Index
        for (int i = 0; i < hairObjects.Length; i++)
        {
            if (hairObjects[i].activeSelf)
            {
                currentIndex = i;
                break;
            }
        }

        // Finde das aktive ManTop-GameObject und speichere den Index
        for (int i = 0; i < manTops.Length; i++)
        {
            if (manTops[i].activeSelf)
            {
                currentManTopIndex = i;
                break;
            }
        }

        // Finde das aktive WomanTop-GameObject und speichere den Index
        for (int i = 0; i < womanTops.Length; i++)
        {
            if (womanTops[i].activeSelf)
            {
                currentWomanTopIndex = i;
                break;
            }
        }

        // Finde das aktive ManBottom-GameObject und speichere den Index
        for (int i = 0; i < manBottoms.Length; i++)
        {
            if (manBottoms[i].activeSelf)
            {
                currentManBottomIndex = i;
                break;
            }
        }

        // Finde das aktive WomanBottom-GameObject und speichere den Index
        for (int i = 0; i < womanBottoms.Length; i++)
        {
            if (womanBottoms[i].activeSelf)
            {
                currentWomanBottomIndex = i;
                break;
            }
        }

        // Finde das aktive ManShoe-GameObject und speichere den Index
        for (int i = 0; i < manShoes.Length; i++)
        {
            if (manShoes[i].activeSelf)
            {
                currentManShoeIndex = i;
                break;
            }
        }

        // Finde das aktive WomanShoe-GameObject und speichere den Index
        for (int i = 0; i < womanShoes.Length; i++)
        {
            if (womanShoes[i].activeSelf)
            {
                currentWomanShoeIndex = i;
                break;
            }
        }
        currentManSkin = 1;
        currentWomanSkin = 1;
       
    }



    void WomanButtonClicked()
    {
        woman.SetActive(true);
        man.SetActive(false);
    }

    void ManButtonClicked()
    {
        woman.SetActive(false);
        man.SetActive(true);
    }

    void ChangeHairLeft()
    {
        ChangeHair("l");
    }

    void ChangeHairRight()
    {
        ChangeHair("r");
    }

    void ChangeTopLeft()
    {
        ChangeTop("l");
    }

    void ChangeTopRight()
    {
        ChangeTop("r");
    }

    void ChangeBottomLeft()
    {
        ChangeBottom("l");
    }

    void ChangeBottomRight()
    {
        ChangeBottom("r");
    }

    void ChangeShoeLeft()
    {
        ChangeShoe("l");
    }

    void ChangeShoeRight()
    {
        ChangeShoe("r");
    }

    void ChangeHair(string direction)
    {
        if (man.activeSelf)
        {
           if (!manHair1.activeSelf)
            {
                manHair1.SetActive(true);
                manHair2.SetActive(true);
            }
            else
            {
                {
                    manHair1.SetActive(false);
                    manHair2.SetActive(false);
                }
            }
        } else
        {
           if (direction == "l")
            {
                hairObjects[currentIndex].SetActive(false);
                currentIndex = (currentIndex - 1 + hairObjects.Length) % hairObjects.Length;
                hairObjects[currentIndex].SetActive(true);
            } else
            {
                hairObjects[currentIndex].SetActive(false);
                currentIndex = (currentIndex + 1) % hairObjects.Length;
                hairObjects[currentIndex].SetActive(true);
            }
        }
    }

    void ChangeTop(string direction)
    {
        if (man.activeSelf)
        {
            if (direction == "l")
            {
                manTops[currentManTopIndex].SetActive(false);
                currentManTopIndex = (currentManTopIndex - 1 + manTops.Length) % manTops.Length;
                manTops[currentManTopIndex].SetActive(true);
            }
            else
            {
                manTops[currentManTopIndex].SetActive(false);
                currentManTopIndex = (currentManTopIndex + 1) % manTops.Length;
                manTops[currentManTopIndex].SetActive(true);
            }
        }
        else
        {
            if (direction == "l")
            {
                womanTops[currentWomanTopIndex].SetActive(false);
                currentWomanTopIndex = (currentWomanTopIndex - 1 + womanTops.Length) % womanTops.Length;
                womanTops[currentWomanTopIndex].SetActive(true);
            }
            else
            {
                womanTops[currentWomanTopIndex].SetActive(false);
                currentWomanTopIndex = (currentWomanTopIndex + 1) % womanTops.Length;
                womanTops[currentWomanTopIndex].SetActive(true);
            }
        }
    }

    void ChangeBottom(string direction)
    {
        if (man.activeSelf)
        {
            if (direction == "l")
            {
                manBottoms[currentManBottomIndex].SetActive(false);
                currentManBottomIndex = (currentManBottomIndex - 1 + manBottoms.Length) % manBottoms.Length;
                manBottoms[currentManBottomIndex].SetActive(true);
            }
            else
            {
                manBottoms[currentManBottomIndex].SetActive(false);
                currentManBottomIndex = (currentManBottomIndex + 1) % manBottoms.Length;
                manBottoms[currentManBottomIndex].SetActive(true);
            }
        }
        else
        {
            if (direction == "l")
            {
                womanBottoms[currentWomanBottomIndex].SetActive(false);
                currentWomanBottomIndex = (currentWomanBottomIndex - 1 + womanBottoms.Length) % womanBottoms.Length;
                womanBottoms[currentWomanBottomIndex].SetActive(true);
            }
            else
            {
                womanBottoms[currentWomanBottomIndex].SetActive(false);
                currentWomanBottomIndex = (currentWomanBottomIndex + 1) % womanBottoms.Length;
                womanBottoms[currentWomanBottomIndex].SetActive(true);
            }
        }
    }

    void ChangeShoe(string direction)
    {
        if (man.activeSelf)
        {
            if (direction == "l")
            {
                manShoes[currentManShoeIndex].SetActive(false);
                currentManShoeIndex = (currentManShoeIndex - 1 + manShoes.Length) % manShoes.Length;
                manShoes[currentManShoeIndex].SetActive(true);
            }
            else
            {
                manShoes[currentManShoeIndex].SetActive(false);
                currentManShoeIndex = (currentManShoeIndex + 1) % manShoes.Length;
                manShoes[currentManShoeIndex].SetActive(true);
            }
        }
        else
        {
            if (direction == "l")
            {
                womanShoes[currentWomanShoeIndex].SetActive(false);
                currentWomanShoeIndex = (currentWomanShoeIndex - 1 + womanShoes.Length) % womanShoes.Length;
                womanShoes[currentWomanShoeIndex].SetActive(true);
            }
            else
            {
                womanShoes[currentWomanShoeIndex].SetActive(false);
                currentWomanShoeIndex = (currentWomanShoeIndex + 1) % womanShoes.Length;
                womanShoes[currentWomanShoeIndex].SetActive(true);
            }
        }
    }


    void ChangeSkin()
    {
     
        if (man.activeSelf)
        {
            if (currentManSkin == 1)
            {
                manBody.mainTexture = manBodyTexture2;
                manArm.mainTexture = manArmTexture2;
                manLeg.mainTexture = manLegTexture2;
                manHead.mainTexture = manHeadTexture2;
                currentManSkin = 2;
            } else
            {
                manBody.mainTexture = manBodyTexture1;
                manArm.mainTexture = manArmTexture1;
                manLeg.mainTexture = manLegTexture1;
                manHead.mainTexture = manHeadTexture1;
                currentManSkin = 1;
            }

        } else
        {
            if (currentWomanSkin == 1)
            {
                womanBody.mainTexture = womanBodyTexture2;
                womanArm.mainTexture = womanArmTexture2;
                womanLeg.mainTexture = womanLegTexture2;
                womanHead.mainTexture = womanHeadTexture2;
                currentWomanSkin = 2;
            }
            else
            {
                womanBody.mainTexture = womanBodyTexture1;
                womanArm.mainTexture = womanArmTexture1;
                womanLeg.mainTexture = womanLegTexture1;
                womanHead.mainTexture = womanHeadTexture1;
                currentWomanSkin = 1;
            }
        }
    }

    void ApplyChanges()
    {
        if (man.activeSelf)
        {
            loadManIndex = 1;
            loadWomanIndex = 0;
            if (manHair1.activeSelf)
            {
                loadManHairIndex = 1;
            } else
            {
                loadManHairIndex = 0;
            }
            loadManTopIndex = currentManTopIndex;
            loadManBottomIndex = currentManBottomIndex;
            loadManShoeIndex = currentManShoeIndex;
        } else
        {
            loadManIndex = 0;
            loadWomanIndex = 1;
            loadWomanHairIndex = currentIndex;
            loadWomanTopIndex = currentWomanTopIndex;
            loadWomanBottomIndex = currentWomanBottomIndex;
            loadWomanShoeIndex = currentWomanShoeIndex;
        }

        if (UnityEditor.EditorApplication.isPlaying == false)
        {
            Application.Quit();
        }
        else
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    
}

}
