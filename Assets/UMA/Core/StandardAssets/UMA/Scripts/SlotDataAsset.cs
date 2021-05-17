#if UNITY_EDITOR
using System.Text;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace UMA
{
	/// <summary>
	/// Contains the immutable data shared between slots of the same type.
	/// </summary>
	[System.Serializable]
	[PreferBinarySerialization] 
	public partial class SlotDataAsset : ScriptableObject, ISerializationCallbackReceiver, INameProvider
    {
		public string slotName;
		[System.NonSerialized]
		public int nameHash;

#if UNITY_EDITOR
		private StringBuilder errorBuilder  = new StringBuilder();


		public bool HasErrors
        {
			get
            {
				return (!string.IsNullOrEmpty(Errors));
            }
        }
		public string Errors;

		/// <summary>
		/// Returns true if meshdata is valid or null (a utility slot).
		/// </summary>
		/// <returns></returns>
		public bool ValidateMeshData()
        {
			Errors = "";
			errorBuilder.Clear();

			if (meshData == null)
            {
				return true;
            }
			if (material == null)
            {
				AddError("material is null. A valid UMAMaterial that matches the overlay should be assigned.");
            }
			Errors = meshData.Validate();
			return true;
        }

        private void AddError(string v)
        {
			if (errorBuilder.Length == 0)
			{
				errorBuilder.Append(v);
			}
			else
            {
				errorBuilder.Append("; ");
				errorBuilder.Append(v);
            }
        }

        public ReorderableList tagList { get; set; }
		public bool eventsFoldout { get; set; } = false;
#endif

		public UMARendererAsset RendererAsset { get { return _rendererAsset; } }
		[SerializeField] private UMARendererAsset _rendererAsset=null;

        #region INameProvider

        public string GetAssetName()
        {
            return slotName;
        }
        public int GetNameHash()
        {
            return nameHash;
        }

        #endregion
        /// <summary>
        /// The UMA material.
        /// </summary>
        /// <remarks>
        /// The UMA material contains both a reference to the Unity material
        /// used for drawing and information needed for matching the textures
        /// and colors to the various material properties.
        /// </remarks>
        [UMAAssetFieldVisible]
		[SerializeField]
		public UMAMaterial material;

		/// <summary>
		/// materialName is used to save the name of the material, but ONLY if we have cleared the material when building bundles.
		/// You can't count on this field to contain a value unless it was set during the cleanup phase by the indexer!
		/// </summary>
		public string materialName;

		/// <summary>
		/// This SlotDataAsset will not be included after this LOD level.
		/// Set high by default so behavior is the same.
		/// </summary>
		[Tooltip("If you are using an LOD system, this is the maximum LOD that this slot will be displayed. After that, it will be discarded during mesh generation. a value of -1 will never be dropped.")]
		public int maxLOD=-1;

		/// <summary>
		/// 
		/// </summary>
		public bool useAtlasOverlay;

		/// <summary>
		/// Default overlay scale for slots using the asset.
		/// </summary>
		public float overlayScale = 1.0f;
		/// <summary>
		/// The animated bone names.
		/// </summary>
		/// <remarks>
		/// The animated bones array is required for cases where optimizations
		/// could remove transforms from the rig. Animated bones will always
		/// be preserved.
		/// </remarks>
		public string[] animatedBoneNames = new string[0];
		/// <summary>
		/// The animated bone name hashes.
		/// </summary>
		/// <remarks>
		/// The animated bones array is required for cases where optimizations
		/// could remove transforms from the rig. Animated bones will always
		/// be preserved.
		/// </remarks>
		[UnityEngine.HideInInspector]
		public int[] animatedBoneHashes = new int[0];

#pragma warning disable 649
		//UMA2.8+ we need to use DNAConverterField now because that can contain Behaviours and the new controllers
		//we need this because we need the old data out of it on deserialize
		/// <summary>
		/// Optional DNA converter specific to the slot.
		/// </summary>
		[FormerlySerializedAs("slotDNA")]
		[SerializeField]
		private DnaConverterBehaviour _slotDNALegacy;
#pragma warning restore 649

		//UMA 2.8 FixDNAPrefabs: this is a new field that can take DNAConverter Prefabs *and* DNAConverterControllers
		[SerializeField]
		[Tooltip("Optional DNA converter specific to the slot. Accepts a DNAConverterController asset or a legacy DNAConverterBehaviour prefab.")]
		private DNAConverterField _slotDNA = new DNAConverterField();

		[Tooltip("If isWildCardSlot = true, then the overlays on this slot are applied to any slot or overlay with a matching tag when the recipe is built. This is used in Wardrobe Recipes to apply overlays to other slots.")]
		public bool isWildCardSlot;

		//UMA 2.8 FixDNAPrefabs: I'm putting the required property for this here because theres no properties anywhere else!
		public IDNAConverter slotDNA
		{
			get { return _slotDNA.Value; }
			set { _slotDNA.Value = value; }
		}

		public bool isUtilitySlot
		{
			get
			{
				if (meshData != null || meshData.vertexCount > 0) return false;

				if (material == null) return true;
				if (CharacterBegun != null && CharacterBegun.GetPersistentEventCount() > 0) return true;
				if (SlotAtlassed != null && SlotAtlassed.GetPersistentEventCount() > 0) return true;
				if (DNAApplied != null && DNAApplied.GetPersistentEventCount() > 0) return true;
				if (CharacterCompleted != null && CharacterCompleted.GetPersistentEventCount() > 0) return true;

				return false;
			}
		}

		public void LoadFromIndex()
        {
			material = UMAAssetIndexer.Instance.GetAsset<UMAMaterial>(materialName);
        }

		//UMA 2.8 FixDNAPrefabs: Swaps the legacy converter (DnaConverterBehaviour Prefab) for the new DNAConverterController
		/// <summary>
		/// Replaces a legacy DnaConverterBehaviour Prefab with a new DynamicDNAConverterController
		/// </summary>
		/// <returns>returns true if any converters were replaced.</returns>
		public bool UpgradeFromLegacy(DnaConverterBehaviour oldConverter, DynamicDNAConverterController newConverter)
		{
			if (_slotDNA.Value as Object == oldConverter)//Not sure why I am being told by visualStudio to cast the left side to Object here...
			{
				_slotDNA.Value = newConverter;
				return true;
			}
			return false;
		}

		/// <summary>
		/// The mesh data.
		/// </summary>
		/// <remarks>
		/// The UMAMeshData contains all of the Unity mesh data and additional
		/// information needed for mesh manipulation while minimizing overhead
		/// from accessing Unity's managed memory.
		/// </remarks>
		public UMAMeshData meshData;
		public int subMeshIndex;
		/// <summary>
		/// Use this to identify slots that serves the same purpose
		/// Eg. ChestArmor, Helmet, etc.
		/// </summary>
		public string slotGroup;
		/// <summary>
		/// This can be used for hiding, matching etc. 
		/// It's used by the DynamicCharacterSystem to hide slots by tag.
		/// </summary>
		public string[] tags;

		// Wildcard slot race matches
		public string[] Races;

		/// <summary>
		/// Callback event when character update begins.
		/// </summary>
		public UMADataEvent CharacterBegun;
		/// <summary>
		/// Callback event when slot overlays are atlased.
		/// </summary>
		public UMADataSlotMaterialRectEvent SlotAtlassed;
		/// <summary>
		/// Callback event when character DNA is applied.
		/// </summary>
		public UMADataEvent DNAApplied;
		/// <summary>
		/// Callback event when character update is complete.
		/// </summary>
		public UMADataEvent CharacterCompleted;


		/// <summary>
		/// This slot was auto generated as a LOD slot based on another slot.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		public bool autoGeneratedLOD;

		public SlotDataAsset()
		{
            
		}

        public void OnDestroy()
        {
			meshData.FreeBoneWeights();
        }

		public void OnDisable()
		{
			meshData.FreeBoneWeights();
		}

		public int GetTextureChannelCount(UMAGeneratorBase generator)
		{
			return material.channels.Length;
		}
        
		public override string ToString()
		{
			return "SlotData: " + slotName;
		}

        public void UpdateMeshData(SkinnedMeshRenderer meshRenderer, string rootBoneName)
        {
            meshData = new UMAMeshData();
            meshData.RootBoneName = rootBoneName;
            meshData.RetrieveDataFromUnityMesh(meshRenderer);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void UpdateMeshData(SkinnedMeshRenderer meshRenderer)
		{
			meshData = new UMAMeshData();
			meshData.RetrieveDataFromUnityMesh(meshRenderer);
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}


        public void OnEnable()
        {
			if (meshData == null)
				return;

			if (meshData.LoadedBoneweights)
            {
				// already loaded. just return.
				return;
            }
			if (meshData.ManagedBoneWeights != null && meshData.ManagedBoneWeights.Length > 0)
            {
				meshData.LoadVariableBoneWeights();
            }
			else if (meshData.boneWeights != null && meshData.boneWeights.Length > 0)
			{
				meshData.LoadBoneWeights();
			}
		}

        public void UpdateMeshData()
		{
		}

		public void OnAfterDeserialize()
		{
			nameHash = UMAUtils.StringToHash(slotName);

			//UMA 2.8 FixDNAPrefabs: Automatically update the data from the old field to the new one
			if (_slotDNALegacy != null && _slotDNA.Value == null)
			{
				_slotDNA.Value = _slotDNALegacy;
				//Clear the legacy field?
			}
		}

		public void OnBeforeSerialize() 
		{ 

		}

		public void Assign(SlotDataAsset source)
		{
			slotName = source.slotName;
			nameHash = source.nameHash;
			material = source.material;
			overlayScale = source.overlayScale;
			animatedBoneNames = source.animatedBoneNames;
			animatedBoneHashes = source.animatedBoneHashes;
			meshData = source.meshData;
			subMeshIndex = source.subMeshIndex;
			slotGroup = source.slotGroup;
			tags = source.tags;
			Races = source.Races;
		}
	}
}
