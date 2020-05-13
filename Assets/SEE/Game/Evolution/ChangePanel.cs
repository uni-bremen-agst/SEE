using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.Evolution
{
    public class ChangePanel : MonoBehaviour
    {
        /// <summary>
        /// Unity Event that gets invoked when the panel, this script is attached to, closes.
        /// </summary>
        public UnityEvent OnPanelClose;

        private void Start()
        {
            if (OnPanelClose == null)
            {
                OnPanelClose = new UnityEvent();
            }
        }

        /// <summary>
        /// Opens the given panel by using its animator component.
        /// Also closes the panel this script is attached to.
        /// </summary>
        /// <param name="panel">the panel which should be opened</param>
        public void OpenOtherPanel(GameObject panel)
        {
            Animator currentPanelAnimator = gameObject.GetComponent<Animator>();
            Animator newPanelAnimator = panel.GetComponent<Animator>();

            if (currentPanelAnimator != null && newPanelAnimator != null)
            {
                currentPanelAnimator.SetBool("isOpen", !currentPanelAnimator.GetBool("isOpen"));
                newPanelAnimator.SetBool("isOpen", !newPanelAnimator.GetBool("isOpen"));
            }

            OnPanelClose.Invoke();
        }
    }
}