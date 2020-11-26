using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MixedRealityGameObjectInteractions : MonoBehaviour, IMixedRealityPointerHandler
{
    /// <summary>
    /// Custom MixedReality pointed action for current game object
    /// </summary>
    private Action<GameObject> MixedRealityPointedAction;

    /// <summary>
    /// AppBar showing when pointed
    /// </summary>
    private GameObject AppBar;

    BoundsControl BoundsControl;

    public bool CreateAppBar = false;
    private AppBar AppBarComponent;
    private bool ShowAppBar = true;

    // Start is called before the first frame update
    void Start()
    {
        AppBar = GameObject.Find("MainAppBar");

        if(AppBar == null)
        {
            Debug.Log("No AppBar found in current scene");

            if(CreateAppBar)
            {
                GameObject appBar = new GameObject("MainAppBar", new Type[] { typeof(AppBar) });
                appBar.transform.parent = null;
                AppBar = appBar;

                Debug.Log($"A new AppBar {AppBar.name} was created and added to root into the current scene");
            }
            else
            {
                Debug.Log($"No AppBar was created because of the Property {nameof(CreateAppBar)}:{CreateAppBar.ToString()}");
            }
        }
        else
        {
            if (!AppBar.TryGetComponent<AppBar>(out AppBarComponent))
            {
                AppBar.AddComponent<AppBar>();
                AppBarComponent = AppBar.GetComponent<AppBar>();
                Debug.Log($"A AppBar component was added to the GameObject {AppBar.name}!");

            }
        }

        if (!gameObject.TryGetComponent<BoundsControl>(out BoundsControl))
        {
            gameObject.AddComponent<BoundsControl>();
            BoundsControl = gameObject.GetComponent<BoundsControl>();
            Debug.Log($"A BoundsControl was added to the GameObject {gameObject.name}!");
            BoundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateByPointer;

        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// Changes the default action (showing the MixedReality AppBar) for this game object when it were pointed 
    /// </summary>
    /// <param name="SelectionAction">
    /// A method handling the action when the GameObject was pointed where the GameObject in parameter is the pointed object
    /// </param>
    public void ChangeStandardPointedAction(Action<GameObject> SelectionAction, bool showAppBar = true)
    {
        MixedRealityPointedAction = SelectionAction;
        ShowAppBar = showAppBar;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        PerfromeGameObjectPointedAction();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
     {
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {

    }

    /// <summary>
    /// Action when GameObject were pointed
    /// </summary>
    private void PerfromeGameObjectPointedAction()
    {
        if (MixedRealityPointedAction != null)
        {
            if (ShowAppBar)
            {
                ShowGameObjectAppBar();
            }

            MixedRealityPointedAction(gameObject);
            Debug.Log($"Pointed on {gameObject.name}");
        }
        else
        {
            Debug.Log($"Pointed on {gameObject.name}");
            ShowGameObjectAppBar();
        }
    }

    private void ShowGameObjectAppBar()
    {
        AppBarComponent.Target = BoundsControl;
    }
}
