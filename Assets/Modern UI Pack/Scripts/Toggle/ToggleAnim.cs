using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    public class ToggleAnim : MonoBehaviour
    {
        [HideInInspector] public Toggle toggleObject;
        [HideInInspector] public Animator toggleAnimator;
        bool isInitalized = false;

        void Start()
        {
            if (toggleObject == null)
                toggleObject = gameObject.GetComponent<Toggle>();

            toggleAnimator = toggleObject.GetComponent<Animator>();
            toggleObject.onValueChanged.AddListener(TaskOnClick);
            CheckForState();
            isInitalized = true;
        }

        void OnEnable()
        {
            if (isInitalized == true)
                CheckForState();
        }

        void CheckForState()
        {
            if (toggleObject.isOn)
                toggleAnimator.Play("Toggle On");
            else
                toggleAnimator.Play("Toggle Off");
        }

        void TaskOnClick(bool value)
        {
            if (toggleObject.isOn)
                toggleAnimator.Play("Toggle On");
            else
                toggleAnimator.Play("Toggle Off");
        }
    }
}