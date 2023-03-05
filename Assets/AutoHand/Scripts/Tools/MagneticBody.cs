using Autohand;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/magnetic-forces")]
public class MagneticBody : MonoBehaviour
{
    public int magneticIndex = 0;
    public float strengthMultiplyer = 1f;
    public UnityMagneticEvent magneticEnter;
    public UnityMagneticEvent magneticExit;

    [HideInInspector]
    public Rigidbody body;
    private void Start() {
        body = GetComponent<Rigidbody>();
    }
}
