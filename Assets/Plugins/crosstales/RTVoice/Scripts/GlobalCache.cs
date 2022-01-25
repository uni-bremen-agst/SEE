using System.Linq;
using UnityEngine;

namespace Crosstales.RTVoice
{
   /// <summary>Global cache for wrappers.</summary>
   [ExecuteInEditMode]
   [DisallowMultipleComponent]
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_global_cache.html")]
   public class GlobalCache : Crosstales.Common.Util.Singleton<GlobalCache>
   {
      #region Variables

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("ClipCacheSize")] [Header("Cache Settings"), Tooltip("Size of the clip cache in MB (default: 256)"), Range(16, 1024), SerializeField]
      private int clipCacheSize = Util.Constants.DEFAULT_CACHE_SIZE_CLIPS;

      [Tooltip("Automatically loads and saves the cache (default: false)"), SerializeField] private bool persistCache;

      ///<summary>Dictionary with all cached clips.</summary>
      public readonly System.Collections.Generic.Dictionary<Model.Wrapper, AudioClip> Clips = new System.Collections.Generic.Dictionary<Model.Wrapper, AudioClip>();

      private readonly System.Collections.Generic.List<Model.Wrapper> clipKeys = new System.Collections.Generic.List<Model.Wrapper>();

      private Transform tf;

      private static string dataStorePath;

      #endregion


      #region Properties

      ///<summary>Size of the clip cache in Bytes.</summary>
      public int ClipCacheSize
      {
         get => clipCacheSize * Util.Constants.FACTOR_MB;
         set => clipCacheSize = Mathf.Clamp(value / Util.Constants.FACTOR_MB, 1, Util.Constants.DEFAULT_MAX_CACHE_SIZE_CLIPS);
      }

      /// <summary>Current size of the clip cache in Bytes.</summary>
      public int CurrentClipCacheSize => Clips.Sum(pair => pair.Value.samples * 2 * 4);

      /// <summary>Automatically loads and saves the cache.</summary>
      public bool PersistCache
      {
         get => persistCache;
         set => persistCache = value;
      }

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         dataStorePath = $"{Application.persistentDataPath}/rtvoice_datastore.xml";

         if (persistCache)
            LoadCache();
      }

      private void OnValidate()
      {
         if (clipCacheSize <= 16)
         {
            clipCacheSize = 16;
         }
         else if (clipCacheSize > Util.Constants.DEFAULT_MAX_CACHE_SIZE_CLIPS)
         {
            clipCacheSize = Util.Constants.DEFAULT_MAX_CACHE_SIZE_CLIPS;
         }
      }

      protected override void OnApplicationQuit()
      {
         if (persistCache)
            SaveCache();

         ClearCache();

         base.OnApplicationQuit();
      }

      #endregion


      #region Public methods

      /// <summary>Resets this object.</summary>
      //[RuntimeInitializeOnLoadMethod]
      public static void ResetObject()
      {
         DeleteInstance();
      }

      /// <summary>Returns the AudioClip for a given key.</summary>
      /// <param name="key">Key for the AudioClip.</param>
      /// <returns>AudioClip for the given key.</returns>
      public AudioClip GetClip(Model.Wrapper key)
      {
         if (key != null)
         {
            Clips.TryGetValue(key, out AudioClip data);

            //Debug.LogWarning("DATA: " + data);
            return data;
         }

         return null;
      }

      /// <summary>Removes an AudioClip for a given key.</summary>
      /// <param name="key">Key for the AudioClip.</param>
      public void RemoveClip(Model.Wrapper key)
      {
         if (key != null && Clips.ContainsKey(key))
         {
            Destroy(Clips[key]);
            Clips.Remove(key);
            clipKeys.Remove(key);
         }
      }

      /// <summary>Adds an AudioClip for a given key.</summary>
      /// <param name="key">Key for the AudioClip.</param>
      /// <param name="data">AudioClip for the key.</param>
      public void AddClip(Model.Wrapper key, AudioClip data)
      {
         if (key != null && data != null && !Clips.ContainsKey(key))
         {
            while (CurrentClipCacheSize >= ClipCacheSize)
            {
               RemoveClip(clipKeys[0]);
            }

            Clips.Add(key, data);
            clipKeys.Add(key);
         }
      }

      /// <summary>Clears the clips cache.</summary>
      public void ClearClipCache()
      {
         Util.Context.NumberOfCachedSpeeches = 0;
         Util.Context.NumberOfNonCachedSpeeches = 0;

         foreach (System.Collections.Generic.KeyValuePair<Model.Wrapper, AudioClip> kvp in Clips)
         {
            Destroy(kvp.Value);
         }

         Clips.Clear();
         clipKeys.Clear();
      }

      /// <summary>Clears the complete cache.</summary>
      public void ClearCache()
      {
         ClearClipCache();
      }

      /// <summary>Saves the complete cache.</summary>
      public void SaveCache()
      {
         System.Collections.Generic.List<DataStore> data = Clips.Select(kvp => new DataStore(kvp.Key, Crosstales.Common.Audio.WavMaster.FromAudioClip(kvp.Value))).ToList();

         Common.Util.XmlHelper.SerializeToFile(data, dataStorePath);

         Debug.Log("SaveCache: " + data.Count);
      }

      /// <summary>Loads the complete cache.</summary>
      public void LoadCache()
      {
         if (System.IO.File.Exists(dataStorePath))
         {
            System.Collections.Generic.List<DataStore> data = Common.Util.XmlHelper.DeserializeFromFile<System.Collections.Generic.List<DataStore>>(dataStorePath);

            if (data != null)
            {
               foreach (DataStore ds in data)
               {
                  AddClip(ds.wrapper, Crosstales.Common.Audio.WavMaster.ToAudioClip(ds.Data));
               }
            }
         }

         Debug.Log("LoadCache: " + Clips.Count);
      }

      #endregion
   }

   /// <summary>Model for a voice.</summary>
   [System.Serializable]
   public class DataStore
   {
      public Model.Wrapper wrapper;
      public byte[] Data;

      /// <summary>Default.</summary>
      public DataStore()
      {
      }

      /// <summary>Instantiate the class.</summary>
      /// <param name="wrapper">Wrapper of the speech.</param>
      /// <param name="data">Data of the speech.</param>
      public DataStore(Model.Wrapper wrapper, byte[] data)
      {
         this.wrapper = wrapper;
         Data = data;
      }
   }
}
// © 2020-2021 crosstales LLC (https://www.crosstales.com)