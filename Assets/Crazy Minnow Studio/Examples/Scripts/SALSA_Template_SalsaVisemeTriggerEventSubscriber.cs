using CrazyMinnow.SALSA;
using UnityEngine;

public class SALSA_Template_SalsaVisemeTriggerEventSubscriber : MonoBehaviour
{
	[SerializeField] private Salsa salsaInstance = null;

	private void OnEnable()
	{
		Salsa.VisemeTriggered += SalsaOnVisemeTriggered;
	}

	private void OnDisable()
	{
		Salsa.VisemeTriggered -= SalsaOnVisemeTriggered;
	}

	private void SalsaOnVisemeTriggered(object sender, Salsa.SalsaNotificationArgs e)
	{
		if (e.salsaInstance == salsaInstance)
		{
			Debug.Log("Viseme triggered: " + e.visemeTrigger);
		}
	}
}
