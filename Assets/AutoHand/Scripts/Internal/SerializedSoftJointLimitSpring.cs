using UnityEngine;

[System.Serializable]
public class SerializedSoftJointLimitSpring {
    private SoftJointLimitSpring struc = new SoftJointLimitSpring();

    public static implicit operator SoftJointLimitSpring(SerializedSoftJointLimitSpring c) {
        return new SoftJointLimitSpring() { spring = c._spring, damper = c._damper };
    }
    public static explicit operator SerializedSoftJointLimitSpring(SoftJointLimitSpring c) {
        return new SerializedSoftJointLimitSpring(c);
    }

    public SerializedSoftJointLimitSpring() { }
    private SerializedSoftJointLimitSpring(SoftJointLimitSpring _data) {
        this.damper = _data.damper;
        this.spring = _data.spring;
    }

    [SerializeField]
    private float _spring = 0;
    [SerializeField]
    private float _damper = 0;

    public float damper { get { return struc.damper; } set { _damper = struc.damper = value; } }
    public float spring { get { return struc.spring; } set { _spring = struc.spring = value; } }
}
