using System;
using Autohand;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;


// FIXME : Must be deleted later, for test purposes only
namespace SEE.Controls.Actions
{
    public class TestScript : MonoBehaviour
    {
        public void Start()
        {
            GlobalActionHistory.Execute(ActionStateType.Move);
        }

        public void Update()
        {
            GlobalActionHistory.Update();
        }
    }
}