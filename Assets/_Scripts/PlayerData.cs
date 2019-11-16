using UnityEngine;

namespace SEE
{

    public static class PlayerData
    {
        public static GameObject playerHead = InitializePlayer();
        public static GameObject PlayerHead
        {
            get
            {
                return playerHead;
            }
            private set
            {
                playerHead = value;
            }
        }

        public static GameObject InitializePlayer()
        {
            GameObject prefab = (GameObject)Resources.Load("Prefabs/PlayerHead", typeof(GameObject));
            GameObject playerHead = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(prefab);
            Object.DontDestroyOnLoad(playerHead);
            Object.DontDestroyOnLoad(playerHead.GetComponentInChildren<MeshRenderer>());
            Object.DontDestroyOnLoad(playerHead.GetComponentInChildren<MeshRenderer>().material);
            return playerHead;
        }
    }

}
