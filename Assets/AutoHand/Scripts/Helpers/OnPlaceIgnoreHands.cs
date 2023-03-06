using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public class OnPlaceIgnoreHands : MonoBehaviour {

        public PlacePoint placePoint;
        public Hand[] ignoreHands;

        void OnEnable() {
            placePoint.OnPlaceEvent += OnPlace;
            placePoint.OnRemoveEvent += OnRemove;
        }


        void OnDisable() {
            placePoint.OnPlaceEvent -= OnPlace;
            placePoint.OnRemoveEvent -= OnRemove;
        }

        void OnPlace(PlacePoint point, Grabbable grab) {
            foreach(var hand in ignoreHands)
                grab.IgnoreHand(hand, true, true);
        }

        void OnRemove(PlacePoint point, Grabbable grab) {
            foreach(var hand in ignoreHands) 
                grab.IgnoreHand(hand, false, true);
        }
    }
}