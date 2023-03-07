using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo {

    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/controller-input")]
    public class XRHandPlayerControllerLink : MonoBehaviour{
        public XRHandControllerLink moveController;
        public XRHandControllerLink turnController;
        public AutoHandPlayer player;

        [Header("Input")]
        public Common2DAxis moveAxis;
        public Common2DAxis turnAxis;

        
        void Update(){
            player.Move(moveController.GetAxis2D(moveAxis));
            player.Turn(turnController.GetAxis2D(turnAxis).x);
        }
        void FixedUpdate(){
            player.Move(moveController.GetAxis2D(moveAxis));
        }
    }
}