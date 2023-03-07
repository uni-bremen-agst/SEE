using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo {
public class HandSwapper : MonoBehaviour{
        public AutoHandPlayer player;
        public Hand fromHand;
        public Hand toHand;
        public GameObject fromModel;
        public GameObject toModel;

        bool swapped;
        public void Swap() {
            if(!swapped){
                if (toHand.left)
                    player.handLeft = toHand;
                else
                    player.handRight = toHand;

                fromHand.gameObject.SetActive(false);
                fromModel.gameObject.SetActive(true);
                toHand.gameObject.SetActive(true);
                toModel.gameObject.SetActive(false);
            }
            else { 
                if (fromHand.left)
                    player.handLeft = fromHand;
                else
                    player.handRight = fromHand;

                fromHand.gameObject.SetActive(true); 
                fromModel.gameObject.SetActive(false);
                toHand.gameObject.SetActive(false);
                toModel.gameObject.SetActive(true);

            }
            swapped = !swapped;
        }
    }
}