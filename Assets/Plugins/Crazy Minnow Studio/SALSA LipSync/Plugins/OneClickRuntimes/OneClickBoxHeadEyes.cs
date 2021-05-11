using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	public class OneClickBoxHeadEyes : MonoBehaviour
	{
		public static void Setup(GameObject go)
		{
			string head = "boxHead.v2";

			if (go)
			{
				Eyes eyes = go.GetComponent<Eyes>();
				if (eyes == null)
				{
					eyes = go.AddComponent<Eyes>();
				}
				else
				{
					DestroyImmediate(eyes);
					eyes = go.AddComponent<Eyes>();
				}
				QueueProcessor qp = go.GetComponent<QueueProcessor>();
				if (qp == null) qp = go.AddComponent<QueueProcessor>();
				
				// System properties
				eyes.characterRoot = go.transform;
				eyes.queueProcessor = qp;
				
				// Heads - Bone_Rotation
				eyes.BuildHeadTemplate(Eyes.HeadTemplates.Bone_Rotation_XY);
				eyes.heads[0].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, head);
				eyes.heads[0].expData.name = "head";
				eyes.heads[0].expData.components[0].name = "head";
				if (go.name.Contains("small"))
				{
					eyes.headTargetOffset.y = 0.225f;
				}
				else
				{
					eyes.headTargetOffset.y = 1.4f;
					eyes.headRandDistRange = new Vector2(3f, 3f);
					eyes.headTargetRadius = 0.05f;
				}
				eyes.CaptureMin(ref eyes.heads);
				eyes.CaptureMax(ref eyes.heads);
				
				// Eyes - Blendshapes
				SkinnedMeshRenderer smr = Eyes.FindTransform(eyes.characterRoot, head).GetComponent<SkinnedMeshRenderer>();
				eyes.BuildEyeTemplate(Eyes.EyeTemplates.BlendShapes);
				eyes.RemoveExpression(ref eyes.eyes, 1);
				eyes.eyes[0].expData.controllerVars[0].smr = smr;
				eyes.eyes[0].expData.controllerVars[0].blendIndex = 4;
				eyes.eyes[0].expData.controllerVars[1].smr = smr;
				eyes.eyes[0].expData.controllerVars[1].blendIndex = 7;
				eyes.eyes[0].expData.controllerVars[2].smr = smr;
				eyes.eyes[0].expData.controllerVars[2].blendIndex = 5;
				eyes.eyes[0].expData.controllerVars[3].smr = smr;
				eyes.eyes[0].expData.controllerVars[3].blendIndex = 6;
				if (go.GetComponentInChildren<EyeGizmo>() != null)
					DestroyImmediate(go.GetComponentInChildren<EyeGizmo>().gameObject);
				eyes.eyes[0].gizmo = eyes.CreateEyeGizmo(smr.name, eyes.characterRoot);
				eyes.eyes[0].gizmo.transform.parent = smr.transform;
				if (go.name.Contains("small"))
				{
					eyes.eyes[0].gizmo.transform.localPosition = new Vector3(0f, 0.2239f, 0.1624f);
				}
				else
				{
					eyes.eyes[0].gizmo.transform.localPosition = new Vector3(0f, 1.378f, 1.037f);
					eyes.eyeRandTrackFov = new Vector3(0.4f, 0.2f, 0f);
					eyes.eyeRandDistRange = new Vector2(3f, 3f);
					eyes.eyeTargetRadius = 0.05f;
				}
				
				// Eyelids - Blendshapes
				eyes.BuildEyelidTemplate(Eyes.EyelidTemplates.BlendShapes, Eyes.EyelidSelection.Upper);
				eyes.RemoveExpression(ref eyes.blinklids, 1);
				eyes.blinklids[0].expData.controllerVars[0].smr = smr;
				eyes.blinklids[0].expData.controllerVars[0].blendIndex = 8;

				// Add a parent if the character root matches the head bone
				if (go.transform.parent == null)
					eyes.characterRoot = eyes.AddParent(go.transform);
				else
					eyes.characterRoot = go.transform.parent;
				// if (eyes.characterRoot == eyes.heads[0].expData.controllerVars[0].bone)
				
				// Initialize the Eyes moduel
				eyes.Initialize();
			}
		}
	}
}