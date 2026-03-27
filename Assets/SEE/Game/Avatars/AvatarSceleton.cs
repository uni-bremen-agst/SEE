namespace SEE.Game.Avatars
{
    /// <summary>
    /// Defines the names of our avatar's bones.
    /// </summary>
    public static class AvatarSceleton
    {
        /// <summary>
        /// Name of the game object representing the aim target for <see cref="AimIK"/>.
        /// </summary>
        public const string AimTarget = "AimTarget";
        /// <summary>
        /// Name of the game object representing the aim transform for <see cref="AimIK"/>.
        /// </summary>
        public const string AimTransform = "AimTransform";

        /// <summary>
        /// Name of the base body game object of the avatar. It holds the <see cref="SkinnedMeshRenderer"/>,
        /// and <see cref="FACSnimator"/> among other CC5 components. It is an immediate child of the
        /// avatar root.
        /// </summary>
        public const string BaseBody = "CC_Base_Body";

        /// <summary>
        /// Name of the bone root of the avatar. It is an immediate child of the avatar root.
        /// </summary>
        public const string BoneRoot = "CC_Base_BoneRoot";

        /// <summary>
        /// Name of the hip bone of the avatar.
        /// </summary>
        public const string Hip = BoneRoot + "/CC_Base_Hip";

        /// <summary>
        /// Name of the waist of the avatar.
        /// </summary>
        public const string Waist = Hip + "/CC_Base_Waist";

        /// <summary>
        /// Name of the lower spine of the avatar.
        /// </summary>
        public const string Spine1 = Waist + "/CC_Base_Spine01";

        /// <summary>
        /// Name of the upper spine of the avatar.
        /// </summary>
        public const string Spine2 = Spine1 + "/CC_Base_Spine02";

        /// <summary>
        /// Name of the second neck twist bone of the avatar.
        /// </summary>
        public const string NeckTwist2 = Spine2 + "/CC_Base_NeckTwist01/CC_Base_NeckTwist02";

        /// <summary>
        /// Name of the head bone of the avatar.
        /// </summary>
        public const string Head = NeckTwist2 + "/CC_Base_Head";

        /// <summary>
        /// Name of left clavicle bone of the avatar.
        /// </summary>
        public const string LeftClavicle = Spine2 + "/CC_Base_L_Clavicle";

        /// <summary>
        /// Name of left upperarm bone of the avatar.
        /// </summary>
        public const string LeftUpperArm = LeftClavicle + "/CC_Base_L_Upperarm";

        /// <summary>
        /// Name of left forearm bone of the avatar.
        /// </summary>
        public const string LeftForeArm = LeftUpperArm + "/CC_Base_L_Forearm";

        /// <summary>
        /// Name of the left hand bone of the avatar.
        /// </summary>
        public const string LeftHand = LeftForeArm + "/CC_Base_L_Hand";

        /// <summary>
        /// Name of right clavicle bone of the avatar.
        /// </summary>
        public const string RightClavicle = Spine2 + "/CC_Base_R_Clavicle";

        /// <summary>
        /// Name of right upperarm bone of the avatar.
        /// </summary>
        public const string RightUpperArm = RightClavicle + "/CC_Base_R_Upperarm";

        /// <summary>
        /// Name of right forearm bone of the avatar.
        /// </summary>
        public const string RightForeArm = RightUpperArm + "/CC_Base_R_Forearm";

        /// <summary>
        /// Name of the right hand bone of the avatar.
        /// </summary>
        public const string RightHand = RightForeArm + "/CC_Base_R_Hand";

        /// <summary>
        /// Name of the pelvis of the avatar.
        /// </summary>
        public const string Pelvis = Hip + "/CC_Base_Pelvis";

        /// <summary>
        /// Name of the left thigh of the avatar.
        /// </summary>
        public const string LeftTigh = Pelvis + "/CC_Base_L_Thigh";

        /// <summary>
        /// Name of the left calf of the avatar.
        /// </summary>
        public const string LeftCalf = LeftTigh + "/CC_Base_L_Calf";

        /// <summary>
        /// Name of the left foot of the avatar.
        /// </summary>
        public const string LeftFoot = LeftCalf + "/CC_Base_L_Foot";

        /// <summary>
        /// Name of the right thigh of the avatar.
        /// </summary>
        public const string RightTigh = Pelvis + "/CC_Base_R_Thigh";

        /// <summary>
        /// Name of the right calf of the avatar.
        /// </summary>
        public const string RightCalf = RightTigh + "/CC_Base_R_Calf";

        /// <summary>
        /// Name of the right foot of the avatar.
        /// </summary>
        public const string RightFoot = RightCalf + "/CC_Base_R_Foot";

        /// <summary>
        /// Name of the spine bone in the hierarchy (relative to the root of the avatar).
        /// It is the prefix of all other bones below.
        /// </summary>
        public const string Spine = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02";

        /// <summary>
        /// Names of the bones of the left middle finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftMidFinger1 = LeftHand + "/CC_Base_L_Mid1";
        public const string LeftMidFinger2 = LeftHand + "/CC_Base_L_Mid1/CC_Base_L_Mid2";
        public const string LeftMidFinger3 = LeftHand + "/CC_Base_L_Mid1/CC_Base_L_Mid2/CC_Base_L_Mid3";

        /// <summary>
        /// Names of the bones of the left index finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftIndexFinger1 = LeftHand + "/CC_Base_L_Index1";
        public const string LeftIndexFinger2 = LeftHand + "/CC_Base_L_Index1/CC_Base_L_Index2";
        public const string LeftIndexFinger3  = LeftHand + "/CC_Base_L_Index1/CC_Base_L_Index2/CC_Base_L_Index3";

        /// <summary>
        /// Names of the bones of the left ring finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftRingFinger1 = LeftHand + "/CC_Base_L_Ring1";
        public const string LeftRingFinger2 = LeftHand + "/CC_Base_L_Ring1/CC_Base_L_Ring2";
        public const string LeftRingFinger3 = LeftHand + "/CC_Base_L_Ring1/CC_Base_L_Ring2/CC_Base_L_Ring3";

        /// <summary>
        /// Names of the bones of the left little finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftPinkyFinger1 = LeftHand + "/CC_Base_L_Pinky1";
        public const string LeftPinkyFinger2 = LeftHand + "/CC_Base_L_Pinky1/CC_Base_L_Pinky2";
        public const string LeftPinkyFinger3 = LeftHand + "/CC_Base_L_Pinky1/CC_Base_L_Pinky2/CC_Base_L_Pinky3";

        /// <summary>
        /// Names of the bones of the left thumb in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string LeftThumb1 = LeftHand + "/CC_Base_L_Thumb1";
        public const string LeftThumb2 = LeftHand + "/CC_Base_L_Thumb1/CC_Base_L_Thumb2";
        public const string LeftThumb3 = LeftHand + "/CC_Base_L_Thumb1/CC_Base_L_Thumb2/CC_Base_L_Thumb3";

        /// <summary>
        /// Names of the bones of the right middle finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightMidFinger1 = RightHand + "/CC_Base_R_Mid1";
        public const string RightMidFinger2 = RightHand + "/CC_Base_R_Mid1/CC_Base_R_Mid2";
        public const string RightMidFinger3 = RightHand + "/CC_Base_R_Mid1/CC_Base_R_Mid2/CC_Base_R_Mid3";

        /// <summary>
        /// Names of the bones of the right index finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightIndexFinger1 = RightHand + "/CC_Base_R_Index1";
        public const string RightIndexFinger2 = RightHand + "/CC_Base_R_Index1/CC_Base_R_Index2";
        public const string RightIndexFinger3 = RightHand + "/CC_Base_R_Index1/CC_Base_R_Index2/CC_Base_R_Index3";

        /// <summary>
        /// Names of the bones of the right ring finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightRingFinger1 = RightHand + "/CC_Base_R_Ring1";
        public const string RightRingFinger2 = RightHand + "/CC_Base_R_Ring1/CC_Base_R_Ring2";
        public const string RightRingFinger3 = RightHand + "/CC_Base_R_Ring1/CC_Base_R_Ring2/CC_Base_R_Ring3";

        /// <summary>
        /// Names of the bones of the right little finger in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightPinkyFinger1 = RightHand + "/CC_Base_R_Pinky1";
        public const string RightPinkyFinger2 = RightHand + "/CC_Base_R_Pinky1/CC_Base_R_Pinky2";
        public const string RightPinkyFinger3 = RightHand + "/CC_Base_R_Pinky1/CC_Base_R_Pinky2/CC_Base_R_Pinky3";

        /// <summary>
        /// Names of the bones of the right thumb in the hierarchy (relative to the root of the avatar).
        /// </summary>
        public const string RightThumb1 = RightHand + "/CC_Base_R_Thumb1";
        public const string RightThumb2 = RightHand + "/CC_Base_R_Thumb1/CC_Base_R_Thumb2";
        public const string RightThumb3 = RightHand + "/CC_Base_R_Thumb1/CC_Base_R_Thumb2/CC_Base_R_Thumb3";
    }
}
