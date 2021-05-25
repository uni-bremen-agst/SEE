using CrazyMinnow.SALSA;
using UnityEngine;

public class SALSA_Template_SalsaEventSubscriber : MonoBehaviour
{
    public Salsa salsa;

    private void OnEnable()
    {
        Salsa.StartedSalsaing += OnStartedSalsaing;
        Salsa.StoppedSalsaing += OnStoppedSalsaing;
    }
    private void OnDisable()
    {
        Salsa.StartedSalsaing -= OnStartedSalsaing;
        Salsa.StoppedSalsaing -= OnStoppedSalsaing;
    }

    private void OnStoppedSalsaing(object sender, Salsa.SalsaNotificationArgs e)
    {
        if (e.salsaInstance == salsa)
        {
            // do some stuff...
            Debug.Log("SALSA fired OnStoppedSalsaing for: " + e.salsaInstance.name);
        }
    }

    private void OnStartedSalsaing(object sender, Salsa.SalsaNotificationArgs e)
    {
        if (e.salsaInstance == salsa)
        {
            // do some stuff...
            Debug.Log("SALSA fired OnStartedSalsaing for: " + e.salsaInstance.name);
        }
    }
}
