using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base-class for all singletons.</summary>
   [DisallowMultipleComponent]
   public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
   {
      #region Variables

      [Tooltip("Don't destroy gameobject during scene switches (default: false)."), SerializeField] private bool dontDestroy = true;

      /// <summary>Fully qualified prefab path.</summary>
      public static string PrefabPath;

      /// <summary>Name of the gameobject in the scene.</summary>
      public static string GameObjectName = typeof(T).Name;

      protected static T instance;
      private static readonly object lockObj = new object();

      #endregion


      #region Properties

      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static T Instance
      {
         get
         {
            //if (!BaseHelper.isEditorMode && SingletonHelper.isQuitting) 
            if (SingletonHelper.isQuitting)
            {
               //Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
               return instance;
            }

            if (instance == null)
               CreateInstance();

            //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' GET: {instance.GetInstanceID()}");
            return instance;
         }

         protected set
         {
            lock (lockObj)
            {
               //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' SET: {value?.GetInstanceID()}");
               instance = value;
            }
         }
      }

      /// <summary>Don't destroy gameobject during scene switches.</summary>
      public bool DontDestroy
      {
         get => dontDestroy;
         set => dontDestroy = value;
      }

      #endregion


      #region MonoBehaviour methods

      protected virtual void Awake()
      {
         //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' AWAKE: {activeInstance.GetInstanceID()}");

         Util.BaseHelper.ApplicationIsPlaying = Application.isPlaying; //needed to enforce the right mode
         //isQuitting = false;

         if (instance == null)
         {
            Instance = GetComponent<T>();

            if (!Util.BaseHelper.isEditorMode && dontDestroy)
               DontDestroyOnLoad(transform.root.gameObject);
         }
         else
         {
            if (!Util.BaseHelper.isEditorMode && dontDestroy && instance != this)
            {
               Debug.LogWarning($"Only one active instance of '{typeof(T).Name}' allowed in all scenes!{System.Environment.NewLine}This object will now be destroyed.", this);
               Destroy(gameObject, 0.1f);
            }
         }
      }

      protected virtual void OnDestroy()
      {
         if (instance == this)
         {
            //SingletonHelper.isQuitting = !BaseHelper.isEditorMode;
            //SingletonHelper.isQuitting = true;

            //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' ONDESTROY: {instance.GetInstanceID()}");

            if (!dontDestroy)
               Instance = null;
            //DeleteInstance();
         }
      }

      protected virtual void OnApplicationQuit()
      {
         SingletonHelper.isQuitting = true;

         Util.BaseHelper.ApplicationIsPlaying = false;
      }

      #endregion


      /// <summary>Creates an instance of this object.</summary>
      /// <param name="searchExistingGameObject">Search for existing GameObjects of this object (default: true, optional)</param>
      /// <param name="deleteExistingInstance">Delete existing instance of this object (default: false, optional)</param>
      public static void CreateInstance(bool searchExistingGameObject = true, bool deleteExistingInstance = false)
      {
         if (deleteExistingInstance)
            DeleteInstance();

         if (instance == null)
         {
            // Search for existing instance.
            if (searchExistingGameObject)
               Instance = (T)FindObjectOfType(typeof(T));

            // Create new instance if one doesn't already exist.
            if (instance == null)
            {
               if (!string.IsNullOrEmpty(PrefabPath))
               {
                  T prefab = Resources.Load<T>(PrefabPath);

                  if (prefab == null)
                  {
                     Debug.LogWarning("Singleton prefab missing: " + PrefabPath);
                  }
                  else
                  {
                     Instance = Instantiate(prefab);

                     Instance.name = GameObjectName;
                     //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' CREATE Prefab: {instance.GetInstanceID()}");
                  }
               }

               if (instance == null)
               {
                  if (BaseHelper.isEditorMode)
                  {
#if UNITY_EDITOR
                     //instanceEditor = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags($"{typeof(T).Name} (Hidden Singleton)", HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy).AddComponent<T>();
                     Instance = new GameObject(GameObjectName).AddComponent<T>();
#endif
                     //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' CREATE Editor: {instance.GetInstanceID()} - {BaseHelper.isEditorMode}");
                  }
                  else
                  {
                     Instance = new GameObject(GameObjectName).AddComponent<T>();

                     //Debug.LogWarning($"{Time.realtimeSinceStartup}-[Singleton] Instance '{typeof(T)}' CREATE Play: {instance.GetInstanceID()} - {BaseHelper.isEditorMode}");
                  }
               }
            }
         }
      }

      /// <summary>Deletes the instance of this object.</summary>
      public static void DeleteInstance()
      {
         if (instance != null)
         {
            T _instance = instance;

            Instance = null;

            Destroy(_instance.gameObject);
         }
      }
   }

   /// <summary>Helper-class for singletons.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoad]
#endif
   public class SingletonHelper
   {
#if UNITY_EDITOR
      private const string key = "CT_SINGLETON_ISQUITTING";

      private static bool quitting;
      private static bool isQuittingSet;

      public static bool isQuitting
      {
         get
         {
            //if (BaseHelper.isEditorMode)
            //   return false;

            if (!isQuittingSet)
            {
               isQuittingSet = true;

               quitting = UnityEditor.BuildPipeline.isBuildingPlayer || Crosstales.Common.Util.CTPlayerPrefs.GetBool(key);
            }

            return quitting;
         }

         set
         {
            //Debug.Log("SET isQuitting: " + value);

            if (value != quitting)
            {
               quitting = value;

               Crosstales.Common.Util.CTPlayerPrefs.SetBool(key, value);
               Crosstales.Common.Util.CTPlayerPrefs.Save();
            }
         }
      }
#else
      public static bool isQuitting { get; set; } = false;
#endif

      private static bool isInitialized;

      #region Constructor

      static SingletonHelper()
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - Constructor!");

         initialize();
      }

      #endregion


      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void initialize()
      {
         if (isInitialized)
         {
            //Debug.Log($"{Time.realtimeSinceStartup} - already initialized!");
         }
         else
         {
            //Debug.Log($"{Time.realtimeSinceStartup} - initialize: {isQuitting}");

            isInitialized = true;

            Application.quitting += onQuitting;
            //Application.wantsToQuit += onWantsToQuit;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += onSceneUnloaded;
			
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += onPlayModeStateChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += onSceneClosing;
            //UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += onSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += onSceneOpened;
#endif
         }
      }

      private static void onSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - onSceneLoaded");

         isQuitting = false;
         Util.BaseHelper.ApplicationIsPlaying = true;
      }

      private static void onSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - onSceneUnloaded");

         isQuitting = true;
         Util.BaseHelper.ApplicationIsPlaying = false;
      }

#if UNITY_EDITOR
      private static void onPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
      {
         isQuitting = obj == UnityEditor.PlayModeStateChange.ExitingEditMode || obj == UnityEditor.PlayModeStateChange.ExitingPlayMode;

         //Debug.LogWarning($"{Time.realtimeSinceStartup} - onPlayModeStateChanged: {obj} - {isQuitting}");
      }

      private static void onSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingscene)
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - onSceneClosing");

         isQuitting = true;
      }
/*
   private static void onSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
   {
      Debug.Log($"{Time.realtimeSinceStartup} - onSceneOpening");

      isQuitting = false;
   }
*/
      private static void onSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - onSceneOpened");

         isQuitting = false;
      }
#endif

/*
   private static bool onWantsToQuit()
   {
      Debug.Log($"{Time.realtimeSinceStartup} - onWantsToQuit");

      isQuitting = true;
      return true;
   }
*/
      private static void onQuitting()
      {
         //Debug.Log($"{Time.realtimeSinceStartup} - onQuitting");

         isQuitting = true;
      }
   }
}
// © 2020-2021 crosstales LLC (https://www.crosstales.com)