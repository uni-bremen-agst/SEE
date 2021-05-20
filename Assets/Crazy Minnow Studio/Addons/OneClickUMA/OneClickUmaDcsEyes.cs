using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	public class OneClickUmaDcsEyes : MonoBehaviour
	{
		public static void Setup(GameObject umaGO)
		{
			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes == null)
			{
				eyes = umaGO.AddComponent<Eyes>();
			}
			else
			{
				DestroyImmediate(eyes);
				eyes = umaGO.AddComponent<Eyes>();
			}
			QueueProcessor qp = umaGO.GetComponent<QueueProcessor>();
			if (qp == null) qp = umaGO.AddComponent<QueueProcessor>();
			if (qp) qp.useMergeWithInfluencer = false;

			// System Properties
			eyes.characterRoot = umaGO.transform;
			eyes.queueProcessor = qp;

			// Eyelids - UMA
			eyes.BuildEyelidTemplate(Eyes.EyelidTemplates.UMA, Eyes.EyelidSelection.Upper);

			float blinkAmount = -1;
			UmaUepProxy proxy = umaGO.GetComponent<UmaUepProxy>();
			if (proxy)
			{
				// Left eye
				eyes.blinklids[0].expData.controllerVars[0].umaUepProxy = proxy;
				eyes.blinklids[0].expData.controllerVars[0].blendIndex = proxy.GetPoseIndex("leftEyeOpen_Close");
				eyes.blinklids[0].expData.controllerVars[0].uepAmount = blinkAmount;
				eyes.blinklids[0].expData.name = "eyelidL";
				eyes.blinklids[0].expData.components[0].isAnimatorControlled = false;
				// Right eye
				eyes.blinklids[1].expData.controllerVars[0].umaUepProxy = proxy;
				eyes.blinklids[1].expData.controllerVars[0].blendIndex = proxy.GetPoseIndex("rightEyeOpen_Close");
				eyes.blinklids[1].expData.controllerVars[0].uepAmount = blinkAmount;
				eyes.blinklids[1].expData.name = "eyelidR";
				eyes.blinklids[1].expData.components[0].isAnimatorControlled = false;

				// Track lids
				eyes.CopyBlinkToTrack();
				eyes.tracklids[0].referenceIdx = 0; // left eye
				eyes.tracklids[1].referenceIdx = 1; // right eye
				if (eyes.eyelidTemplate == Eyes.EyelidTemplates.UMA && eyes.tracklids.Count > 1)
				{
					eyes.tracklids[0].referenceIdx = 0;
					eyes.tracklids[1].referenceIdx = 1;
				}
			}
		}

		public static void ConfigureHead(GameObject umaGO)
		{
			string head = "^head$";
			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes)
			{
				eyes.headBones.Clear();
				eyes.heads.Clear();
				eyes.BuildHeadTemplate(Eyes.HeadTemplates.Bone_Rotation_XY);
				eyes.heads[0].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, head);
				eyes.heads[0].expData.name = "head";
				eyes.heads[0].expData.components[0].name = "head";
				eyes.headTargetOffset.y = 0.1f;
				eyes.CaptureMin(ref eyes.heads);
				eyes.CaptureMax(ref eyes.heads);
				eyes.UpdateRuntimeExpressionControllers(ref eyes.heads);
			}
		}

		public static void ConfigureEyes(GameObject umaGO)
		{
			string eyeL = "^lefteye$";
			string eyeR = "^righteye$";

			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes)
			{
				eyes.eyeBones.Clear();
				eyes.eyes.Clear();
				eyes.BuildEyeTemplate(Eyes.EyeTemplates.Bone_Rotation);
				eyes.eyes[0].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, eyeL);
				eyes.eyes[0].expData.name = "eyeL";
				eyes.eyes[0].expData.components[0].name = "eyeL";
				eyes.eyes[1].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, eyeR);
				eyes.eyes[1].expData.name = "eyeR";
				eyes.eyes[1].expData.components[0].name = "eyeR";
				eyes.CaptureMin(ref eyes.eyes);
				eyes.CaptureMax(ref eyes.eyes);
				eyes.UpdateRuntimeExpressionControllers(ref eyes.eyes);
			}
		}

		public static void ConfigureBlinklids(GameObject umaGO)
		{
			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes && eyes.blinklids.Count > 0)
			{
				eyes.UpdateRuntimeExpressionControllers(ref eyes.blinklids);
			}
		}

		public static void ConfigureTracklids(GameObject umaGO)
		{
			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes && eyes.tracklids.Count > 0)
			{
				eyes.UpdateRuntimeExpressionControllers(ref eyes.tracklids);
			}
		}

		public static void Initialize(GameObject umaGO)
		{
			Eyes eyes = umaGO.GetComponent<Eyes>();
			if (eyes)
			{
				eyes.Initialize();
			}
		}
	}
}