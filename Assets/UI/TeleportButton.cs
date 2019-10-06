using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    public GameObject ListItem;

    public void OnTeleport()
    {
        ListItem.GetComponent<ListItem>().OnTeleport();
    }
}
