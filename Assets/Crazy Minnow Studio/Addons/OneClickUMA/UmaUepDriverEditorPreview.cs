using UMA;
using UMA.PoseTools;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	[ExecuteInEditMode]
	public class UmaUepDriverEditorPreview : MonoBehaviour
	{
		[HideInInspector] public Transform twirler;
		private UMASkeleton skeleton;
		private UmaUepDriver uepDriver;
		private Transform skeletonRoot;

		private void Start()
		{
			uepDriver = GetComponent<UmaUepDriver>();

			if (uepDriver.expPlayer == null)
				uepDriver.expPlayer = gameObject.GetComponent<UMAExpressionPlayer>();
			if (uepDriver.expPlayer == null)
				uepDriver.expPlayer = gameObject.AddComponent<UMAExpressionPlayer>();

			skeletonRoot = GameObject.Find("Root").transform;
			twirler = new GameObject("twirler").transform;
			twirler.parent = this.gameObject.transform;
			twirler.gameObject.AddComponent<UmaUepEditorPreviewTwirler>();

			uepDriver.uepProxy = GetComponent<UmaUepProxy>();
			uepDriver.InitVars();
			uepDriver.expPlayer.expressionSet = GetComponent<UMAData>().umaRecipe.raceData.expressionSet;
			skeleton = new UMASkeleton(skeletonRoot);

			uepDriver.expPlayer.Initialize();
		}
		void OnRenderObject()
		{
			if (uepDriver.expPlayer.expressionSet == null) return;
			if (skeleton == null) return;

			uepDriver.expPlayer.expressionSet.RestoreBones(skeleton);
		}

		void Update()
		{
			uepDriver.UpdateExpressionPlayer();
			UpdatePreview();
		}

		void UpdatePreview()
		{
			if (uepDriver.expPlayer.expressionSet == null) return;
			if (skeletonRoot == null) return;

			uepDriver.expPlayer.expressionSet.RestoreBones(skeleton);

			float[] values = uepDriver.expPlayer.Values;

			for (int i = 0; i < values.Length; i++)
			{
				float weight = values[i];

				UMABonePose pose = null;
				if (weight > 0)
				{
					pose = uepDriver.expPlayer.expressionSet.posePairs[i].primary;
				}
				else
				{
					weight = -weight;
					pose = uepDriver.expPlayer.expressionSet.posePairs[i].inverse;
				}

				if (pose == null) continue;

				pose.ApplyPose(skeleton, weight);
			}
		}
	}
}