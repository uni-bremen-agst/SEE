using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UMA
{
	/// <summary>
	/// Utility class for generating texture atlases
	/// </summary>
	public class UMAGeneratorPro
	{
		private struct PackSize
		{
			public int Width;
			public int Height;
			public bool success;
			public int xMax;
			public int yMax;
		}


		private struct SizeInt
        {
			public int Width;
			public int Height;
        }

		private class GeneratedMaterialLookupKey : IEquatable<GeneratedMaterialLookupKey>
		{
			public List<OverlayData> overlayList;
			public UMARendererAsset rendererAsset;
			//public string slotname;

            public override int GetHashCode()
            {
				if (overlayList != null)
					return overlayList.GetHashCode();
				return base.GetHashCode();
            }

            public bool Equals(GeneratedMaterialLookupKey other)
			{
				/*Debug.Log("comparing to slot: " + slotname);
				if (rendererAsset != null)
                {
					if (rendererAsset != other.rendererAsset)
                    {
						Debug.Log("Renderer assets do not match");
						return false;
                    }
                }
				if (overlayList != null)
                {
					if (overlayList != other.overlayList)
                    {
						Debug.Log("Overlay lists do not match");
						return false;
                    }
                } 
				return true; */
				return (overlayList == other.overlayList && rendererAsset == other.rendererAsset);
			}
		}

		TextureProcessPRO textureProcesser;

		MaxRectsBinPack packTexture;

		UMAGeneratorBase umaGenerator;
		UMAData umaData;
		Texture[] backUpTexture;
		bool updateMaterialList;
		int scaleFactor;
		MaterialDefinitionComparer comparer = new MaterialDefinitionComparer();
		List<UMAData.GeneratedMaterial> generatedMaterials;
		List<UMARendererAsset> uniqueRenderers = new List<UMARendererAsset>();
		List<UMAData.GeneratedMaterial> atlassedMaterials = new List<UMAData.GeneratedMaterial>(20);
		Dictionary<GeneratedMaterialLookupKey, UMAData.GeneratedMaterial> generatedMaterialLookup;


		private UMAData.GeneratedMaterial FindOrCreateGeneratedMaterial(UMAMaterial umaMaterial, UMARendererAsset renderer = null)
		{
			if (umaMaterial.materialType == UMAMaterial.MaterialType.Atlas)
			{
				foreach (var atlassedMaterial in atlassedMaterials)
				{
					if (atlassedMaterial.umaMaterial == umaMaterial && atlassedMaterial.rendererAsset == renderer)
					{
						return atlassedMaterial;
					}
					else
					{
						if (atlassedMaterial.umaMaterial.Equals(umaMaterial) && atlassedMaterial.rendererAsset == renderer)
						{
							return atlassedMaterial;
						}
					}
				}
			}

			var res = new UMAData.GeneratedMaterial();
			res.rendererAsset = renderer;
			res.umaMaterial = umaMaterial;
			res.material = UnityEngine.Object.Instantiate(umaMaterial.material) as Material;
			res.material.name = umaMaterial.material.name;
#if UNITY_WEBGL
			res.material.shader = Shader.Find(res.material.shader.name);
#endif
			res.material.CopyPropertiesFromMaterial(umaMaterial.material);
			atlassedMaterials.Add(res);
			generatedMaterials.Add(res);

			return res;
		}

		protected bool IsUVCoordinates(Rect r)
		{
			if (r.width == 0.0f || r.height == 0.0f)
				return false;

			if (r.width <= 1.0f && r.height <= 1.0f)
				return true;
			return false;
		}

		protected Rect ScaleToBase(Rect r, Texture BaseTexture)
		{
			if (!BaseTexture) return r;
			float w = BaseTexture.width;
			float h = BaseTexture.height;

			return new Rect(r.x * w, r.y * h, r.width * w, r.height * h);
		}

		protected void Start()
		{
			if (generatedMaterialLookup == null)
			{
				generatedMaterialLookup = new Dictionary<GeneratedMaterialLookupKey, UMAData.GeneratedMaterial>(20);
			}
			else
			{
				generatedMaterialLookup.Clear();
			}
			backUpTexture = umaData.backUpTextures();
			umaData.CleanTextures();
			generatedMaterials = new List<UMAData.GeneratedMaterial>(20);
			atlassedMaterials.Clear();
			uniqueRenderers.Clear();

			SlotData[] slots = umaData.umaRecipe.slotDataList;

			for (int i = 0; i < slots.Length; i++)
			{
				SlotData slot = slots[i];
				if (slot == null)
					continue; 
				if (slot.Suppressed)
					continue;

				//Keep a running list of unique RendererHashes from our slots
				//Null rendererAsset gets added, which is good, it is the default renderer.
#if UMA_ADDRESSABLES
				bool found = false;
				foreach (var renderer in uniqueRenderers)
				{
					if (renderer == null && slot.rendererAsset == null)
                    {
						found = true;
						break;
                    }
					if (renderer != null && slot.rendererAsset != null && renderer.name == slot.rendererAsset.name)
                    {
						slot.rendererAsset = renderer;
						found = true;
						break;
                    }
				}
				if (!found)
					uniqueRenderers.Add(slot.rendererAsset);
#else
				if (!uniqueRenderers.Contains(slot.rendererAsset))
					uniqueRenderers.Add(slot.rendererAsset);
#endif
					// Let's only add the default overlay if the slot has meshData and NO overlays
					// This should be able to be removed if default overlay/textures are ever added to uma materials...
					if ((slot.asset.meshData != null) && (slot.OverlayCount == 0))
				{
                    if (umaGenerator.defaultOverlaydata != null)
                        slot.AddOverlay(umaGenerator.defaultOverlaydata);
				}

                OverlayData overlay0 = slot.GetOverlay(0);
				if ((slot.material != null) && (overlay0 != null))
				{
					GeneratedMaterialLookupKey lookupKey = new GeneratedMaterialLookupKey
					{
						overlayList = slot.GetOverlayList(),
						rendererAsset = slot.rendererAsset
					};

					UMAData.GeneratedMaterial generatedMaterial;
					if (!generatedMaterialLookup.TryGetValue(lookupKey, out generatedMaterial))
					{
						generatedMaterial = FindOrCreateGeneratedMaterial(slot.material, slot.rendererAsset);
						generatedMaterialLookup.Add(lookupKey, generatedMaterial);
					}

					int validOverlayCount = 0;
					for (int j = 0; j < slot.OverlayCount; j++)
					{
						var overlay = slot.GetOverlay(j);
						if (overlay != null)
						{
							validOverlayCount++;
#if (UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE) && !UNITY_2017_3_OR_NEWER //supported platforms for procedural materials
							if (overlay.isProcedural)
								overlay.GenerateProceduralTextures();
#endif
						}
					}
					UMAData.MaterialFragment tempMaterialDefinition = new UMAData.MaterialFragment();
					tempMaterialDefinition.baseOverlay = new UMAData.textureData();

					tempMaterialDefinition.umaMaterial = slot.material;
					if (overlay0.isEmpty)
					{
						tempMaterialDefinition.isNoTextures = true;
					}
					else
					{
						tempMaterialDefinition.baseOverlay.textureList = overlay0.textureArray;
						tempMaterialDefinition.baseOverlay.alphaTexture = overlay0.alphaMask;
						tempMaterialDefinition.baseOverlay.overlayType = overlay0.overlayType;
						tempMaterialDefinition.baseColor = overlay0.colorData.color;
						tempMaterialDefinition.size = overlay0.pixelCount;

						tempMaterialDefinition.overlays = new UMAData.textureData[validOverlayCount - 1];
						tempMaterialDefinition.overlayColors = new Color32[validOverlayCount - 1];
						tempMaterialDefinition.rects = new Rect[validOverlayCount - 1];
						tempMaterialDefinition.overlayData = new OverlayData[validOverlayCount];
						tempMaterialDefinition.channelMask = new Color[validOverlayCount][];
						tempMaterialDefinition.channelAdditiveMask = new Color[validOverlayCount][];
						tempMaterialDefinition.overlayData[0] = slot.GetOverlay(0);
						tempMaterialDefinition.channelMask[0] = slot.GetOverlay(0).colorData.channelMask;
						tempMaterialDefinition.channelAdditiveMask[0] = slot.GetOverlay(0).colorData.channelAdditiveMask;
					}
					tempMaterialDefinition.slotData = slot;

					int overlayID = 0;
					for (int j = 1; j < slot.OverlayCount; j++)
					{
						OverlayData overlay = slot.GetOverlay(j);
						if (overlay == null)
							continue;

						if (IsUVCoordinates(overlay.rect))
						{
							tempMaterialDefinition.rects[overlayID] = ScaleToBase(overlay.rect, overlay0.textureArray[0]);
							
						}
						else
						{
							tempMaterialDefinition.rects[overlayID] = overlay.rect; // JRRM: Convert here into base overlay coordinates?
						}
						tempMaterialDefinition.overlays[overlayID] = new UMAData.textureData();
						tempMaterialDefinition.overlays[overlayID].textureList = overlay.textureArray;
						tempMaterialDefinition.overlays[overlayID].alphaTexture = overlay.alphaMask;
						tempMaterialDefinition.overlays[overlayID].overlayType = overlay.overlayType;
						tempMaterialDefinition.overlayColors[overlayID] = overlay.colorData.color;

						overlayID++;
						tempMaterialDefinition.overlayData[overlayID] = overlay;
						tempMaterialDefinition.channelMask[overlayID] = overlay.colorData.channelMask;
						tempMaterialDefinition.channelAdditiveMask[overlayID] = overlay.colorData.channelAdditiveMask;
					}

					tempMaterialDefinition.overlayList = lookupKey.overlayList;
					tempMaterialDefinition.isRectShared = false;
					for (int j = 0; j < generatedMaterial.materialFragments.Count; j++)
					{
						if (tempMaterialDefinition.overlayList == generatedMaterial.materialFragments[j].overlayList)
						{
							tempMaterialDefinition.isRectShared = true;
							tempMaterialDefinition.rectFragment = generatedMaterial.materialFragments[j];
							break;
						}
					}
					generatedMaterial.materialFragments.Add(tempMaterialDefinition);
				}
			}

			//****************************************************
			//* Set parameters based on shader parameter mapping
			//****************************************************
			for (int i=0;i<generatedMaterials.Count;i++)
			{
				UMAData.GeneratedMaterial ugm = generatedMaterials[i];
				for (int j = 0; j < ugm.materialFragments.Count; j++)
				{
					UMAData.MaterialFragment matfrag = ugm.materialFragments[j];
					// Shader parameters from the shared colors are only pulled from the first overlay.
					// this is so we don't have conflicting overlays.
					// TODO: if we pulled from all of them, it would be a little slower, but it could allow for us to have overlays override some
					// parameters. 
					if (matfrag.overlayData != null && matfrag.overlayData.Length > 0)
					{
						OverlayData od = matfrag.overlayData[0];
						if (od.colorData.HasProperties)
						{
							foreach (var s in od.colorData.PropertyBlock.shaderProperties)
							{
								if (ugm.material.HasProperty(s.name))
								{
									s.Apply(ugm.material);
								}
							}
						}
					}
				}

				if (ugm.umaMaterial.shaderParms != null)
				{

					// Set Shader properties from shared colors
					for(int j=0;j<ugm.umaMaterial.shaderParms.Length;j++)
					{
						UMAMaterial.ShaderParms parm = ugm.umaMaterial.shaderParms[j];
						if (ugm.material.HasProperty(parm.ParameterName))
						{
							foreach (OverlayColorData ocd in umaData.umaRecipe.sharedColors)
							{
								if (ocd.name == parm.ColorName)
								{
									ugm.material.SetColor(parm.ParameterName, ocd.color);
									break;
								}
							}
						}
					}

				}
			}
			packTexture = new MaxRectsBinPack(umaGenerator.atlasResolution, umaGenerator.atlasResolution, false);
		}

		public class MaterialDefinitionComparer : IComparer<UMAData.MaterialFragment>
		{
			public int Compare(UMAData.MaterialFragment x, UMAData.MaterialFragment y)
			{
				return y.size - x.size;
			}
		}

		public void ProcessTexture(UMAGeneratorBase _umaGenerator, UMAData _umaData, bool updateMaterialList, int InitialScaleFactor)
		{
			umaGenerator = _umaGenerator;
			umaData = _umaData;
			this.updateMaterialList = updateMaterialList;
			scaleFactor = InitialScaleFactor;
			textureProcesser = new TextureProcessPRO();

			Start();

			umaData.generatedMaterials.rendererAssets = uniqueRenderers;
			umaData.generatedMaterials.materials = generatedMaterials;

			GenerateAtlasData();
			OptimizeAtlas();

			textureProcesser.ProcessTexture(_umaData,_umaGenerator);

			CleanBackUpTextures();
			UpdateUV();

			// Procedural textures were done here 
			if (updateMaterialList)
			{
				for (int j = 0; j < umaData.rendererCount; j++)
				{
					var renderer = umaData.GetRenderer(j);
					var mats = renderer.sharedMaterials;
					var newMats = new Material[mats.Length];
					var atlasses = umaData.generatedMaterials.materials;	
					int materialIndex = 0;
					for (int i = 0; i < atlasses.Count; i++)
					{
						if (atlasses[i].rendererAsset == umaData.GetRendererAsset(j))
						{
							UMAUtils.DestroySceneObject(mats[materialIndex]);
							newMats[materialIndex] = atlasses[i].material;
							atlasses[i].skinnedMeshRenderer = renderer;
							atlasses[i].materialIndex = materialIndex;
							materialIndex++;
						}
					}
					renderer.sharedMaterials = newMats;
				}
			}
		}


		private void CleanBackUpTextures()
		{
			for (int textureIndex = 0; textureIndex < backUpTexture.Length; textureIndex++)
			{
				if (backUpTexture[textureIndex] != null)
				{
					Texture tempTexture = backUpTexture[textureIndex];
					if (tempTexture is RenderTexture)
					{
						RenderTexture tempRenderTexture = tempTexture as RenderTexture;
						tempRenderTexture.Release();
						UMAUtils.DestroySceneObject(tempRenderTexture);
						tempRenderTexture = null;
					}
					else
					{
						UMAUtils.DestroySceneObject(tempTexture);
					}
					backUpTexture[textureIndex] = null;
				}
			}
		}



		/// <summary>
		/// JRRM - this is where we calculate the atlas rectangles.
		/// </summary>
		private void GenerateAtlasData()
		{
			SizeInt area = new SizeInt();
			float atlasRes = umaGenerator.atlasResolution;
			Vector2 StartScale = Vector2.one / (float)scaleFactor;
			Vector2 Scale = Vector2.one;
			bool scaled = false;

			for (int i = 0; i < atlassedMaterials.Count; i++)
			{
				var generatedMaterial = atlassedMaterials[i];
				if (generatedMaterial.umaMaterial.channels == null || generatedMaterial.umaMaterial.channels.Length == 0)
					continue;
				
				
				area.Width = umaGenerator.atlasResolution;
				area.Height = umaGenerator.atlasResolution;

				generatedMaterial.materialFragments.Sort(comparer);
				generatedMaterial.resolutionScale = StartScale;
				generatedMaterial.cropResolution = new Vector2(atlasRes, atlasRes);

				// We need a better method than this.
				// if "BestFitSquare"
				switch (umaGenerator.AtlasOverflowFitMethod)
				{
                    case UMAGeneratorBase.FitMethod.BestFitSquare:
                        scaled = CalculateBestFitSquare(area, atlasRes, ref Scale, generatedMaterial);
                        break;
                    default: // Shrink Textures
						while (!CalculateRects(generatedMaterial, area).success)
						{
							generatedMaterial.resolutionScale = generatedMaterial.resolutionScale * umaGenerator.FitPercentageDecrease;
						}
						break;
				}

				UpdateSharedRect(generatedMaterial);
				if (scaled)
				{
					generatedMaterial.resolutionScale = Scale * StartScale; 
					UpdateAtlasRects(generatedMaterial, Scale);
				}
			}
		}

        private bool CalculateBestFitSquare(SizeInt area, float atlasRes, ref Vector2 Scale, UMAData.GeneratedMaterial generatedMaterial)
        {
            while (true)
            {
                PackSize lastRect = 
					
					
					CalculateRects(generatedMaterial, area);
                if (lastRect.success)
                {
                    if (area.Width != umaGenerator.atlasResolution || area.Height != umaGenerator.atlasResolution)
                    {
						float neww = lastRect.xMax; // was area.Width;
						float newh = lastRect.yMax; // was area.Height;

                        Scale.x = atlasRes / neww;
                        Scale.y = atlasRes / newh;
						return true;
                    }
                    return false; // Everything fit, let's leave.
                }

                int projectedWidth = lastRect.xMax + lastRect.Width;
                int projectedHeight = lastRect.yMax + lastRect.Height;


				if (area.Width < area.Height)
                {
					// increase width.
					if (projectedWidth <= area.Width)
					{
						// If projectedWidth < the area width, then it's not
						// increasing each loop, and it MUST.
						area.Width += lastRect.Width;
					}
					else
					{
						area.Width = projectedWidth;
					}
				}
				else
                {
					// increase height
					if (projectedHeight <= area.Height)
					{
						area.Height += lastRect.Height;
					}
					else
					{
						area.Height = projectedHeight;
					}
				}
            }
			// no exit here! :)
        }

        private void UpdateAtlasRects(UMAData.GeneratedMaterial generatedMaterial, Vector2 Scale)
		{
			for (int i = 0; i < generatedMaterial.materialFragments.Count; i++)
			{
				var fragment = generatedMaterial.materialFragments[i];
				Vector2 pos = fragment.atlasRegion.position * Scale; //ceil ?
				Vector2 size = fragment.atlasRegion.size *= Scale; // floor ?
				pos.x = Mathf.Ceil(pos.x);
				pos.y = Mathf.Ceil(pos.y);
				size.x = Mathf.Floor(size.x);
				size.y = Mathf.Floor(size.y);
				fragment.atlasRegion.Set(pos.x, pos.y, size.x, size.y);
			}
		}


		private void UpdateSharedRect(UMAData.GeneratedMaterial generatedMaterial)
		{
			for (int i = 0; i < generatedMaterial.materialFragments.Count; i++)
			{
				var fragment = generatedMaterial.materialFragments[i];
				if (fragment.isRectShared)
				{
					fragment.atlasRegion = fragment.rectFragment.atlasRegion;
				}
			}
		}


		// TODO:
		// Calculate the smallest square to hold rectangles.

		private PackSize CalculateRects(UMAData.GeneratedMaterial material, SizeInt area)
		{
			Rect nullRect = new Rect(0, 0, 0, 0);
			PackSize lastPackSize = new PackSize();

			packTexture.Init(area.Width, area.Height, false);

			for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
			{
				var tempMaterialDef = material.materialFragments[atlasElementIndex];
				if (tempMaterialDef.isRectShared)
					continue;
				if (tempMaterialDef.isNoTextures)
					continue;

				int width = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].width * material.resolutionScale.x * tempMaterialDef.slotData.overlayScale);
				int height = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].height * material.resolutionScale.y * tempMaterialDef.slotData.overlayScale);

				// If either width or height are 0 we will end up with nullRect and potentially loop forever
				if (width == 0 || height == 0)
				{
					tempMaterialDef.atlasRegion = nullRect;
					continue;
				}

				tempMaterialDef.atlasRegion = packTexture.Insert(width, height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit);
				lastPackSize.Width = width;
				lastPackSize.Height = height;
				if (tempMaterialDef.atlasRegion.xMax > lastPackSize.xMax)
					lastPackSize.xMax = (int)tempMaterialDef.atlasRegion.xMax;

				if (tempMaterialDef.atlasRegion.yMax > lastPackSize.yMax)
					lastPackSize.yMax = (int)tempMaterialDef.atlasRegion.yMax;



				if (tempMaterialDef.atlasRegion == nullRect)
				{
					if (umaGenerator.fitAtlas)
					{
						//if (Debug.isDebugBuild) // JRRM : re-enable this
						//	Debug.LogWarning("Atlas resolution is too small, Textures will be reduced.", umaData.gameObject);
						lastPackSize.success = false;
						return lastPackSize;
					}
					else
					{
						if (Debug.isDebugBuild)
							Debug.LogError("Atlas resolution is too small, not all textures will fit.", umaData.gameObject);
					}
				}
			}
			lastPackSize.success = true;
			return lastPackSize;
		}

		private bool OldCalculateRects(UMAData.GeneratedMaterial material)
		{
			Rect nullRect = new Rect(0, 0, 0, 0);
			packTexture.Init(umaGenerator.atlasResolution, umaGenerator.atlasResolution, false);

			for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
			{
				var tempMaterialDef = material.materialFragments[atlasElementIndex];
				if (tempMaterialDef.isRectShared)
					continue;

				if (tempMaterialDef.isNoTextures)
					continue;
		
				int width = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].width * material.resolutionScale.x * tempMaterialDef.slotData.overlayScale);
				int height = Mathf.FloorToInt(tempMaterialDef.baseOverlay.textureList[0].height * material.resolutionScale.y * tempMaterialDef.slotData.overlayScale);
				
				// If either width or height are 0 we will end up with nullRect and potentially loop forever
				if (width == 0 || height == 0) 
				{
					tempMaterialDef.atlasRegion = nullRect;
					continue;
				}
				
				tempMaterialDef.atlasRegion = packTexture.Insert(width, height, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit);

				if (tempMaterialDef.atlasRegion == nullRect)
				{
					if (umaGenerator.fitAtlas)
					{
						//if (Debug.isDebugBuild) // JRRM : re-enable this
						//	Debug.LogWarning("Atlas resolution is too small, Textures will be reduced.", umaData.gameObject);
						return false;
					}
					else
					{
						if (Debug.isDebugBuild)
							Debug.LogError("Atlas resolution is too small, not all textures will fit.", umaData.gameObject);
					}
				}
			}
			return true;
		}

		private void OptimizeAtlas()
		{
			for (int atlasIndex = 0; atlasIndex < atlassedMaterials.Count; atlasIndex++)
			{
				var material = atlassedMaterials[atlasIndex];
				if (material.umaMaterial.IsEmpty)
                {
					continue;
                }
				Vector2 usedArea = new Vector2(0, 0);
				for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
				{
					if (material.materialFragments[atlasElementIndex].atlasRegion.xMax > usedArea.x)
					{
						usedArea.x = material.materialFragments[atlasElementIndex].atlasRegion.xMax;
					}

					if (material.materialFragments[atlasElementIndex].atlasRegion.yMax > usedArea.y)
					{
						usedArea.y = material.materialFragments[atlasElementIndex].atlasRegion.yMax;
					}
				}

				//Headless mode ends up with zero usedArea
				if(Mathf.Approximately( usedArea.x, 0f ) || Mathf.Approximately( usedArea.y, 0f ))
				{
					material.cropResolution = Vector2.zero;
					return;
				}

				Vector2 tempResolution = new Vector2(umaGenerator.atlasResolution, umaGenerator.atlasResolution);

				bool done = false;
				while (!done && Mathf.Abs(usedArea.x) > 0.0001)
				{
					if (tempResolution.x * 0.5f >= usedArea.x)
					{
						tempResolution = new Vector2(tempResolution.x * 0.5f, tempResolution.y);
					}
					else
					{
						done = true;
					}
				}

				done = false;
				while (!done && Mathf.Abs(usedArea.y) > 0.0001)
				{

					if (tempResolution.y * 0.5f >= usedArea.y)
					{
						tempResolution = new Vector2(tempResolution.x, tempResolution.y * 0.5f);
					}
					else
					{
						done = true;
					}
				}

				material.cropResolution = tempResolution;
			}
		}

		private void UpdateUV()
		{
			UMAData.GeneratedMaterials umaAtlasList = umaData.generatedMaterials;

			for (int atlasIndex = 0; atlasIndex < umaAtlasList.materials.Count; atlasIndex++)
			{
				var material = umaAtlasList.materials[atlasIndex];
				if (material.umaMaterial.materialType != UMAMaterial.MaterialType.Atlas)
					continue;

				Vector2 finalAtlasAspect = new Vector2(umaGenerator.atlasResolution / material.cropResolution.x, umaGenerator.atlasResolution / material.cropResolution.y);

				for (int atlasElementIndex = 0; atlasElementIndex < material.materialFragments.Count; atlasElementIndex++)
				{
					Rect tempRect = material.materialFragments[atlasElementIndex].atlasRegion;
					tempRect.xMin = tempRect.xMin * finalAtlasAspect.x;
					tempRect.xMax = tempRect.xMax * finalAtlasAspect.x;
					tempRect.yMin = tempRect.yMin * finalAtlasAspect.y;
					tempRect.yMax = tempRect.yMax * finalAtlasAspect.y;
					material.materialFragments[atlasElementIndex].atlasRegion = tempRect;
				}
			}
		}
	}
}
