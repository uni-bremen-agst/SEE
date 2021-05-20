using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	public class OneClickBase : MonoBehaviour
	{
		/// <summary>
		/// RELEASE NOTES:
		/// 	2.5.0.2:
		/// 		~ Moved OneClickConfiguration type to separate file.
		/// 		~ Refactor of OneClickExpression > derived type OneClickEmoterExpression and moved to separate file.
		/// 		~ Complete refactor of control type (OneClickComponent) implementation.
		/// 			Much easier to add new OneClick-configurable types (i.e. Shape, Bone, UMA, Animator, etc.)
		/// 		~ More refactoring for clarity/readability...
		/// 	2.5.0.1:
		/// 		+ Animator controller type...AddAnimatorComponent()
		/// 		~ Refactoring...
		/// 		! Bone timing applied vice default timings...
		/// 	2.5.0:
		/// 		+ audioUpdateDelay setting: default 0.0875f.
		/// 		+ support for always emphasis emotes: default false.
		/// 		~ audio clip resource now only updates if not set.
		/// 		~ lipsync emphasis chance defaults to 1.0f.
		///		2.1.6:
		/// 		~ audio source now defaults to loop = false.
		///		2.1.5:
		/// 		+ support for UMA core controller, requires SALSA core 2.1.0-experimental+
		/// 	2.1.4:
		/// 		+ new regex blendshape search to handle more complex scenarios.
		/// 	2.1.3:
		/// 		! AddBoneComponent now includes duration values.
		///		2.1.2:
		/// 		~ null AudioClip does not assign.
		/// 	2.1.1:
		/// 		+ new string constants supporting: 2018.4+ check for prefab and warn > then unpack or cancel.
		/// 	2.1.0:
		/// 		~ convert from editor code to full engine code and move to Plugins.
		/// 	2.0.1:
		/// 		! fix out-of-range error when no blendshapes are detected.
		///		2.0.0-BETA : Initial release.
		/// ==========================================================================
		/// PURPOSE: This script provides simple, simulated lip-sync input to the
		///		Salsa component from text/string values. For the latest information
		///		visit crazyminnowstudio.com.
		/// ==========================================================================
		/// DISCLAIMER: While every attempt has been made to ensure the safe content
		///		and operation of these files, they are provided as-is, without
		///		warranty or guarantee of any kind. By downloading and using these
		///		files you are accepting any and all risks associated and release
		///		Crazy Minnow Studio, LLC of any and all liability.
		/// ==========================================================================
		/// </summary>

		public const string RESOURCE_CLIP = "Assets/Crazy Minnow Studio/Examples/Audio/Promo-male.mp3";
		public const string PREFAB_ALERT_TITLE = "Prefab Unpack Warning";
		public const string PREFAB_ALERT_MSG = "Your selection is a prefab and must be unpacked to apply this setup. " +
											   "You can create a new prefab once the setup is complete. " +
											   "Do you want to proceed?";


		protected static List<SkinnedMeshRenderer> requiredSmrs = new List<SkinnedMeshRenderer>();
		protected static List<OneClickConfiguration> oneClickConfigurations = new List<OneClickConfiguration>();
		protected static Salsa salsa;
		protected static Emoter emoter;
		protected static UmaUepProxy uepProxy; // only if there is a UMA component requiring it.
//		protected static Eyes eyes;
		protected static GameObject selectedObject;

		protected static float emphasizerTrigger;

		// adjust these salsa settings to taste...
		//	- data analysis settings
		protected static bool autoAdjustAnalysis = true;
		protected static bool autoAdjustMicrophone = false; // only true if you are using micInput
		protected static float audioUpdateDelay = 0.0875f;
		// advanced dynamics
		protected static float loCutoff = 0.015f;
		protected static float hiCutoff = 0.75f;
		protected static bool useAdvDyn = true;
		protected static float advDynPrimaryBias = 0.45f;
		protected static bool useAdvDynJitter = true;
		protected static float advDynJitterAmount = 0.10f;
		protected static float advDynJitterProb = 0.20f;
		protected static float advDynSecondaryMix = 0.0f;

		// emoter settings...
		protected static float emphasisChance = 1.0f;
		protected static bool useRandomEmotes = false;
		protected static bool isChancePerEmote = false;
		protected static int numRandomEmotesPerCycle = 1;
		protected static float randomEmoteMinTimer = 1.0f;
		protected static float randomEmoteMaxTimer = 2.0f;
		protected static float randomChance = 0.5f;
		protected static bool useRandomFrac = false;
		protected static float randomFracBias = 0.5f;
		protected static bool useRandomHoldDuration = false;
		protected static float randomHoldDurationMin = 0.1f;
		protected static float randomHoldDurationMax = 0.5f;
		protected static LerpEasings.EasingType easingType = LerpEasings.EasingType.CubicOut;

		private static OneClickConfiguration currentConfiguration;

		protected static void NewConfiguration(OneClickConfiguration.ConfigType configType)
		{
			oneClickConfigurations.Add(new OneClickConfiguration(configType));
			currentConfiguration = oneClickConfigurations[oneClickConfigurations.Count - 1];
		}

		protected static void AddSmrSearch(string search)
		{
			currentConfiguration.smrSearches.Add(search);
		}

		/// <summary>
		/// Abstraction of setting up a new expression...
		/// </summary>
		/// <param name="expressionName"></param>
		protected static void NewExpression(string expressionName)
		{
			if (currentConfiguration.type == OneClickConfiguration.ConfigType.Salsa)
				currentConfiguration.oneClickExpressions.Add(new OneClickExpression(expressionName, new List<OneClickComponent>()));
			else
				currentConfiguration.oneClickExpressions.Add(new OneClickEmoterExpression(expressionName, new List<OneClickComponent>()));
		}

		protected static void AddShapeComponent(string[] blendshapeNames,
												float durOn,
												float durHold,
												float durOff,
		                                        string componentName = "",
		                                        float amount = 1.0f,
												bool useRegex = false
			)
		{
			var currentConfigExpression = currentConfiguration.oneClickExpressions[currentConfiguration.oneClickExpressions.Count - 1];
			currentConfigExpression.components.Add(new OneClickShapeComponent(componentName,
				                                      blendshapeNames,
				                                      amount,
													  durOn,
													  durHold,
													  durOff,
				                                      OneClickComponent.ComponentType.Shape,
													  useRegex));
		}

		protected static void AddUepPoseComponent(string poseName,
												  float durOn,
												  float durHold,
												  float durOff,
												  string componentName = "",
												  float amount = 1.0f
			)
		{
			var currentConfigExpression = currentConfiguration.oneClickExpressions[currentConfiguration.oneClickExpressions.Count - 1];
			currentConfigExpression.components.Add(new OneClickUepComponent(componentName,
													  poseName,
				                                      amount,
													  durOn,
													  durHold,
													  durOff,
				                                      OneClickComponent.ComponentType.UMA));
		}

		protected static void AddAnimatorComponent(string componentName,
												  string animatorSearch,
												  float durOn,
												  float durHold,
												  float durOff,
												  int parmIndex,
												  bool isTriggereParmBiDir = false
			)
		{
			var currentConfigExpression = currentConfiguration.oneClickExpressions[currentConfiguration.oneClickExpressions.Count - 1];
			currentConfigExpression.components.Add(new OneClickAnimatorComponent(componentName,
															animatorSearch,
															parmIndex,
															isTriggereParmBiDir,
															durOn,
															durHold,
															durOff,
															OneClickComponent.ComponentType.Animator));
		}

		protected static void AddBoneComponent(string componentSearchName,
		                                       TformBase maxTform,
											   float durOn,
											   float durHold,
											   float durOff,
		                                       string componentName = "",
		                                       bool constrainPos = false,
		                                       bool constrainRot = true,
		                                       bool constrainScl = false)
		{
			var currentConfigExpression = currentConfiguration.oneClickExpressions[currentConfiguration.oneClickExpressions.Count - 1];
			currentConfigExpression.components.Add(new OneClickBoneComponent(componentName,
				                                      componentSearchName,
				                                      maxTform,
				                                      constrainPos,
				                                      constrainRot,
				                                      constrainScl,
													  durOn,
													  durHold,
													  durOff,
				                                      OneClickComponent.ComponentType.Bone));
		}

		protected static void AddEmoteFlags(bool isRandom, bool isEmph, bool isRepeater, float frac = 1.0f, bool isAlwaysEmph = false)
		{
			var currentExpression = (OneClickEmoterExpression)currentConfiguration.oneClickExpressions[currentConfiguration.oneClickExpressions.Count - 1];
			currentExpression.SetEmoterBools(isRandom, isEmph, isRepeater, frac, isAlwaysEmph);
		}

		protected static void DoOneClickiness(GameObject go, AudioClip clip)
		{
			selectedObject = go;

			// setup QueueProcessor
			var qp = selectedObject.GetComponent<QueueProcessor>(); // get QueueProcessor on current object -- we no longer look in-scene.
			if (qp == null)
				qp = selectedObject.AddComponent<QueueProcessor>();

			// if there is a uepProxy, get reference to it.
			uepProxy = go.GetComponent<UmaUepProxy>();

			foreach (var configuration in oneClickConfigurations)
			{
				if (!FindSkinnedMeshRenderers(configuration)) return;

				// module-specific configuraiton requirements::
				switch (configuration.type)
				{
					#region salsa-specific setup
					case OneClickConfiguration.ConfigType.Salsa:
						ConfigureSalsaSettings(clip, qp);
						break;
					#endregion

					#region emoter-specific setup
					case OneClickConfiguration.ConfigType.Emoter:
						ConfigEmoterSettings(qp);
						break;
					#endregion
				}

				ConfigureModuleExpressions(configuration);
			}
		}

		private static bool FindSkinnedMeshRenderers(OneClickConfiguration configuration)
		{
			requiredSmrs.Clear(); // reset list of SMRs...

			if (configuration.smrSearches.Count > 0)
			{
				var allSmrs = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				// quick test to see if this object has the necessary stuff
				if (allSmrs == null || allSmrs.Length == 0)
				{
					Debug.LogError(
						"This object does not have the required components. No Skinned Mesh Renderers found. Ensure this one-click script was applied to the root of the model prefab in the scene hierarchy.");
					return false;
				}

				// Find the necessary SMR's
				foreach (var smr in allSmrs)
				{
					foreach (var smrSearch in configuration.smrSearches)
					{
						if (Regex.IsMatch(smr.name, smrSearch, RegexOptions.IgnoreCase))
							requiredSmrs.Add(smr);
					}
				}

				if (requiredSmrs.Count == 0)
				{
					string smrsSearched = "";
					foreach (var smrSearch in configuration.smrSearches)
						smrsSearched += smrSearch + " ";
					Debug.LogError(
						"This object does not have the required components. Could not find the referenced SMRs. Ensure the appropriate one-click was used for your model type and generation. " +
						smrsSearched);
					return false;
				}
			}

			return true;
		}

		private static void ConfigEmoterSettings(QueueProcessor qp)
		{
			emoter = selectedObject.GetComponent<Emoter>();
			if (emoter == null)
				emoter = selectedObject.AddComponent<Emoter>();

			if (salsa != null)
				salsa.emoter = emoter;

			emoter.queueProcessor = qp;

			emoter.lipsyncEmphasisChance = emphasisChance;
			emoter.useRandomEmotes = useRandomEmotes;
			emoter.isChancePerEmote = isChancePerEmote;
			emoter.NumRandomEmotesPerCycle = numRandomEmotesPerCycle;
			emoter.randomEmoteMinTimer = randomEmoteMinTimer;
			emoter.randomEmoteMaxTimer = randomEmoteMaxTimer;
			emoter.randomChance = randomChance;
			emoter.useRandomFrac = useRandomFrac;
			emoter.randomFracBias = randomFracBias;
			emoter.useRandomHoldDuration = useRandomHoldDuration;
			emoter.randomHoldDurationMin = randomHoldDurationMin;
			emoter.randomHoldDurationMax = randomHoldDurationMax;

			emoter.emotes.Clear();
		}

		private static void ConfigureSalsaSettings(AudioClip clip, QueueProcessor qp)
		{
			salsa = selectedObject.GetComponent<Salsa>();
			if (salsa == null)
				salsa = selectedObject.AddComponent<Salsa>();

			// configure AudioSource for demonstration
			var audSrc = selectedObject.GetComponent<AudioSource>();
			if (audSrc == null)
				audSrc = selectedObject.AddComponent<AudioSource>();
			audSrc.playOnAwake = true;
			audSrc.loop = false;
			if (clip != null && audSrc.clip == null)
				audSrc.clip = clip;
			salsa.audioSrc = audSrc;

			salsa.queueProcessor = qp;

			// adjust salsa settings
			//	- data analysis settings
			salsa.autoAdjustAnalysis = autoAdjustAnalysis;
			salsa.autoAdjustMicrophone = autoAdjustMicrophone;
			salsa.audioUpdateDelay = audioUpdateDelay;
			//	- advanced dynamics
			salsa.loCutoff = loCutoff;
			salsa.hiCutoff = hiCutoff;
			salsa.useAdvDyn = useAdvDyn;
			salsa.advDynPrimaryBias = advDynPrimaryBias;
			salsa.useAdvDynJitter = useAdvDynJitter;
			salsa.advDynJitterAmount = advDynJitterAmount;
			salsa.advDynJitterProb = advDynJitterProb;
			salsa.advDynSecondaryMix = advDynSecondaryMix;

			salsa.emphasizerTrigger = emphasizerTrigger;

			salsa.visemes.Clear();
		}

		protected static void Init()
		{
			oneClickConfigurations.Clear();	// clean configurations to prevent additive configurations
			requiredSmrs.Clear();
		}

		private static void ConfigureModuleExpressions(OneClickConfiguration configuration)
		{
			for (int exp = 0; exp < configuration.oneClickExpressions.Count; exp++)
			{
				Expression expression = InitializeExpressionType(configuration, exp);

				var componentCount = 0; // keeps track of good component creation to prevent creating components when criteria are not met (i.e. search blendshape name not found).
				var currCmpntID = 0; // holds the index of the current component being configured.
				InspectorControllerHelperData controlHelper;
				ExpressionComponent component = new ExpressionComponent();

				for (int j = 0; j < configuration.oneClickExpressions[exp].components.Count; j++)
				{
					var oneClickComponent = configuration.oneClickExpressions[exp].components[j];

					switch (oneClickComponent.type)
					{
						case OneClickComponent.ComponentType.Shape:
							// create a component for each valid, required SMR.
							for (int i = 0; i < requiredSmrs.Count; i++)
							{
								// cast the OneClickComponent...
								var oneClickShapeComponent = (OneClickShapeComponent) oneClickComponent;

								int blendshapeIndex = -1;
								// we need to confirm proposed blendshape names are viable...
								foreach (var blendshapeName in oneClickShapeComponent.blendshapeNames)
								{
									if (oneClickShapeComponent.useRegex)
										blendshapeIndex = RegexFindBlendshapeName(requiredSmrs[i], blendshapeName);
									else
										blendshapeIndex = requiredSmrs[i].sharedMesh.GetBlendShapeIndex(blendshapeName);

									if (blendshapeIndex > -1) // we found one!
										break;
								}
								if (blendshapeIndex == -1) // we didn't find our blendshape...
									continue;

								// Create a new component if applicable.
								currCmpntID = CreateNewComponent(componentCount, expression);

								controlHelper = expression.controllerVars[currCmpntID];
								controlHelper.smr = requiredSmrs[i];
								controlHelper.blendIndex = blendshapeIndex;
								controlHelper.minShape = 0f;
								controlHelper.maxShape = oneClickShapeComponent.maxAmount;

								component = expression.components[currCmpntID];
								component.controlType = ExpressionComponent.ControlType.Shape;

								ApplyCommonSettingsToComponent(component, oneClickComponent, currCmpntID);
								componentCount++; // good component created, update component count.
							}

							break;

						case OneClickComponent.ComponentType.UMA:
							var oneClickUepComponent = (OneClickUepComponent) oneClickComponent;

							// Create a new component if applicable.
							currCmpntID = CreateNewComponent(componentCount, expression);

							controlHelper = expression.controllerVars[currCmpntID];
							controlHelper.umaUepProxy = uepProxy;
							controlHelper.blendIndex = uepProxy.GetPoseIndex(oneClickUepComponent.poseName);
							controlHelper.minShape = 0f;
							controlHelper.uepAmount = oneClickUepComponent.maxAmount;

							component = expression.components[currCmpntID];
							component.controlType = ExpressionComponent.ControlType.UMA;

							ApplyCommonSettingsToComponent(component, oneClickComponent, currCmpntID);
							componentCount++; // good component created, update component count.

							break;

						case OneClickComponent.ComponentType.Bone:
							var oneClickBoneComponent = (OneClickBoneComponent) oneClickComponent;

							// confirm bone is viable...
							var bone = FindBone(oneClickBoneComponent.componentSearchName);
							if (bone == null)
							{
								Debug.LogWarning("Could not find bone: " + oneClickBoneComponent.componentSearchName);
								continue;
							}

							// Create a new component if applicable.
							currCmpntID = CreateNewComponent(componentCount, expression);

							controlHelper = expression.controllerVars[currCmpntID];
							controlHelper.bone = bone;
							controlHelper.startTform = ConvertBoneToTform(bone);
							controlHelper.endTform = oneClickBoneComponent.max;
							controlHelper.fracRot = oneClickBoneComponent.useRot;
							controlHelper.fracPos = oneClickBoneComponent.usePos;
							controlHelper.fracScl = oneClickBoneComponent.useScl;
							controlHelper.inspIsSetStart = true;
							controlHelper.inspIsSetEnd = true;

							component = expression.components[currCmpntID];
							component.controlType = ExpressionComponent.ControlType.Bone;

							controlHelper.StoreBoneBase();
							controlHelper.StoreStartTform();

							ApplyCommonSettingsToComponent(component, oneClickComponent, currCmpntID);
							componentCount++; // good component created, update component count.

							break;

						case OneClickComponent.ComponentType.Animator:
							var oneClickAnimatorComponent = (OneClickAnimatorComponent) oneClickComponent;

							var animator = FindAnimator(oneClickAnimatorComponent.componentSearchName);
							if (animator == null)
							{
								Debug.LogWarning("Could not find Animator: " + oneClickAnimatorComponent.componentSearchName);
								continue;
							}

							// Create a new component if applicable.
							currCmpntID = CreateNewComponent(componentCount, expression);

							controlHelper = expression.controllerVars[currCmpntID];
							controlHelper.animator = animator;
							controlHelper.isTriggerParameterBiDirectional = oneClickAnimatorComponent.isTriggerParmBiDirectional;
							controlHelper.blendIndex = oneClickAnimatorComponent.animationParmIndex;

							component = expression.components[currCmpntID];
							component.controlType = ExpressionComponent.ControlType.Animator;

							ApplyCommonSettingsToComponent(component, oneClickComponent, currCmpntID);
							componentCount++; // good component created, update component count.

							break;
					}
				}

				// if no component was created for this expression, remove it.
				if (componentCount == 0)
				{
					Debug.Log("Removed expression " + expression.name + " This may be OK, but may indicate a change in the model generator. If this is a supported model, contact Crazy Minnow Studio via assetsupport@crazyminnow.com.");
					switch (configuration.type)
					{
						case OneClickConfiguration.ConfigType.Salsa:
							salsa.visemes.RemoveAt(salsa.visemes.Count - 1);
							break;
						case OneClickConfiguration.ConfigType.Emoter:
							emoter.emotes.RemoveAt(emoter.emotes.Count - 1);
							break;
					}
				}

				// module-specific wrap-up
				switch (configuration.type)
				{
					case OneClickConfiguration.ConfigType.Salsa:
						salsa.DistributeTriggers(LerpEasings.EasingType.SquaredIn);
						break;
				}
			}
		}

		private static Expression InitializeExpressionType(OneClickConfiguration configuration, int exp)
		{
			Expression expressionData;
			// module-specific expression actions
			switch (configuration.type)
			{
				case OneClickConfiguration.ConfigType.Salsa:
					// create our salsa viseme for each oneClickExpression.
					salsa.visemes.Add(new LipsyncExpression(configuration.oneClickExpressions[exp].name,
						new InspectorControllerHelperData(), 0f));
					var viseme = salsa.visemes[salsa.visemes.Count - 1];
					viseme.expData.inspFoldout = false;
					expressionData = viseme.expData;
					break;
				case OneClickConfiguration.ConfigType.Emoter:
					// create our emoter emote for each oneClickExpression.
					emoter.emotes.Add(new EmoteExpression(configuration.oneClickExpressions[exp].name,
						new InspectorControllerHelperData(), false, true, false, 0f));
					var emote = emoter.emotes[emoter.emotes.Count - 1];
					var oneClickEmoterExpression = (OneClickEmoterExpression) configuration.oneClickExpressions[exp];
					emote.expData.inspFoldout = false;
					emote.isRandomEmote = oneClickEmoterExpression.isRandom;
					emote.isLipsyncEmphasisEmote = oneClickEmoterExpression.isEmphasis;
					emote.isAlwaysEmphasisEmote = oneClickEmoterExpression.isAlwaysEmphasis;
					emote.isRepeaterEmote = oneClickEmoterExpression.isRepeater;
					emote.frac = oneClickEmoterExpression.expressionDynamics;
					expressionData = emote.expData;
					break;

				default:
					expressionData = salsa.visemes[salsa.visemes.Count - 1].expData;
					break;
			}

			return expressionData;
		}

		private static int RegexFindBlendshapeName(SkinnedMeshRenderer smr, string bName)
		{
			var bNames = GetBlendshapeNames(smr);
			for (int i = 0; i < bNames.Length; i++)
			{
				if (Regex.IsMatch(bNames[i], bName, RegexOptions.IgnoreCase))
					return i;
			}

			return -1;
		}

		private static string[] GetBlendshapeNames(SkinnedMeshRenderer smr)
		{
			string[] bNames = new string[smr.sharedMesh.blendShapeCount];
			for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
			{
				bNames[i] = smr.sharedMesh.GetBlendShapeName(i);
			}

			return bNames;
		}

		private static TformBase ConvertBoneToTform(Transform bone)
		{
			return new TformBase(new Vector3(bone.localPosition.x, bone.localPosition.y, bone.localPosition.x),
			                     new Quaternion(bone.localRotation.x, bone.localRotation.y, bone.localRotation.z, bone.localRotation.w),
			                     new Vector3(bone.localScale.x, bone.localScale.y, bone.localScale.z));
		}

		private static Transform FindBone(string componentSearchName)
		{
			var bones = selectedObject.GetComponentsInChildren<Transform>();
			foreach (var bone in bones)
			{
				if (Regex.IsMatch(bone.name, componentSearchName, RegexOptions.IgnoreCase))
					return bone;
			}

			return null;
		}
		private static Animator FindAnimator(string componentSearchName)
		{
			var animators = selectedObject.GetComponentsInChildren<Animator>();
			foreach (var animator in animators)
			{
				if (Regex.IsMatch(animator.name, componentSearchName, RegexOptions.IgnoreCase))
					return animator;
			}

			return null;
		}

		private static void ApplyCommonSettingsToComponent(ExpressionComponent component,
		                                                   OneClickComponent oneClickComponent,
		                                                   int componentNumber)
		{
			component.durationOn = oneClickComponent.durOn;
			component.durationOff = oneClickComponent.durOff;
			component.durationHold = oneClickComponent.durHold;
			component.easing = easingType;
			component.inspFoldout = false;
			component.name = String.IsNullOrEmpty(oneClickComponent.componentName)
				            ? "component " + componentNumber.ToString()
				            : oneClickComponent.componentName;
		}

		private static int CreateNewComponent(int componentCount, Expression expression)
		{
			if (componentCount > expression.components.Count - 1)
			{
				expression.components.Add(new ExpressionComponent());
				expression.controllerVars.Add(new InspectorControllerHelperData());
			}

			return expression.components.Count - 1;
		}
	}
}