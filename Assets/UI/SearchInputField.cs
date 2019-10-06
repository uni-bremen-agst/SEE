using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SearchInputField : MonoBehaviour
{
    public GameObject ScrollView;
    public ContentManager Content;
    public InputField SearchField;

    private bool IsSelected;

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            if (!IsSelected)
            {
                ScrollView.SetActive(true);
            }
            IsSelected = true;
        }
        else
        {
            if (IsSelected)
            {
                // TODO properly deactivate if click back into game etc
            }
            IsSelected = false;
        }
    }

    public void OnValueChanged()
    {
        Content.Filter(SearchField.text);
    }
}
