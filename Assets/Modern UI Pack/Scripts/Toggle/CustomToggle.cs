using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    [RequireComponent(typeof(Toggle))]
    [RequireComponent(typeof(Animator))]
    public class CustomToggle : MonoBehaviour
    {
        [HideInInspector] public Toggle toggleObject;
        [HideInInspector] public Animator toggleAnimator;

        void Start()
        {
            if (toggleObject == null)
                toggleObject = gameObject.GetComponent<Toggle>();
           
            if (toggleAnimator == null)
                toggleAnimator = toggleObject.GetComponent<Animator>();

            toggleObject.onValueChanged.AddListener(UpdateStateDynamic);
            UpdateState();
        }

        void OnEnable()
        {
            if (toggleObject == null)
                return;

            UpdateState();
        }

        public void UpdateState()
        {
            if (toggleObject.isOn)
                toggleAnimator.Play("Toggle On");
            else
                toggleAnimator.Play("Toggle Off");
        }

        void UpdateStateDynamic(bool value)
        {
            if (toggleObject.isOn)
                toggleAnimator.Play("Toggle On");
            else
                toggleAnimator.Play("Toggle Off");
        }
    }
}