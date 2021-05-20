using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UMA
{
	/// <summary>
	/// Base class for UMA character generators.
	/// </summary>
	public abstract class UMAGeneratorBase : MonoBehaviour
	{
		public enum FitMethod {DecreaseResolution, BestFitSquare };

		public bool fitAtlas;
		[HideInInspector]
		public TextureMerge textureMerge;
        [Tooltip("Convert this to a normal texture.")]
		public bool convertRenderTexture;
        [Tooltip("Create Mipmaps for the generated texture. Checking this is a good idea.")]
		public bool convertMipMaps;
        [Tooltip("Initial size of the texture atlas (square)")]
		public int atlasResolution;
		[Tooltip("In Editor Initial size of the texture atlas (square)")]
		public int editorAtlasResolution = 1024;

		[Tooltip("How the textures are fit in the atlas if they are too large to fit normally")]
		public FitMethod AtlasOverflowFitMethod = FitMethod.DecreaseResolution;

		[Tooltip("The percentage to shrink the textures if using DecreaseResolution fit method")]
		[Range(0.1f,0.9f)]
		public float FitPercentageDecrease = 0.5f;

		[Tooltip("When true, the rescaled textures will use a higher mipmap when being downsampled. This will result in a more detailed texture.")]
		public bool SharperFitTextures = true;

		[Tooltip("The default overlay to display if a slot has meshData and no overlays assigned")]
		public OverlayDataAsset defaultOverlayAsset;
		[Tooltip("UMA will ignore items with this tag when rebuilding the skeleton.")]
		public string ignoreTag = "UMAIgnore";

		[NonSerialized]
		public bool FreezeTime;

		public bool SaveAndRestoreIgnoredItems;

		protected OverlayData _defaultOverlayData;
		public OverlayData defaultOverlaydata
		{
			get { return _defaultOverlayData; }
		}

		public static HashSet<int> CreatedAvatars = new HashSet<int>();

        /// <summary>
        /// returns true if the UMAData is in the update queue.
        /// Note that this will return false if the UMA is currently being processed!
        /// </summary>
        /// <param name="umaToCheck"></param>
        /// <returns></returns>
        public abstract bool updatePending(UMAData umaToCheck);

        /// <summary>
        /// Returns true if the UMA is at pos 0 in the DirtyList -
        /// this means it's the UMA that is currently being processed
        /// </summary>
        /// <param name="umaToCheck"></param>
        /// <returns></returns>
        public abstract bool updateProcessing(UMAData umaToCheck);

        /// <summary>
        /// removes the UMAData if it exists in the update queue.
        /// Use this if you need to delete the UMA after scheduling an update for it.
        /// </summary>
        /// <param name="umaToRemove"></param>
        public abstract void removeUMA(UMAData umaToRemove);

		/// <summary>
		/// Adds the dirty UMA to the update queue.
		/// </summary>
		/// <param name="umaToAdd">UMA data to add.</param>
		public abstract void addDirtyUMA(UMAData umaToAdd);
		/// <summary>
		/// Is the dirty queue empty?.
		/// </summary>
		/// <returns><c>true</c> if dirty queue is empty; otherwise, <c>false</c>.</returns>
		public abstract bool IsIdle();

		/// <summary>
		/// Dirty queue size.
		/// </summary>
		/// <returns>The number of items in the dirty queue.</returns>
		public abstract int QueueSize();

		/// <summary>
		/// Call this method to force the generator to work right now.
		/// </summary>
		public abstract void Work();

		/// <summary>
		/// Try to finds the static generator in the scene.
		/// </summary>
		/// <returns>The instance.</returns>
		public static UMAGeneratorBase FindInstance()
		{
			var generatorGO = GameObject.Find("UMAGenerator");
			if (generatorGO == null) return null;
			return generatorGO.GetComponent<UMAGeneratorBase>();
		}

		/// <summary>
		/// Utility class to store data about active animator.
		/// </summary>
		public class AnimatorState
		{
			public bool wasCopied = false;
			public bool FreezeTime;
			private bool wasInitialized;
			private int[] stateHashes = new int[0];
			private float[] stateTimes = new float[0];
			AnimatorControllerParameter[] parameters;
			private Dictionary<int, float> layerWeights = new Dictionary<int, float>();

			public void SaveAnimatorState(Animator animator, UMAData umaData)
			{
				if (animator == null)
                {
					wasCopied = false;
					return;
                }
				umaData.FireAnimatorStateSavedEvent();

				if (animator.runtimeAnimatorController == null)
					return;

				int layerCount = 0;
				if (animator.isInitialized)
				{
					layerCount = animator.layerCount;
				}
				stateHashes = new int[layerCount];
				stateTimes = new float[layerCount];
				if (animator.isInitialized)
				{
					parameters = new AnimatorControllerParameter[animator.parameterCount];
					Array.Copy(animator.parameters, parameters, animator.parameterCount);

					foreach (AnimatorControllerParameter param in parameters)
					{
						switch (param.type)
						{
							case AnimatorControllerParameterType.Bool:
								param.defaultBool = animator.GetBool(param.nameHash);
								break;
							case AnimatorControllerParameterType.Float:
								param.defaultFloat = animator.GetFloat(param.nameHash);
								break;
							case AnimatorControllerParameterType.Int:
								param.defaultInt = animator.GetInteger(param.nameHash);
								break;
						}
					}
				}
				layerWeights.Clear();

				for (int i = 0; i < layerCount; i++)
				{
					var state = animator.GetCurrentAnimatorStateInfo(i);
					stateHashes[i] = state.fullPathHash;
#if UNITY_EDITOR
					float time = state.normalizedTime;
					if (!FreezeTime)
					{
						time += Time.deltaTime / state.length;

					}
#else
					float time = state.normalizedTime + Time.deltaTime / state.length;
#endif
					stateTimes[i] = Mathf.Max(0, time);
					layerWeights.Add(i, animator.GetLayerWeight(i));
				}

				wasCopied = true;
			}

			public void RestoreAnimatorState(Animator animator, UMAData umaData)
			{
				if (wasCopied == false)
					return;
				if (animator == false)
					return;

				if (animator.layerCount == stateHashes.Length)
				{
					for (int i = 0; i < animator.layerCount; i++)
					{
						animator.Play(stateHashes[i], i, stateTimes[i]);
						if (i < layerWeights.Count)
						{
							animator.SetLayerWeight(i, layerWeights[i]);
						}
					}
				}
				if (parameters != null)
				{
					foreach (AnimatorControllerParameter param in parameters)
					{
						if (!animator.IsParameterControlledByCurve(param.nameHash))
						{
							switch (param.type)
							{
								case AnimatorControllerParameterType.Bool:
									animator.SetBool(param.nameHash, param.defaultBool);
									break;
								case AnimatorControllerParameterType.Float:
									animator.SetFloat(param.nameHash, param.defaultFloat);
									break;
								case AnimatorControllerParameterType.Int:
									animator.SetInteger(param.nameHash, param.defaultInt);
									break;
							}
						}
					}
				}
 
				if(!animator.gameObject.activeInHierarchy) {  
					return;
				}

					umaData.FireAnimatorStateRestoredEvent();
				if (animator.isInitialized)
				{
#if UNITY_EDITOR
					if (FreezeTime || animator.enabled == false)
					{
						animator.Update(0);
					}
					else
					{
						animator.Update(Time.deltaTime);
					}

#else
					if (animator.enabled == true)
						animator.Update(Time.deltaTime);
					else
						animator.Update(0);
#endif
				}
			}
		}

		/// <summary>
		/// Update the avatar of a UMA character.
		/// </summary>
		/// <param name="umaData">UMA data.</param>
		public virtual void UpdateAvatar(UMAData umaData)
		{
			if (umaData)
			{
				if (umaData.animationController != null)
				{
					var umaTransform = umaData.transform;
					var oldParent = umaTransform.parent;
					var originalRot = umaTransform.localRotation;
					var originalPos = umaTransform.localPosition;
                    var animator = umaData.animator;

                    umaTransform.SetParent(null, false);
					umaTransform.localRotation = Quaternion.identity;
					umaTransform.localPosition = Vector3.zero;
					
					if (animator == null)
					{
						animator = umaData.gameObject.GetComponent<Animator>();
						if (animator == null)
							animator = umaData.gameObject.AddComponent<Animator>();
						SetAvatar(umaData, animator);
						animator.runtimeAnimatorController = umaData.animationController;
						umaData.animator = animator;

						umaTransform.SetParent(oldParent, false);
						umaTransform.localRotation = originalRot;
						umaTransform.localPosition = originalPos;
					}
					else
					{
						AnimatorState snapshot = new AnimatorState();
#if UNITY_EDITOR
						snapshot.FreezeTime = FreezeTime;
#endif
						snapshot.SaveAnimatorState(animator,umaData);
						if (!umaData.KeepAvatar || animator.avatar == null)
						{
							UMAUtils.DestroyAvatar(animator.avatar);
							SetAvatar(umaData, animator);
						}

						umaTransform.SetParent(oldParent, false);
						umaTransform.localRotation = originalRot;
						umaTransform.localPosition = originalPos;

						if (animator.runtimeAnimatorController != null)
							snapshot.RestoreAnimatorState(animator,umaData);
						if (umaData.KeepAvatar)
                        {
							animator.Rebind();
						}
					}
				}
			}
		}

		/// <summary>
		/// Creates a new avatar for a UMA character.
		/// </summary>
		/// <param name="umaData">UMA data.</param>
		/// <param name="animator">Animator.</param>
		public static void SetAvatar(UMAData umaData, Animator animator)
		{
			var umaTPose = umaData.umaRecipe.raceData.TPose;

			switch (umaData.umaRecipe.raceData.umaTarget)
			{
				case RaceData.UMATarget.Humanoid:
					umaTPose.DeSerialize();
					animator.avatar = CreateAvatar(umaData, umaTPose);
					break;
				case RaceData.UMATarget.Generic:
					animator.avatar = CreateGenericAvatar(umaData);
					break;
			}
		}

		public static void DebugLogHumanAvatar(GameObject root, HumanDescription description)
		{
			if (Debug.isDebugBuild)
				Debug.Log("***", root);
			Dictionary<String, String> bones = new Dictionary<String, String>();
			foreach (var sb in description.skeleton)
			{
				if (Debug.isDebugBuild)
					Debug.Log(sb.name);
				bones[sb.name] = sb.name;
			}
			if (Debug.isDebugBuild)
				Debug.Log("----");
			foreach (var hb in description.human)
			{
				string boneName;
				if (bones.TryGetValue(hb.boneName, out boneName))
				{
					if (Debug.isDebugBuild)
						Debug.Log(hb.humanName + " -> " + boneName);
				}
				else
				{
					if (Debug.isDebugBuild)
						Debug.LogWarning(hb.humanName + " !-> " + hb.boneName);
				}
			}
			if (Debug.isDebugBuild)
				Debug.Log("++++");
		}

		/// <summary>
		/// Creates a human (biped) avatar for a UMA character.
		/// </summary>
		/// <returns>The human avatar.</returns>
		/// <param name="umaData">UMA data.</param>
		/// <param name="umaTPose">UMA TPose.</param>
		public static Avatar CreateAvatar(UMAData umaData, UmaTPose umaTPose)
		{
			umaTPose.DeSerialize();
			HumanDescription description = CreateHumanDescription(umaData, umaTPose);
			//DebugLogHumanAvatar(umaData.gameObject, description);
			Avatar res = AvatarBuilder.BuildHumanAvatar(umaData.gameObject, description);
			CreatedAvatars.Add(res.GetInstanceID());
			res.name = umaData.name;
			return res;
		}

		/// <summary>
		/// Creates a generic avatar for a UMA character.
		/// </summary>
		/// <returns>The generic avatar.</returns>
		/// <param name="umaData">UMA data.</param>
		public static Avatar CreateGenericAvatar(UMAData umaData)
		{
			Avatar res = AvatarBuilder.BuildGenericAvatar(umaData.gameObject, umaData.umaRecipe.GetRace().genericRootMotionTransformName);
			res.name = umaData.name;
			CreatedAvatars.Add(res.GetInstanceID());
			return res;
		}

		/// <summary>
		/// Creates a Mecanim human description for a UMA character.
		/// </summary>
		/// <returns>The human description.</returns>
		/// <param name="umaData">UMA data.</param>
		/// <param name="umaTPose">UMA TPose.</param>
		public static HumanDescription CreateHumanDescription(UMAData umaData, UmaTPose umaTPose)
		{
			var res = new HumanDescription();
			res.armStretch = umaTPose.armStretch == 0.0f ? 0.05f : umaTPose.armStretch; // this is for compatiblity with the existing tpose. 
			res.legStretch = umaTPose.legStretch == 0.0f ? 0.05f : umaTPose.legStretch; 
			res.feetSpacing = umaTPose.feetSpacing;
			res.lowerArmTwist = umaTPose.lowerArmTwist == 0.0f ? 0.5f : umaTPose.lowerArmTwist;
			res.lowerLegTwist = umaTPose.lowerLegTwist == 0.0f ? 0.5f : umaTPose.lowerLegTwist;
			res.upperArmTwist = umaTPose.upperArmTwist == 0.0f ? 0.5f : umaTPose.upperArmTwist;
			res.upperLegTwist = umaTPose.upperLegTwist == 0.0f ? 0.5f : umaTPose.upperLegTwist;
			res.skeleton = umaTPose.boneInfo;
			res.human = umaTPose.humanInfo;

			SkeletonModifier(umaData, ref res.skeleton, res.human);
			return res;
		}

#pragma warning disable 618
		private void ModifySkeletonBone(ref SkeletonBone bone, Transform trans)
		{
			bone.position = trans.localPosition;
			bone.rotation = trans.localRotation;
			bone.scale = trans.localScale;
		}

		private static List<SkeletonBone> newBones = new List<SkeletonBone>();
		private static void SkeletonModifier(UMAData umaData, ref SkeletonBone[] bones, HumanBone[] human)
		{
			int missingBoneCount = 0;
			newBones.Clear();

			while (!umaData.skeleton.HasBone(UMAUtils.StringToHash(bones[missingBoneCount].name)))
			{
				missingBoneCount++;
			}
			if (missingBoneCount > 0)
			{
				// force the two root transforms, reuse old bones entries to ensure any humanoid identifiers stay intact
				var realRootBone = umaData.transform;
				var newBone = bones[missingBoneCount - 2];
				newBone.position = realRootBone.localPosition;
				newBone.rotation = realRootBone.localRotation;
				newBone.scale = realRootBone.localScale;
				//				Debug.Log(newBone.name + "<-"+realRootBone.name);
				newBone.name = realRootBone.name;
				newBones.Add(newBone);

				var rootBoneTransform = umaData.umaRoot.transform;
				newBone = bones[missingBoneCount - 1];
				newBone.position = rootBoneTransform.localPosition;
				newBone.rotation = rootBoneTransform.localRotation;
				newBone.scale = rootBoneTransform.localScale;
				//				Debug.Log(newBone.name + "<-" + rootBoneTransform.name);
				newBone.name = rootBoneTransform.name;
				newBones.Add(newBone);
			}

			for (var i = missingBoneCount; i < bones.Length; i++)
			{
				var skeletonbone = bones[i];
				int boneHash = UMAUtils.StringToHash(skeletonbone.name);
				GameObject boneGO = umaData.skeleton.GetBoneGameObject(boneHash);
				if (boneGO != null)
				{
					skeletonbone.position = boneGO.transform.localPosition;
					skeletonbone.scale = boneGO.transform.localScale;
					skeletonbone.rotation = umaData.skeleton.GetTPoseCorrectedRotation(boneHash, skeletonbone.rotation);
					newBones.Add(skeletonbone);
				}
			}
			bones = newBones.ToArray();
		}
#pragma warning restore 618
	}
}
