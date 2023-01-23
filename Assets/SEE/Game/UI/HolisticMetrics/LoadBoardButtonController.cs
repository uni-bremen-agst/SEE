using UnityEngine;

namespace SEE.Game.UI.HolisticMetrics
{
    public class LoadBoardButtonController : MonoBehaviour
    {
        private bool gotClick;
        
        public void OnClick()
        {
            gotClick = true;
        }

        internal bool GetClick()
        {
            if (gotClick)
            {
                gotClick = false;
                return true;
            }

            return false;
        }
    }
}
