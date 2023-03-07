using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public class PlaceJoint : PlacePoint {
        [Tooltip("This is for connecting grabbables together, leave empty if using a static")]
        public Grabbable localGrabbable;
        public Rigidbody localRigidbody;
        public Vector3 anchor;
        public Vector3 axis = Vector3.right;
        public bool autoConfigureConnection = true;
        public Vector3 connectedAnchor;
        public Vector3 secondaryAxis = Vector3.up;
        public ConfigurableJointMotion xMotion, yMotion, zMotion;
        public ConfigurableJointMotion angularXMotion, angularYMotion, angularZMotion;
        [SerializeField]
        public SerializedSoftJointLimitSpring linearLimitSpring;
        [SerializeField]
        public SerializedSoftJointLimit linearLimit;
        [SerializeField]
        public SerializedSoftJointLimitSpring angularXLimitSpring;
        public SerializedSoftJointLimit lowAngularXLimit;
        public SerializedSoftJointLimit highAngularXLimit;
        public SerializedSoftJointLimitSpring angularYZLimitSpring;
        public SerializedSoftJointLimit angularYLimit;
        public SerializedSoftJointLimit angularZLimit;
        public Vector3 targetPosition;
        public Vector3 targetVelocity;
        public SerializedJointDrive xDrive;
        public SerializedJointDrive yDrive;
        public SerializedJointDrive zDrive;
        public Vector3 targetAngularVelocity;
        public RotationDriveMode rotationDriveMode;
        public SerializedJointDrive angularXDrive;
        public SerializedJointDrive angularYZDrive;
        public SerializedJointDrive slerpDrive;
        public JointProjectionMode projectionMode = JointProjectionMode.PositionAndRotation;
        public float projectionDistance = 0f;
        public float projectionAngle = 0;
        public bool configuredInWorldSpace;
        public bool swapBodies;


        public bool enableCollision = false;
        public bool enablePreprocessing = true;
        public float breakForce = float.PositiveInfinity;
        public float breakTorque = float.PositiveInfinity;
        public float massScale = 1;
        public float connectedMassScale = 1;
        [Range(1, 255), Tooltip("The solver iterations to use while a placeJoint is connected, higher is higher quality joint stability but more expensive 1-255")]
        public int solverIterations = 100;
        [Range(1, 255), Tooltip("The velocity solver iterations to use while a placeJoint is connected, higher is higher quality joint stability but more expensive 1-255")]
        public int velocitySolverIterations = 100;

        Quaternion targetRotation = Quaternion.identity;
        ConfigurableJoint connectionJoint = null;

        Vector3 prePlacedPos;
        Quaternion prePlacedRot;


        protected override void Start() {
            base.Start();
            base.makePlacedKinematic = false;
            base.placedJointLink = null;
        }
        internal override void Highlight(Grabbable placeObj) {
            base.Highlight(placeObj);
        }

        public override bool CanPlace(Grabbable placeObj) {
            return base.CanPlace(placeObj) && placeObj.body != localRigidbody;
        }

        //protected override void OnEnable() {
        //    base.OnEnable();
        //    if(localGrabbable != null)
        //        localGrabbable.OnReleaseEvent += OnReleased;
        //}

        //protected override void OnDisable() {
        //    base.OnDisable();
        //    if(localGrabbable != null)
        //        localGrabbable.OnReleaseEvent -= OnReleased;
        //}
        //void OnReleased(Hand hand, Grabbable grab) {
        //    if(localGrabbable != null) {
        //        Remove();
        //    }
        //}

        public override void Place(Grabbable placeObj) {
            prePlacedPos = placeObj.transform.position;
            prePlacedRot = placeObj.transform.rotation;

            base.Place(placeObj);
            if(connectionJoint == null && placedObject != null) {
                CreateConnection(placeObj);
            }

            if(localGrabbable) {
                if (localGrabbable.body != null)
                {
                    localGrabbable.body.velocity = Vector3.zero;
                    localGrabbable.body.angularVelocity = Vector3.zero;
                }
                for(int i = 0; i < localGrabbable.jointedBodies.Count; i++) {
                    localGrabbable.jointedBodies[i].velocity = Vector3.zero;
                    localGrabbable.jointedBodies[i].angularVelocity = Vector3.zero;
                }
            }

        }

        private void FixedUpdate() {
            if(placedObject != null) {
                if(connectionJoint == null)
                    Remove(placedObject);
            }
        }


        
        public override void Remove(Grabbable placeObj) {
            if(localGrabbable != null && localGrabbable.body != null)
                localGrabbable.RemoveJointedBody(placeObj.body);
            placeObj.RemoveJointedBody(localRigidbody);

            if(connectionJoint != null) {
                Destroy(connectionJoint);
                connectionJoint = null;
            }

            localRigidbody.solverIterations = Physics.defaultSolverIterations;
            localRigidbody.solverVelocityIterations = Physics.defaultSolverVelocityIterations;
            base.Remove(placeObj);

        }

        Vector3 pregrabPos;
        Quaternion pregrabRot;
        public void CreateConnection(Grabbable connection) {
            if(localRigidbody != null) {
                connection.AddJointedBody(localRigidbody);
                if(localGrabbable != null && localGrabbable.body != null)
                    localGrabbable.AddJointedBody(connection.body);
            }

            localRigidbody.solverIterations = solverIterations;
            localRigidbody.solverVelocityIterations = velocitySolverIterations;
            connectionJoint = connection.gameObject.AddComponent<ConfigurableJoint>();
            connectionJoint.connectedBody = localRigidbody;
            connectionJoint.anchor = anchor;
            connectionJoint.axis = axis;
            connectionJoint.autoConfigureConnectedAnchor = false;
            connectionJoint.connectedAnchor = Vector3.zero;
            connectionJoint.secondaryAxis = secondaryAxis;
            connectionJoint.xMotion = xMotion;
            connectionJoint.yMotion = yMotion;
            connectionJoint.zMotion = zMotion;
            connectionJoint.angularXMotion = angularXMotion;
            connectionJoint.angularYMotion = angularYMotion;
            connectionJoint.angularZMotion = angularZMotion;
            connectionJoint.linearLimitSpring = linearLimitSpring;
            connectionJoint.linearLimit = linearLimit;
            connectionJoint.angularXLimitSpring = angularXLimitSpring;
            connectionJoint.lowAngularXLimit = lowAngularXLimit;
            connectionJoint.highAngularXLimit = highAngularXLimit;
            connectionJoint.angularYZLimitSpring = angularYZLimitSpring;
            connectionJoint.angularYLimit = angularYLimit;
            connectionJoint.angularZLimit = angularZLimit;
            connectionJoint.targetPosition = targetPosition;
            connectionJoint.targetVelocity = targetVelocity;
            connectionJoint.xDrive = xDrive;
            connectionJoint.yDrive = yDrive;
            connectionJoint.zDrive = zDrive;
            connectionJoint.targetAngularVelocity = targetAngularVelocity;
            connectionJoint.rotationDriveMode = rotationDriveMode;
            connectionJoint.angularXDrive = angularXDrive;
            connectionJoint.angularYZDrive = angularYZDrive;
            connectionJoint.slerpDrive = slerpDrive;
            connectionJoint.projectionMode = projectionMode;
            connectionJoint.projectionDistance = projectionDistance;
            connectionJoint.projectionAngle = projectionAngle;
            connectionJoint.configuredInWorldSpace = configuredInWorldSpace;
            connectionJoint.swapBodies = swapBodies;
            connectionJoint.enableCollision = enableCollision;
            connectionJoint.enablePreprocessing = enablePreprocessing;
            connectionJoint.breakForce = breakForce;
            connectionJoint.breakTorque = breakTorque;
            connectionJoint.massScale = massScale;
            connectionJoint.connectedMassScale = connectedMassScale;
        }


        public void Destroyjoint()
        {
            if (connectionJoint != null)
            {
                Destroy(connectionJoint);
                if (localGrabbable != null)
                    localGrabbable.RemoveJointedBody(placedObject.body);
            }
        }
    }
}