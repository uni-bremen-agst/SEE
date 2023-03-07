using UnityEngine;

[System.Serializable]
public class SerializedJointDrive {
    private JointDrive struc = new JointDrive();

    public static implicit operator JointDrive(SerializedJointDrive c) {
        return new JointDrive() { positionSpring = c._spring, positionDamper = c._damper, maximumForce = c._maxForce };
    }
    public static explicit operator SerializedJointDrive(JointDrive c) {
        return new SerializedJointDrive(c);
    }

    public SerializedJointDrive() { }
    private SerializedJointDrive(JointDrive _data) {
        this.damper = _data.positionDamper;
        this.spring = _data.positionSpring;
        this._maxForce = _data.maximumForce;
    }

    [SerializeField]
    private float _spring = 0;
    [SerializeField]
    private float _damper = 0;
    [SerializeField]
    private float _maxForce = 1000;

    public float damper { get { return struc.positionDamper; } set { _damper = struc.positionDamper = value; } }
    public float spring { get { return struc.positionSpring; } set { _spring = struc.positionSpring = value; } }
    public float maxForce { get { return struc.positionSpring; } set { _spring = struc.positionSpring = value; } }
}
