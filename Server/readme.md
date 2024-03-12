
# How to run:

- Vorbereitung: 
Um das Projekt zu starten, wird Docker benötigt. Docker kann unter Linux mit den meisten Paketmanagern heruntergeladen werden, unter Windows kann man Docker über https://www.docker.com/ herunterladen und installieren.

- Nachdem Docker installiert ist, muss man in den Ordner Gameserver-Linux über das Terminal navigieren und dort den Befehl ```docker build -t see-gameserver .``` ausführen.

- Nachdem der Befehl ausgeführt wurde, kann man über das Terminal zurück in den übergeordneten Ordner wechseln (in dem auch das ```compose.yaml``` liegt) und dort den Befehl ```docker-compose up --build``` ausführen. Damit sollen alle Dienste für den Betrieb gestartet werden.

- Sobald alle Dienste gestartet sind, kann man über den Webbrowser mit dem Link http://localhost/ auf die Verwaltungsübersicht zugreifen.

- Hier kann man sich nun mit dem Benutzernamen "thorsten" und dem Passwort "password" anmelden.

- Bei verbinden mit einem Server, kann der aktuelle Port mithilfe des "IP Teilen" Knopfes gefunden werden.

- Dieser muss beim Starten von SEE in das Feld "Server UDP Port" eingefügt werden.

# Orderstruktur
```
│   compose.yaml
│   readme.md
│
├───Bachelorarbeit
│   │   bachelorarbeit.pdf
│   │   data.xlsx
│   │
│   └───Bilder
│           app_db.png
│           architektur.png
│           backend-layer.png
│           backend-test-server.png
│           backend-test-user.png
│           communication-now.png
│           communication-then-receiving.png
│           communication-then-sending.png
│           create_server.png
│           generated_avatar.PNG
│           github_avatar.png
│           grading-scale.PNG
│           graph-abschluss.PNG
│           graph-age.PNG
│           graph-environment.PNG
│           graph-experience.PNG
│           graph-sus.PNG
│           graph-szenarios.PNG
│           graph-visuals.PNG
│           login.png
│           overview.png
│           personalsettings.png
│           see.png
│           server.png
│           settings.png
│           unib_logo.png
│
├───Backend
│   │   .dockerignore
│   │   .gitignore
│   │   compose.yaml
│   │   Dockerfile
│   │   HELP.md
│   │   mvnw
│   │   mvnw.cmd
│   │   pom.xml
│   │
│   ├───.idea
│   │       .gitignore
│   │       compiler.xml
│   │       encodings.xml
│   │       jarRepositories.xml
│   │       jpa-buddy.xml
│   │       misc.xml
│   │       uiDesigner.xml
│   │       vcs.xml
│   │
│   └───src
│       ├───main
│       │   ├───java
│       │   │   └───uni
│       │   │       └───bachelorprojekt
│       │   │           └───see
│       │   │               │   SeeApplication.java
│       │   │               │
│       │   │               ├───controller
│       │   │               │   ├───file
│       │   │               │   │   │   FileController.java
│       │   │               │   │   │
│       │   │               │   │   └───payload
│       │   │               │   │           PayloadFile.java
│       │   │               │   │
│       │   │               │   ├───server
│       │   │               │   │       ServerController.java
│       │   │               │   │
│       │   │               │   └───user
│       │   │               │       │   UserController.java
│       │   │               │       │
│       │   │               │       └───payload
│       │   │               │           │   AuthenticateUser.java
│       │   │               │           │
│       │   │               │           ├───request
│       │   │               │           │       ChangePasswordRequest.java
│       │   │               │           │       ChangeUsernameRequest.java
│       │   │               │           │       LoginRequest.java
│       │   │               │           │       SignupRequest.java
│       │   │               │           │
│       │   │               │           └───response
│       │   │               │                   MessageResponse.java
│       │   │               │                   UserInfoResponse.java
│       │   │               │
│       │   │               ├───model
│       │   │               │       ERole.java
│       │   │               │       File.java
│       │   │               │       Role.java
│       │   │               │       Server.java
│       │   │               │       ServerConfig.java
│       │   │               │       User.java
│       │   │               │
│       │   │               ├───repo
│       │   │               │       FileRepo.java
│       │   │               │       RoleRepository.java
│       │   │               │       ServerConfigRepo.java
│       │   │               │       ServerRepo.java
│       │   │               │       UserRepo.java
│       │   │               │
│       │   │               ├───security
│       │   │               │   │   WebConfig.java
│       │   │               │   │   WebSecurityConfig.java
│       │   │               │   │
│       │   │               │   ├───jwt
│       │   │               │   │       AuthEntryPointJwt.java
│       │   │               │   │       AuthTokenFilter.java
│       │   │               │   │       JwtUtils.java
│       │   │               │   │
│       │   │               │   └───services
│       │   │               │           UserDetailsImpl.java
│       │   │               │           UserDetailsServiceImpl.java
│       │   │               │
│       │   │               ├───service
│       │   │               │       ContainerService.java
│       │   │               │       FileService.java
│       │   │               │       ServerService.java
│       │   │               │       UserService.java
│       │   │               │
│       │   │               └───Util
│       │   │                       FileType.java
│       │   │                       ServerStatusType.java
│       │   │
│       │   └───resources
│       │           application.properties
│       │
│       └───test
│           └───java
│               └───uni
│                   └───bachelorprojekt
│                       └───see
│                           │   SeeApplicationTests.java
│                           │
│                           └───security
│                               └───jwt
│                                       JwtUtilsTest.java
│
├───Frontend
│   │   .dockerignore
│   │   .env
│   │   .eslintrc.cjs
│   │   .gitignore
│   │   Dockerfile
│   │   index.html
│   │   package.json
│   │   pnpm-lock.yaml
│   │   README.md
│   │   tsconfig.json
│   │   tsconfig.node.json
│   │   vite.config.ts
│   │
│   ├───public
│   │       android-chrome-192x192.png
│   │       apple-touch-icon.png
│   │       browserconfig.xml
│   │       favicon-16x16.png
│   │       favicon-32x32.png
│   │       favicon.ico
│   │       mstile-150x150.png
│   │       safari-pinned-tab.svg
│   │       site.webmanifest
│   │
│   └───src
│       │   App.tsx
│       │   index.css
│       │   main.tsx
│       │   Router.tsx
│       │   vite-env.d.ts
│       │
│       ├───components
│       │       Avatar.tsx
│       │       Header.tsx
│       │       LoginForm.tsx
│       │       ServerList.tsx
│       │       ServerListItem.tsx
│       │
│       ├───contexts
│       │       AuthContext.tsx
│       │
│       ├───img
│       │       see-logo.png
│       │
│       ├───types
│       │       File.tsx
│       │       Organisation.tsx
│       │       Role.tsx
│       │       Server.tsx
│       │       User.tsx
│       │
│       └───views
│               CreateServerView.tsx
│               HomeView.tsx
│               LoginView.tsx
│               PersonalSettingsView.tsx
│               ServerView.tsx
│               SettingsView.tsx
│
├───Gameserver-Linux
│   │   Dockerfile
│   │   gameserver.x86_64
│   │   UnityPlayer.so
│   │
│   └───gameserver_Data
│       │   app.info
│       │   boot.config
│       │   globalgamemanagers
│       │   globalgamemanagers.assets
│       │   level0
│       │   level1
│       │   level2
│       │   resources.assets
│       │   resources.resource
│       │   RuntimeInitializeOnLoads.json
│       │   ScriptingAssemblies.json
│       │   sharedassets0.assets
│       │   sharedassets0.resource
│       │   sharedassets1.assets
│       │   sharedassets2.assets
│       │
│       ├───Managed
│       │       AmplifyShaderEditor.Samples.BuiltIn.dll
│       │       Antlr4.Runtime.Standard.dll
│       │       Assembly-CSharp-firstpass.dll
│       │       Autodesk.Fbx.dll
│       │       CurvedUI.dll
│       │       DemiLib.dll
│       │       DissonanceVoip.dll
│       │       DOTween.dll
│       │       DOTweenModules.dll
│       │       DOTweenPro.dll
│       │       DynamicPanels.Runtime.dll
│       │       EnoxSoftware.DlibFaceLandmarkDetector.dll
│       │       EnoxSoftware.OpenCVForUnity.dll
│       │       FaceMaskExample.dll
│       │       FbxBuildTestAssets.dll
│       │       FinalIK.dll
│       │       FinalIKBaker.dll
│       │       FinalIKShared.dll
│       │       FinalIKSharedDemoAssets.dll
│       │       FinalIK_UMA2.dll
│       │       FuzzySharp.dll
│       │       HighlightPlus.dll
│       │       HighlightPlusDemo.dll
│       │       HSVPicker.dll
│       │       HtmlAgilityPack.dll
│       │       InControl.dll
│       │       InControl.Examples.dll
│       │       Joveler.Compression.XZ.dll
│       │       Joveler.DynLoader.dll
│       │       MessagePack.dll
│       │       ModernUIPack.dll
│       │       Mono.Security.dll
│       │       mscorlib.dll
│       │       NaughtyAttributes.Core.dll
│       │       netstandard.dll
│       │       Newtonsoft.Json.dll
│       │       OEBIF.dll
│       │       OpenAI.dll
│       │       Parser.dll
│       │       RTG.dll
│       │       RTGDemoScenes.dll
│       │       RTGTutorials.dll
│       │       RTVoice.dll
│       │       SALSA-LipSync.dll
│       │       SALSAExamples.dll
│       │       SALSALipSyncOneClickRuntimes.dll
│       │       SALSAUMAAddons.dll
│       │       SEE.dll
│       │       SimpleFileBrowser.Runtime.dll
│       │       Sirenix.OdinInspector.Attributes.dll
│       │       Sirenix.Serialization.Config.dll
│       │       Sirenix.Serialization.dll
│       │       Sirenix.Utilities.dll
│       │       StreamRpc.dll
│       │       System.Buffers.dll
│       │       System.ComponentModel.Composition.dll
│       │       System.Configuration.dll
│       │       System.Core.dll
│       │       System.Data.DataSetExtensions.dll
│       │       System.Data.dll
│       │       System.dll
│       │       System.Drawing.dll
│       │       System.EnterpriseServices.dll
│       │       System.IO.Compression.dll
│       │       System.IO.Compression.FileSystem.dll
│       │       System.IO.Pipelines.dll
│       │       System.Memory.dll
│       │       System.Net.Http.dll
│       │       System.Numerics.dll
│       │       System.Runtime.CompilerServices.Unsafe.dll
│       │       System.Runtime.dll
│       │       System.Runtime.Serialization.dll
│       │       System.Security.dll
│       │       System.ServiceModel.Internals.dll
│       │       System.Threading.Tasks.Extensions.dll
│       │       System.Transactions.dll
│       │       System.Xml.dll
│       │       System.Xml.Linq.dll
│       │       TinySpline.dll
│       │       Tubular.dll
│       │       UMA_Content.dll
│       │       UMA_Core.dll
│       │       UMA_Examples.dll
│       │       UniTask.Addressables.dll
│       │       UniTask.dll
│       │       UniTask.DOTween.dll
│       │       UniTask.Linq.dll
│       │       UniTask.TextMeshPro.dll
│       │       Unity.AI.Navigation.dll
│       │       Unity.Burst.dll
│       │       Unity.Burst.Unsafe.dll
│       │       Unity.Collections.dll
│       │       Unity.Collections.LowLevel.ILSupport.dll
│       │       Unity.Formats.Fbx.Runtime.dll
│       │       Unity.InputSystem.dll
│       │       Unity.InputSystem.ForUI.dll
│       │       Unity.Mathematics.dll
│       │       Unity.Netcode.Components.dll
│       │       Unity.Netcode.Runtime.dll
│       │       Unity.Networking.Transport.dll
│       │       Unity.Postprocessing.Runtime.dll
│       │       Unity.RenderPipelines.Core.Runtime.dll
│       │       Unity.RenderPipelines.Core.ShaderLibrary.dll
│       │       Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary.dll
│       │       Unity.TextMeshPro.dll
│       │       Unity.Timeline.dll
│       │       Unity.XR.CoreUtils.dll
│       │       Unity.XR.Interaction.Toolkit.dll
│       │       Unity.XR.Management.dll
│       │       Unity.XR.OpenXR.dll
│       │       Unity.XR.OpenXR.Features.ConformanceAutomation.dll
│       │       Unity.XR.OpenXR.Features.MetaQuestSupport.dll
│       │       Unity.XR.OpenXR.Features.MockRuntime.dll
│       │       Unity.XR.OpenXR.Features.OculusQuestSupport.dll
│       │       Unity.XR.OpenXR.Features.RuntimeDebugger.dll
│       │       UnityEngine.AccessibilityModule.dll
│       │       UnityEngine.AIModule.dll
│       │       UnityEngine.AndroidJNIModule.dll
│       │       UnityEngine.AnimationModule.dll
│       │       UnityEngine.AssetBundleModule.dll
│       │       UnityEngine.AudioModule.dll
│       │       UnityEngine.ClothModule.dll
│       │       UnityEngine.ClusterInputModule.dll
│       │       UnityEngine.ClusterRendererModule.dll
│       │       UnityEngine.ContentLoadModule.dll
│       │       UnityEngine.CoreModule.dll
│       │       UnityEngine.CrashReportingModule.dll
│       │       UnityEngine.DirectorModule.dll
│       │       UnityEngine.dll
│       │       UnityEngine.DSPGraphModule.dll
│       │       UnityEngine.GameCenterModule.dll
│       │       UnityEngine.GIModule.dll
│       │       UnityEngine.GridModule.dll
│       │       UnityEngine.HotReloadModule.dll
│       │       UnityEngine.ImageConversionModule.dll
│       │       UnityEngine.IMGUIModule.dll
│       │       UnityEngine.InputLegacyModule.dll
│       │       UnityEngine.InputModule.dll
│       │       UnityEngine.JSONSerializeModule.dll
│       │       UnityEngine.LocalizationModule.dll
│       │       UnityEngine.ParticleSystemModule.dll
│       │       UnityEngine.PerformanceReportingModule.dll
│       │       UnityEngine.Physics2DModule.dll
│       │       UnityEngine.PhysicsModule.dll
│       │       UnityEngine.ProfilerModule.dll
│       │       UnityEngine.PropertiesModule.dll
│       │       UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule.dll
│       │       UnityEngine.ScreenCaptureModule.dll
│       │       UnityEngine.SharedInternalsModule.dll
│       │       UnityEngine.SpatialTracking.dll
│       │       UnityEngine.SpriteMaskModule.dll
│       │       UnityEngine.SpriteShapeModule.dll
│       │       UnityEngine.StreamingModule.dll
│       │       UnityEngine.SubstanceModule.dll
│       │       UnityEngine.SubsystemsModule.dll
│       │       UnityEngine.TerrainModule.dll
│       │       UnityEngine.TerrainPhysicsModule.dll
│       │       UnityEngine.TextCoreFontEngineModule.dll
│       │       UnityEngine.TextCoreTextEngineModule.dll
│       │       UnityEngine.TextRenderingModule.dll
│       │       UnityEngine.TilemapModule.dll
│       │       UnityEngine.TLSModule.dll
│       │       UnityEngine.UI.dll
│       │       UnityEngine.UIElementsModule.dll
│       │       UnityEngine.UIModule.dll
│       │       UnityEngine.UmbraModule.dll
│       │       UnityEngine.UnityAnalyticsCommonModule.dll
│       │       UnityEngine.UnityAnalyticsModule.dll
│       │       UnityEngine.UnityConnectModule.dll
│       │       UnityEngine.UnityCurlModule.dll
│       │       UnityEngine.UnityTestProtocolModule.dll
│       │       UnityEngine.UnityWebRequestAssetBundleModule.dll
│       │       UnityEngine.UnityWebRequestAudioModule.dll
│       │       UnityEngine.UnityWebRequestModule.dll
│       │       UnityEngine.UnityWebRequestTextureModule.dll
│       │       UnityEngine.UnityWebRequestWWWModule.dll
│       │       UnityEngine.VehiclesModule.dll
│       │       UnityEngine.VFXModule.dll
│       │       UnityEngine.VideoModule.dll
│       │       UnityEngine.VirtualTexturingModule.dll
│       │       UnityEngine.VRModule.dll
│       │       UnityEngine.WindModule.dll
│       │       UnityEngine.XR.LegacyInputHelpers.dll
│       │       UnityEngine.XRModule.dll
│       │       Unity_NFGO.dll
│       │       Utilities.Async.Addressables.dll
│       │       Utilities.Async.dll
│       │       Utilities.Audio.dll
│       │       Utilities.Encoding.Wav.dll
│       │       Utilities.Extensions.Addressables.dll
│       │       Utilities.Extensions.dll
│       │       Utilities.Rest.dll
│       │       ViveSR.dll
│       │       VivoxUnity.dll
│       │
│       ├───MonoBleedingEdge
│       │   ├───etc
│       │   │   │   config
│       │   │   │
│       │   │   └───mono
│       │   │       │   browscap.ini
│       │   │       │   config
│       │   │       │
│       │   │       ├───2.0
│       │   │       │   │   DefaultWsdlHelpGenerator.aspx
│       │   │       │   │   machine.config
│       │   │       │   │   settings.map
│       │   │       │   │   web.config
│       │   │       │   │
│       │   │       │   └───Browsers
│       │   │       │           Compat.browser
│       │   │       │
│       │   │       ├───4.0
│       │   │       │   │   DefaultWsdlHelpGenerator.aspx
│       │   │       │   │   machine.config
│       │   │       │   │   settings.map
│       │   │       │   │   web.config
│       │   │       │   │
│       │   │       │   └───Browsers
│       │   │       │           Compat.browser
│       │   │       │
│       │   │       ├───4.5
│       │   │       │   │   DefaultWsdlHelpGenerator.aspx
│       │   │       │   │   machine.config
│       │   │       │   │   settings.map
│       │   │       │   │   web.config
│       │   │       │   │
│       │   │       │   └───Browsers
│       │   │       │           Compat.browser
│       │   │       │
│       │   │       └───mconfig
│       │   │               config.xml
│       │   │
│       │   └───x86_64
│       │           libmono-native.so
│       │           libmonobdwgc-2.0.so
│       │           libMonoPosixHelper.so
│       │
│       ├───Plugins
│       │       libAudioPluginDissonance.so
│       │       libdlibfacelandmarkdetector.so
│       │       liblzma.so
│       │       libopencvforunity.so
│       │       libopencv_aruco_4_7.so
│       │       libopencv_barcode_4_7.so
│       │       libopencv_bgsegm_4_7.so
│       │       libopencv_bioinspired_4_7.so
│       │       libopencv_calib3d_4_7.so
│       │       libopencv_core_4_7.so
│       │       libopencv_dnn_4_7.so
│       │       libopencv_face_4_7.so
│       │       libopencv_features2d_4_7.so
│       │       libopencv_flann_4_7.so
│       │       libopencv_highgui_4_7.so
│       │       libopencv_imgcodecs_4_7.so
│       │       libopencv_imgproc_4_7.so
│       │       libopencv_img_hash_4_7.so
│       │       libopencv_ml_4_7.so
│       │       libopencv_objdetect_4_7.so
│       │       libopencv_phase_unwrapping_4_7.so
│       │       libopencv_photo_4_7.so
│       │       libopencv_plot_4_7.so
│       │       libopencv_structured_light_4_7.so
│       │       libopencv_text_4_7.so
│       │       libopencv_tracking_4_7.so
│       │       libopencv_videoio_4_7.so
│       │       libopencv_videostab_4_7.so
│       │       libopencv_video_4_7.so
│       │       libopencv_wechat_qrcode_4_7.so
│       │       libopencv_xfeatures2d_4_7.so
│       │       libopencv_ximgproc_4_7.so
│       │       libopencv_xphoto_4_7.so
│       │       libopus.so
│       │       libProcessStart.so
│       │       libtinysplinecsharp.so
│       │       lib_burst_generated.so
│       │
│       ├───Resources
│       │       unity default resources
│       │       UnityPlayer.png
│       │       unity_builtin_extra
│       │
│       ├───StreamingAssets
│       │   │   ESE_Board.json
│       │   │   network.cfg
│       │   │   PersonalAssistantGrammar.grxml
│       │   │
│       │   ├───example
│       │   │       CodeFacts.csv
│       │   │       CodeFacts.gxl
│       │   │
│       │   ├───JLGExample
│       │   │   │   CodeFacts.cfg
│       │   │   │   CodeFacts.gxl.xz
│       │   │   │   CodeFacts.jlg
│       │   │   │   CountConsonants.java
│       │   │   │   CountToAThousand.java
│       │   │   │   CountVowels.java
│       │   │   │   ExecutedLoCLogger.java
│       │   │   │   Main.java
│       │   │   │   Makefile
│       │   │   │
│       │   │   └───Unterordner
│       │   │           UnterOrdnerTest.java
│       │   │
│       │   ├───mini
│       │   │   │   Architecture.csv
│       │   │   │   Architecture.gvl
│       │   │   │   Architecture.gxl
│       │   │   │   build.bat
│       │   │   │   CodeFacts.cfg
│       │   │   │   CodeFacts.csv
│       │   │   │   CodeFacts.gvl
│       │   │   │   CodeFacts.gxl
│       │   │   │   CodeFacts.sld
│       │   │   │   Mapping.gxl
│       │   │   │   mini.cfg
│       │   │   │   reduce.py
│       │   │   │
│       │   │   └───src
│       │   │           C1.cs
│       │   │           C2.cs
│       │   │           MainClass.cs
│       │   │           mini.csproj
│       │   │           mini.sln
│       │   │
│       │   ├───mini-evolution
│       │   │       CodeFacts-1.gxl
│       │   │       CodeFacts-2.gxl
│       │   │       CodeFacts-3.gxl
│       │   │       CodeFacts-4.gxl
│       │   │       CodeFacts-5.gxl
│       │   │       mini-evolution.cfg
│       │   │
│       │   ├───net
│       │   │       CodeFacts.csv
│       │   │       CodeFacts.gxl.xz
│       │   │
│       │   ├───reflexion
│       │   │   ├───compiler
│       │   │   │       Architecture.gxl
│       │   │   │       CodeFacts.gxl.xz
│       │   │   │       EmptyMapping.gxl
│       │   │   │       Mapping.gxl
│       │   │   │       Reflexion.cfg
│       │   │   │       ReflexionBoard.cfg
│       │   │   │
│       │   │   ├───mini
│       │   │   │       Architecture.gvl
│       │   │   │       Architecture.gxl
│       │   │   │       CodeFacts.cfg
│       │   │   │       CodeFacts.csv
│       │   │   │       CodeFacts.gvl
│       │   │   │       CodeFacts.gxl
│       │   │   │       CodeFacts.sld
│       │   │   │       EmptyMapping.gxl
│       │   │   │       Mapping.gxl
│       │   │   │       Reflexion.cfg
│       │   │   │       ReflexionLayout.sld
│       │   │   │
│       │   │   └───minilax
│       │   │           Architecture.gxl
│       │   │           CodeFacts.gxl.xz
│       │   │           EmptyMapping.gxl
│       │   │           Files.gxl.xz
│       │   │           Mapping.gxl
│       │   │           Reflexion.cfg
│       │   │
│       │   ├───SteamVR
│       │   │       actions.json
│       │   │       bindings_holographic_controller.json
│       │   │       bindings_knuckles.json
│       │   │       bindings_oculus_touch.json
│       │   │       bindings_vive_controller.json
│       │   │       binding_holographic_hmd.json
│       │   │       binding_rift.json
│       │   │       binding_vive.json
│       │   │       binding_vive_pro.json
│       │   │       binding_vive_tracker_camera.json
│       │   │
│       │   ├───Videos
│       │   │       AddEdge.mp4
│       │   │       EditNode.mp4
│       │   │       evolution.mp4
│       │   │       hideNode.mp4
│       │   │       navigation.mp4
│       │   │       New Render Texture.renderTexture
│       │   │       searchNode.mp4
│       │   │       toggleFocus.mp4
│       │   │       zoomIntoCodeCity.mp4
│       │   │
│       │   └───vswhere
│       │           LICENSE.txt
│       │           VERIFICATION.txt
│       │           vswhere.exe
│       │
│       └───UnitySubsystems
│           └───UnityOpenXR
│                   UnitySubsystemsManifest.json
│
├───Gameserver-Windows
│   │   SEE.exe
│   │   UnityCrashHandler64.exe
│   │   UnityPlayer.dll
│   │
│   ├───MonoBleedingEdge
│   │   ├───EmbedRuntime
│   │   │       mono-2.0-bdwgc.dll
│   │   │       MonoPosixHelper.dll
│   │   │
│   │   └───etc
│   │       └───mono
│   │           │   browscap.ini
│   │           │   config
│   │           │
│   │           ├───2.0
│   │           │   │   DefaultWsdlHelpGenerator.aspx
│   │           │   │   machine.config
│   │           │   │   settings.map
│   │           │   │   web.config
│   │           │   │
│   │           │   └───Browsers
│   │           │           Compat.browser
│   │           │
│   │           ├───4.0
│   │           │   │   DefaultWsdlHelpGenerator.aspx
│   │           │   │   machine.config
│   │           │   │   settings.map
│   │           │   │   web.config
│   │           │   │
│   │           │   └───Browsers
│   │           │           Compat.browser
│   │           │
│   │           ├───4.5
│   │           │   │   DefaultWsdlHelpGenerator.aspx
│   │           │   │   machine.config
│   │           │   │   settings.map
│   │           │   │   web.config
│   │           │   │
│   │           │   └───Browsers
│   │           │           Compat.browser
│   │           │
│   │           └───mconfig
│   │                   config.xml
│   │
│   ├───SEE_BurstDebugInformation_DoNotShip
│   │   └───Data
│   │       └───Plugins
│   │           └───x86_64
│   │                   lib_burst_generated.txt
│   │
│   └───SEE_Data
│       │   app.info
│       │   boot.config
│       │   globalgamemanagers
│       │   globalgamemanagers.assets
│       │   level0
│       │   level1
│       │   level2
│       │   resources.assets
│       │   resources.resource
│       │   RuntimeInitializeOnLoads.json
│       │   ScriptingAssemblies.json
│       │   sharedassets0.assets
│       │   sharedassets0.resource
│       │   sharedassets1.assets
│       │   sharedassets2.assets
│       │
│       ├───Managed
│       │       AmplifyShaderEditor.Samples.BuiltIn.dll
│       │       Antlr4.Runtime.Standard.dll
│       │       Assembly-CSharp-firstpass.dll
│       │       Autodesk.Fbx.dll
│       │       CTNAudio.dll
│       │       CurvedUI.dll
│       │       DemiLib.dll
│       │       DissonanceVoip.dll
│       │       DOTween.dll
│       │       DOTweenModules.dll
│       │       DOTweenPro.dll
│       │       DynamicPanels.Runtime.dll
│       │       EnoxSoftware.DlibFaceLandmarkDetector.dll
│       │       EnoxSoftware.OpenCVForUnity.dll
│       │       FaceMaskExample.dll
│       │       FbxBuildTestAssets.dll
│       │       FinalIK.dll
│       │       FinalIKBaker.dll
│       │       FinalIKShared.dll
│       │       FinalIKSharedDemoAssets.dll
│       │       FinalIK_UMA2.dll
│       │       FuzzySharp.dll
│       │       HighlightPlus.dll
│       │       HighlightPlusDemo.dll
│       │       HSVPicker.dll
│       │       HtmlAgilityPack.dll
│       │       InControl.dll
│       │       InControl.Examples.dll
│       │       Joveler.Compression.XZ.dll
│       │       Joveler.DynLoader.dll
│       │       MessagePack.dll
│       │       ModernUIPack.dll
│       │       Mono.Security.dll
│       │       mscorlib.dll
│       │       NaughtyAttributes.Core.dll
│       │       netstandard.dll
│       │       Newtonsoft.Json.dll
│       │       OEBIF.dll
│       │       OpenAI.dll
│       │       Parser.dll
│       │       RTG.dll
│       │       RTGDemoScenes.dll
│       │       RTGTutorials.dll
│       │       RTVoice.dll
│       │       SALSA-LipSync.dll
│       │       SALSAExamples.dll
│       │       SALSALipSyncOneClickRuntimes.dll
│       │       SALSAUMAAddons.dll
│       │       SEE.dll
│       │       SimpleFileBrowser.Runtime.dll
│       │       Sirenix.OdinInspector.Attributes.dll
│       │       Sirenix.Serialization.Config.dll
│       │       Sirenix.Serialization.dll
│       │       Sirenix.Utilities.dll
│       │       StreamRpc.dll
│       │       System.Buffers.dll
│       │       System.ComponentModel.Composition.dll
│       │       System.Configuration.dll
│       │       System.Core.dll
│       │       System.Data.DataSetExtensions.dll
│       │       System.Data.dll
│       │       System.dll
│       │       System.Drawing.dll
│       │       System.EnterpriseServices.dll
│       │       System.IO.Compression.dll
│       │       System.IO.Compression.FileSystem.dll
│       │       System.IO.Pipelines.dll
│       │       System.Memory.dll
│       │       System.Net.Http.dll
│       │       System.Numerics.dll
│       │       System.Runtime.CompilerServices.Unsafe.dll
│       │       System.Runtime.dll
│       │       System.Runtime.Serialization.dll
│       │       System.Security.dll
│       │       System.ServiceModel.Internals.dll
│       │       System.Threading.Tasks.Extensions.dll
│       │       System.Transactions.dll
│       │       System.Xml.dll
│       │       System.Xml.Linq.dll
│       │       TinySpline.dll
│       │       Tubular.dll
│       │       UMA_Content.dll
│       │       UMA_Core.dll
│       │       UMA_Examples.dll
│       │       UniTask.Addressables.dll
│       │       UniTask.dll
│       │       UniTask.DOTween.dll
│       │       UniTask.Linq.dll
│       │       UniTask.TextMeshPro.dll
│       │       Unity.AI.Navigation.dll
│       │       Unity.Burst.dll
│       │       Unity.Burst.Unsafe.dll
│       │       Unity.Collections.dll
│       │       Unity.Collections.LowLevel.ILSupport.dll
│       │       Unity.Formats.Fbx.Runtime.dll
│       │       Unity.InputSystem.dll
│       │       Unity.InputSystem.ForUI.dll
│       │       Unity.Mathematics.dll
│       │       Unity.Netcode.Components.dll
│       │       Unity.Netcode.Runtime.dll
│       │       Unity.Networking.Transport.dll
│       │       Unity.Postprocessing.Runtime.dll
│       │       Unity.RenderPipelines.Core.Runtime.dll
│       │       Unity.RenderPipelines.Core.ShaderLibrary.dll
│       │       Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary.dll
│       │       Unity.TextMeshPro.dll
│       │       Unity.Timeline.dll
│       │       Unity.XR.CoreUtils.dll
│       │       Unity.XR.Interaction.Toolkit.dll
│       │       Unity.XR.Management.dll
│       │       Unity.XR.OpenXR.dll
│       │       Unity.XR.OpenXR.Features.ConformanceAutomation.dll
│       │       Unity.XR.OpenXR.Features.MetaQuestSupport.dll
│       │       Unity.XR.OpenXR.Features.MockRuntime.dll
│       │       Unity.XR.OpenXR.Features.OculusQuestSupport.dll
│       │       Unity.XR.OpenXR.Features.RuntimeDebugger.dll
│       │       UnityEngine.AccessibilityModule.dll
│       │       UnityEngine.AIModule.dll
│       │       UnityEngine.AndroidJNIModule.dll
│       │       UnityEngine.AnimationModule.dll
│       │       UnityEngine.ARModule.dll
│       │       UnityEngine.AssetBundleModule.dll
│       │       UnityEngine.AudioModule.dll
│       │       UnityEngine.ClothModule.dll
│       │       UnityEngine.ClusterInputModule.dll
│       │       UnityEngine.ClusterRendererModule.dll
│       │       UnityEngine.ContentLoadModule.dll
│       │       UnityEngine.CoreModule.dll
│       │       UnityEngine.CrashReportingModule.dll
│       │       UnityEngine.DirectorModule.dll
│       │       UnityEngine.dll
│       │       UnityEngine.DSPGraphModule.dll
│       │       UnityEngine.GameCenterModule.dll
│       │       UnityEngine.GIModule.dll
│       │       UnityEngine.GridModule.dll
│       │       UnityEngine.HotReloadModule.dll
│       │       UnityEngine.ImageConversionModule.dll
│       │       UnityEngine.IMGUIModule.dll
│       │       UnityEngine.InputLegacyModule.dll
│       │       UnityEngine.InputModule.dll
│       │       UnityEngine.JSONSerializeModule.dll
│       │       UnityEngine.LocalizationModule.dll
│       │       UnityEngine.NVIDIAModule.dll
│       │       UnityEngine.ParticleSystemModule.dll
│       │       UnityEngine.PerformanceReportingModule.dll
│       │       UnityEngine.Physics2DModule.dll
│       │       UnityEngine.PhysicsModule.dll
│       │       UnityEngine.ProfilerModule.dll
│       │       UnityEngine.PropertiesModule.dll
│       │       UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule.dll
│       │       UnityEngine.ScreenCaptureModule.dll
│       │       UnityEngine.SharedInternalsModule.dll
│       │       UnityEngine.SpatialTracking.dll
│       │       UnityEngine.SpriteMaskModule.dll
│       │       UnityEngine.SpriteShapeModule.dll
│       │       UnityEngine.StreamingModule.dll
│       │       UnityEngine.SubstanceModule.dll
│       │       UnityEngine.SubsystemsModule.dll
│       │       UnityEngine.TerrainModule.dll
│       │       UnityEngine.TerrainPhysicsModule.dll
│       │       UnityEngine.TextCoreFontEngineModule.dll
│       │       UnityEngine.TextCoreTextEngineModule.dll
│       │       UnityEngine.TextRenderingModule.dll
│       │       UnityEngine.TilemapModule.dll
│       │       UnityEngine.TLSModule.dll
│       │       UnityEngine.UI.dll
│       │       UnityEngine.UIElementsModule.dll
│       │       UnityEngine.UIModule.dll
│       │       UnityEngine.UmbraModule.dll
│       │       UnityEngine.UnityAnalyticsCommonModule.dll
│       │       UnityEngine.UnityAnalyticsModule.dll
│       │       UnityEngine.UnityConnectModule.dll
│       │       UnityEngine.UnityCurlModule.dll
│       │       UnityEngine.UnityTestProtocolModule.dll
│       │       UnityEngine.UnityWebRequestAssetBundleModule.dll
│       │       UnityEngine.UnityWebRequestAudioModule.dll
│       │       UnityEngine.UnityWebRequestModule.dll
│       │       UnityEngine.UnityWebRequestTextureModule.dll
│       │       UnityEngine.UnityWebRequestWWWModule.dll
│       │       UnityEngine.VehiclesModule.dll
│       │       UnityEngine.VFXModule.dll
│       │       UnityEngine.VideoModule.dll
│       │       UnityEngine.VirtualTexturingModule.dll
│       │       UnityEngine.VRModule.dll
│       │       UnityEngine.WindModule.dll
│       │       UnityEngine.XR.LegacyInputHelpers.dll
│       │       UnityEngine.XRModule.dll
│       │       Unity_NFGO.dll
│       │       Utilities.Async.Addressables.dll
│       │       Utilities.Async.dll
│       │       Utilities.Audio.dll
│       │       Utilities.Encoding.Wav.dll
│       │       Utilities.Extensions.Addressables.dll
│       │       Utilities.Extensions.dll
│       │       Utilities.Rest.dll
│       │       ViveSR.dll
│       │       VivoxUnity.dll
│       │
│       ├───Plugins
│       │   └───x86_64
│       │           AudioPluginDissonance.dll
│       │           dlibfacelandmarkdetector.dll
│       │           InControlNative.dll
│       │           libgcc_s_seh-1.dll
│       │           libHTC_License.dll
│       │           liblzma.dll
│       │           libstdc++-6.dll
│       │           libtinysplinecsharp.dll
│       │           lib_burst_generated.dll
│       │           nanomsg.dll
│       │           opencvforunity.dll
│       │           opus.dll
│       │           SRanipal.dll
│       │           SRWorks_Log.dll
│       │           ViveSR_Client.dll
│       │           VivoxNative.dll
│       │           vivoxsdk.dll
│       │           XInputInterface64.dll
│       │
│       ├───Resources
│       │       unity default resources
│       │       unity_builtin_extra
│       │
│       ├───StreamingAssets
│       │   │   ESE_Board.json
│       │   │   network.cfg
│       │   │   PersonalAssistantGrammar.grxml
│       │   │
│       │   ├───example
│       │   │       CodeFacts.csv
│       │   │       CodeFacts.gxl
│       │   │
│       │   ├───JLGExample
│       │   │   │   CodeFacts.cfg
│       │   │   │   CodeFacts.gxl.xz
│       │   │   │   CodeFacts.jlg
│       │   │   │   CountConsonants.java
│       │   │   │   CountToAThousand.java
│       │   │   │   CountVowels.java
│       │   │   │   ExecutedLoCLogger.java
│       │   │   │   Main.java
│       │   │   │   Makefile
│       │   │   │
│       │   │   └───Unterordner
│       │   │           UnterOrdnerTest.java
│       │   │
│       │   ├───mini
│       │   │   │   Architecture.csv
│       │   │   │   Architecture.gvl
│       │   │   │   Architecture.gxl
│       │   │   │   build.bat
│       │   │   │   CodeFacts.cfg
│       │   │   │   CodeFacts.csv
│       │   │   │   CodeFacts.gvl
│       │   │   │   CodeFacts.gxl
│       │   │   │   CodeFacts.sld
│       │   │   │   Mapping.gxl
│       │   │   │   mini.cfg
│       │   │   │   reduce.py
│       │   │   │
│       │   │   └───src
│       │   │           C1.cs
│       │   │           C2.cs
│       │   │           MainClass.cs
│       │   │           mini.csproj
│       │   │           mini.sln
│       │   │
│       │   ├───mini-evolution
│       │   │       CodeFacts-1.gxl
│       │   │       CodeFacts-2.gxl
│       │   │       CodeFacts-3.gxl
│       │   │       CodeFacts-4.gxl
│       │   │       CodeFacts-5.gxl
│       │   │       mini-evolution.cfg
│       │   │
│       │   ├───Multiplayer
│       │   │       multiplayer.sln
│       │   │
│       │   ├───net
│       │   │       CodeFacts.csv
│       │   │       CodeFacts.gxl.xz
│       │   │
│       │   ├───reflexion
│       │   │   ├───compiler
│       │   │   │       Architecture.gxl
│       │   │   │       CodeFacts.gxl.xz
│       │   │   │       EmptyMapping.gxl
│       │   │   │       Mapping.gxl
│       │   │   │       Reflexion.cfg
│       │   │   │       ReflexionBoard.cfg
│       │   │   │
│       │   │   ├───mini
│       │   │   │       Architecture.gvl
│       │   │   │       Architecture.gxl
│       │   │   │       CodeFacts.cfg
│       │   │   │       CodeFacts.csv
│       │   │   │       CodeFacts.gvl
│       │   │   │       CodeFacts.gxl
│       │   │   │       CodeFacts.sld
│       │   │   │       EmptyMapping.gxl
│       │   │   │       Mapping.gxl
│       │   │   │       Reflexion.cfg
│       │   │   │       ReflexionLayout.sld
│       │   │   │
│       │   │   └───minilax
│       │   │           Architecture.gxl
│       │   │           CodeFacts.gxl.xz
│       │   │           EmptyMapping.gxl
│       │   │           Files.gxl.xz
│       │   │           Mapping.gxl
│       │   │           Reflexion.cfg
│       │   │
│       │   ├───SteamVR
│       │   │       actions.json
│       │   │       bindings_holographic_controller.json
│       │   │       bindings_knuckles.json
│       │   │       bindings_oculus_touch.json
│       │   │       bindings_vive_controller.json
│       │   │       binding_holographic_hmd.json
│       │   │       binding_rift.json
│       │   │       binding_vive.json
│       │   │       binding_vive_pro.json
│       │   │       binding_vive_tracker_camera.json
│       │   │
│       │   ├───Videos
│       │   │       AddEdge.mp4
│       │   │       EditNode.mp4
│       │   │       evolution.mp4
│       │   │       hideNode.mp4
│       │   │       navigation.mp4
│       │   │       New Render Texture.renderTexture
│       │   │       searchNode.mp4
│       │   │       toggleFocus.mp4
│       │   │       zoomIntoCodeCity.mp4
│       │   │
│       │   └───vswhere
│       │           LICENSE.txt
│       │           VERIFICATION.txt
│       │           vswhere.exe
│       │
│       └───UnitySubsystems
│           └───UnityOpenXR
│                   UnitySubsystemsManifest.json
│
└───SEE-Client
    │   SEE.exe
    │   UnityCrashHandler64.exe
    │   UnityPlayer.dll
    │
    ├───MonoBleedingEdge
    │   ├───EmbedRuntime
    │   │       mono-2.0-bdwgc.dll
    │   │       MonoPosixHelper.dll
    │   │
    │   └───etc
    │       └───mono
    │           │   browscap.ini
    │           │   config
    │           │
    │           ├───2.0
    │           │   │   DefaultWsdlHelpGenerator.aspx
    │           │   │   machine.config
    │           │   │   settings.map
    │           │   │   web.config
    │           │   │
    │           │   └───Browsers
    │           │           Compat.browser
    │           │
    │           ├───4.0
    │           │   │   DefaultWsdlHelpGenerator.aspx
    │           │   │   machine.config
    │           │   │   settings.map
    │           │   │   web.config
    │           │   │
    │           │   └───Browsers
    │           │           Compat.browser
    │           │
    │           ├───4.5
    │           │   │   DefaultWsdlHelpGenerator.aspx
    │           │   │   machine.config
    │           │   │   settings.map
    │           │   │   web.config
    │           │   │
    │           │   └───Browsers
    │           │           Compat.browser
    │           │
    │           └───mconfig
    │                   config.xml
    │
    ├───SEE_BurstDebugInformation_DoNotShip
    │   └───Data
    │       └───Plugins
    │           └───x86_64
    │                   lib_burst_generated.txt
    │
    └───SEE_Data
        │   app.info
        │   boot.config
        │   globalgamemanagers
        │   globalgamemanagers.assets
        │   globalgamemanagers.assets.resS
        │   level0
        │   level1
        │   level1.resS
        │   level2
        │   level2.resS
        │   resources.assets
        │   resources.assets.resS
        │   resources.resource
        │   RuntimeInitializeOnLoads.json
        │   ScriptingAssemblies.json
        │   sharedassets0.assets
        │   sharedassets0.assets.resS
        │   sharedassets0.resource
        │   sharedassets1.assets
        │   sharedassets1.assets.resS
        │   sharedassets2.assets
        │
        ├───Managed
        │       AmplifyShaderEditor.Samples.BuiltIn.dll
        │       Antlr4.Runtime.Standard.dll
        │       Assembly-CSharp-firstpass.dll
        │       Autodesk.Fbx.dll
        │       CTNAudio.dll
        │       CurvedUI.dll
        │       DemiLib.dll
        │       DissonanceVoip.dll
        │       DOTween.dll
        │       DOTweenModules.dll
        │       DOTweenPro.dll
        │       DynamicPanels.Runtime.dll
        │       EnoxSoftware.DlibFaceLandmarkDetector.dll
        │       EnoxSoftware.OpenCVForUnity.dll
        │       FaceMaskExample.dll
        │       FbxBuildTestAssets.dll
        │       FinalIK.dll
        │       FinalIKBaker.dll
        │       FinalIKShared.dll
        │       FinalIKSharedDemoAssets.dll
        │       FinalIK_UMA2.dll
        │       FuzzySharp.dll
        │       HighlightPlus.dll
        │       HighlightPlusDemo.dll
        │       HSVPicker.dll
        │       HtmlAgilityPack.dll
        │       InControl.dll
        │       InControl.Examples.dll
        │       Joveler.Compression.XZ.dll
        │       Joveler.DynLoader.dll
        │       MessagePack.dll
        │       ModernUIPack.dll
        │       Mono.Security.dll
        │       mscorlib.dll
        │       NaughtyAttributes.Core.dll
        │       netstandard.dll
        │       Newtonsoft.Json.dll
        │       OEBIF.dll
        │       OpenAI.dll
        │       Parser.dll
        │       RTG.dll
        │       RTGDemoScenes.dll
        │       RTGTutorials.dll
        │       RTVoice.dll
        │       SALSA-LipSync.dll
        │       SALSAExamples.dll
        │       SALSALipSyncOneClickRuntimes.dll
        │       SALSAUMAAddons.dll
        │       SEE.dll
        │       SimpleFileBrowser.Runtime.dll
        │       Sirenix.OdinInspector.Attributes.dll
        │       Sirenix.Serialization.Config.dll
        │       Sirenix.Serialization.dll
        │       Sirenix.Utilities.dll
        │       StreamRpc.dll
        │       System.Buffers.dll
        │       System.ComponentModel.Composition.dll
        │       System.Configuration.dll
        │       System.Core.dll
        │       System.Data.DataSetExtensions.dll
        │       System.Data.dll
        │       System.dll
        │       System.Drawing.dll
        │       System.EnterpriseServices.dll
        │       System.IO.Compression.dll
        │       System.IO.Compression.FileSystem.dll
        │       System.IO.Pipelines.dll
        │       System.Memory.dll
        │       System.Net.Http.dll
        │       System.Numerics.dll
        │       System.Runtime.CompilerServices.Unsafe.dll
        │       System.Runtime.dll
        │       System.Runtime.Serialization.dll
        │       System.Security.dll
        │       System.ServiceModel.Internals.dll
        │       System.Threading.Tasks.Extensions.dll
        │       System.Transactions.dll
        │       System.Xml.dll
        │       System.Xml.Linq.dll
        │       TinySpline.dll
        │       Tubular.dll
        │       UMA_Content.dll
        │       UMA_Core.dll
        │       UMA_Examples.dll
        │       UniTask.Addressables.dll
        │       UniTask.dll
        │       UniTask.DOTween.dll
        │       UniTask.Linq.dll
        │       UniTask.TextMeshPro.dll
        │       Unity.AI.Navigation.dll
        │       Unity.Burst.dll
        │       Unity.Burst.Unsafe.dll
        │       Unity.Collections.dll
        │       Unity.Collections.LowLevel.ILSupport.dll
        │       Unity.Formats.Fbx.Runtime.dll
        │       Unity.InputSystem.dll
        │       Unity.InputSystem.ForUI.dll
        │       Unity.Mathematics.dll
        │       Unity.Netcode.Components.dll
        │       Unity.Netcode.Runtime.dll
        │       Unity.Networking.Transport.dll
        │       Unity.Postprocessing.Runtime.dll
        │       Unity.RenderPipelines.Core.Runtime.dll
        │       Unity.RenderPipelines.Core.ShaderLibrary.dll
        │       Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary.dll
        │       Unity.TextMeshPro.dll
        │       Unity.Timeline.dll
        │       Unity.XR.CoreUtils.dll
        │       Unity.XR.Interaction.Toolkit.dll
        │       Unity.XR.Management.dll
        │       Unity.XR.OpenXR.dll
        │       Unity.XR.OpenXR.Features.ConformanceAutomation.dll
        │       Unity.XR.OpenXR.Features.MetaQuestSupport.dll
        │       Unity.XR.OpenXR.Features.MockRuntime.dll
        │       Unity.XR.OpenXR.Features.OculusQuestSupport.dll
        │       Unity.XR.OpenXR.Features.RuntimeDebugger.dll
        │       UnityEngine.AccessibilityModule.dll
        │       UnityEngine.AIModule.dll
        │       UnityEngine.AndroidJNIModule.dll
        │       UnityEngine.AnimationModule.dll
        │       UnityEngine.ARModule.dll
        │       UnityEngine.AssetBundleModule.dll
        │       UnityEngine.AudioModule.dll
        │       UnityEngine.ClothModule.dll
        │       UnityEngine.ClusterInputModule.dll
        │       UnityEngine.ClusterRendererModule.dll
        │       UnityEngine.ContentLoadModule.dll
        │       UnityEngine.CoreModule.dll
        │       UnityEngine.CrashReportingModule.dll
        │       UnityEngine.DirectorModule.dll
        │       UnityEngine.dll
        │       UnityEngine.DSPGraphModule.dll
        │       UnityEngine.GameCenterModule.dll
        │       UnityEngine.GIModule.dll
        │       UnityEngine.GridModule.dll
        │       UnityEngine.HotReloadModule.dll
        │       UnityEngine.ImageConversionModule.dll
        │       UnityEngine.IMGUIModule.dll
        │       UnityEngine.InputLegacyModule.dll
        │       UnityEngine.InputModule.dll
        │       UnityEngine.JSONSerializeModule.dll
        │       UnityEngine.LocalizationModule.dll
        │       UnityEngine.NVIDIAModule.dll
        │       UnityEngine.ParticleSystemModule.dll
        │       UnityEngine.PerformanceReportingModule.dll
        │       UnityEngine.Physics2DModule.dll
        │       UnityEngine.PhysicsModule.dll
        │       UnityEngine.ProfilerModule.dll
        │       UnityEngine.PropertiesModule.dll
        │       UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule.dll
        │       UnityEngine.ScreenCaptureModule.dll
        │       UnityEngine.SharedInternalsModule.dll
        │       UnityEngine.SpatialTracking.dll
        │       UnityEngine.SpriteMaskModule.dll
        │       UnityEngine.SpriteShapeModule.dll
        │       UnityEngine.StreamingModule.dll
        │       UnityEngine.SubstanceModule.dll
        │       UnityEngine.SubsystemsModule.dll
        │       UnityEngine.TerrainModule.dll
        │       UnityEngine.TerrainPhysicsModule.dll
        │       UnityEngine.TextCoreFontEngineModule.dll
        │       UnityEngine.TextCoreTextEngineModule.dll
        │       UnityEngine.TextRenderingModule.dll
        │       UnityEngine.TilemapModule.dll
        │       UnityEngine.TLSModule.dll
        │       UnityEngine.UI.dll
        │       UnityEngine.UIElementsModule.dll
        │       UnityEngine.UIModule.dll
        │       UnityEngine.UmbraModule.dll
        │       UnityEngine.UnityAnalyticsCommonModule.dll
        │       UnityEngine.UnityAnalyticsModule.dll
        │       UnityEngine.UnityConnectModule.dll
        │       UnityEngine.UnityCurlModule.dll
        │       UnityEngine.UnityTestProtocolModule.dll
        │       UnityEngine.UnityWebRequestAssetBundleModule.dll
        │       UnityEngine.UnityWebRequestAudioModule.dll
        │       UnityEngine.UnityWebRequestModule.dll
        │       UnityEngine.UnityWebRequestTextureModule.dll
        │       UnityEngine.UnityWebRequestWWWModule.dll
        │       UnityEngine.VehiclesModule.dll
        │       UnityEngine.VFXModule.dll
        │       UnityEngine.VideoModule.dll
        │       UnityEngine.VirtualTexturingModule.dll
        │       UnityEngine.VRModule.dll
        │       UnityEngine.WindModule.dll
        │       UnityEngine.XR.LegacyInputHelpers.dll
        │       UnityEngine.XRModule.dll
        │       Unity_NFGO.dll
        │       Utilities.Async.Addressables.dll
        │       Utilities.Async.dll
        │       Utilities.Audio.dll
        │       Utilities.Encoding.Wav.dll
        │       Utilities.Extensions.Addressables.dll
        │       Utilities.Extensions.dll
        │       Utilities.Rest.dll
        │       ViveSR.dll
        │       VivoxUnity.dll
        │
        ├───Plugins
        │   └───x86_64
        │           AudioPluginDissonance.dll
        │           dlibfacelandmarkdetector.dll
        │           InControlNative.dll
        │           libgcc_s_seh-1.dll
        │           libHTC_License.dll
        │           liblzma.dll
        │           libstdc++-6.dll
        │           libtinysplinecsharp.dll
        │           lib_burst_generated.dll
        │           nanomsg.dll
        │           opencvforunity.dll
        │           opus.dll
        │           SRanipal.dll
        │           SRWorks_Log.dll
        │           ViveSR_Client.dll
        │           VivoxNative.dll
        │           vivoxsdk.dll
        │           XInputInterface64.dll
        │
        ├───Resources
        │       unity default resources
        │       unity_builtin_extra
        │
        ├───StreamingAssets
        │   │   ESE_Board.json
        │   │   network.cfg
        │   │   PersonalAssistantGrammar.grxml
        │   │
        │   ├───example
        │   │       CodeFacts.csv
        │   │       CodeFacts.gxl
        │   │
        │   ├───JLGExample
        │   │   │   CodeFacts.cfg
        │   │   │   CodeFacts.gxl.xz
        │   │   │   CodeFacts.jlg
        │   │   │   CountConsonants.java
        │   │   │   CountToAThousand.java
        │   │   │   CountVowels.java
        │   │   │   ExecutedLoCLogger.java
        │   │   │   Main.java
        │   │   │   Makefile
        │   │   │
        │   │   └───Unterordner
        │   │           UnterOrdnerTest.java
        │   │
        │   ├───mini
        │   │   │   Architecture.csv
        │   │   │   Architecture.gvl
        │   │   │   Architecture.gxl
        │   │   │   build.bat
        │   │   │   CodeFacts.cfg
        │   │   │   CodeFacts.csv
        │   │   │   CodeFacts.gvl
        │   │   │   CodeFacts.gxl
        │   │   │   CodeFacts.sld
        │   │   │   Mapping.gxl
        │   │   │   mini.cfg
        │   │   │   reduce.py
        │   │   │
        │   │   └───src
        │   │           C1.cs
        │   │           C2.cs
        │   │           MainClass.cs
        │   │           mini.csproj
        │   │           mini.sln
        │   │
        │   ├───mini-evolution
        │   │       CodeFacts-1.gxl
        │   │       CodeFacts-2.gxl
        │   │       CodeFacts-3.gxl
        │   │       CodeFacts-4.gxl
        │   │       CodeFacts-5.gxl
        │   │       mini-evolution.cfg
        │   │
        │   ├───Multiplayer
        │   │   │   multiplayer.sln
        │   │   │
        │   │   └───src
        │   │       └───src
        │   │               mini.csproj
        │   │               mini.sln
        │   │
        │   ├───net
        │   │       CodeFacts.csv
        │   │       CodeFacts.gxl.xz
        │   │
        │   ├───reflexion
        │   │   ├───compiler
        │   │   │       Architecture.gxl
        │   │   │       CodeFacts.gxl.xz
        │   │   │       EmptyMapping.gxl
        │   │   │       Mapping.gxl
        │   │   │       Reflexion.cfg
        │   │   │       ReflexionBoard.cfg
        │   │   │
        │   │   ├───mini
        │   │   │       Architecture.gvl
        │   │   │       Architecture.gxl
        │   │   │       CodeFacts.cfg
        │   │   │       CodeFacts.csv
        │   │   │       CodeFacts.gvl
        │   │   │       CodeFacts.gxl
        │   │   │       CodeFacts.sld
        │   │   │       EmptyMapping.gxl
        │   │   │       Mapping.gxl
        │   │   │       Reflexion.cfg
        │   │   │       ReflexionLayout.sld
        │   │   │
        │   │   └───minilax
        │   │           Architecture.gxl
        │   │           CodeFacts.gxl.xz
        │   │           EmptyMapping.gxl
        │   │           Files.gxl.xz
        │   │           Mapping.gxl
        │   │           Reflexion.cfg
        │   │
        │   ├───SteamVR
        │   │       actions.json
        │   │       bindings_holographic_controller.json
        │   │       bindings_knuckles.json
        │   │       bindings_oculus_touch.json
        │   │       bindings_vive_controller.json
        │   │       binding_holographic_hmd.json
        │   │       binding_rift.json
        │   │       binding_vive.json
        │   │       binding_vive_pro.json
        │   │       binding_vive_tracker_camera.json
        │   │
        │   ├───Videos
        │   │       AddEdge.mp4
        │   │       EditNode.mp4
        │   │       evolution.mp4
        │   │       hideNode.mp4
        │   │       navigation.mp4
        │   │       New Render Texture.renderTexture
        │   │       searchNode.mp4
        │   │       toggleFocus.mp4
        │   │       zoomIntoCodeCity.mp4
        │   │
        │   └───vswhere
        │           LICENSE.txt
        │           VERIFICATION.txt
        │           vswhere.exe
        │
        └───UnitySubsystems
            └───UnityOpenXR
                    UnitySubsystemsManifest.json
```
