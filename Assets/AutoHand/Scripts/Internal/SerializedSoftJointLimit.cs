using UnityEngine;

[System.Serializable]
public class SerializedSoftJointLimit {
    private SoftJointLimit struc = new SoftJointLimit();
    public static implicit operator SoftJointLimit(SerializedSoftJointLimit c) {
        return new SoftJointLimit() { limit = c._limit, bounciness = c._bounciness, contactDistance = c._contactDistance };
    }
    public static explicit operator SerializedSoftJointLimit(SoftJointLimit c) {
        return new SerializedSoftJointLimit(c);
    }
    public SerializedSoftJointLimit() { }
    private SerializedSoftJointLimit(SoftJointLimit _data) {
        this.limit = _data.limit;
        this.bounciness = _data.bounciness;
        this.contactDistance = _data.contactDistance;
    }
    [SerializeField]
    private float _limit = 0;
    [SerializeField]
    private float _bounciness = 0;
    [SerializeField]
    private float _contactDistance = 0;

    public float limit { get { return struc.limit; } set { _limit = struc.limit = value; } }
    public float bounciness { get { return struc.bounciness; } set { _bounciness = struc.bounciness = value; } }
    public float contactDistance { get { return struc.contactDistance; } set { _contactDistance = struc.contactDistance = value; } }
}