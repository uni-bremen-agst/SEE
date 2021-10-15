#if UNITY_EDITOR && (UNITY_IOS || UNITY_TVOS)
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;


// The native plugin on Apple platforms requires CoreHaptics.framework.
// This build post-processor is responsible for adding it to the UnityFramework target.
public static class AppleBuildPostProcessor
{
	[PostProcessBuildAttribute( 1 )]
	public static void OnPostProcessBuild( BuildTarget target, string path )
	{
		if (target == BuildTarget.iOS ||
		    target == BuildTarget.tvOS)
		{
			var projectPath = PBXProject.GetPBXProjectPath( path );
			var project = new PBXProject();
			project.ReadFromString( File.ReadAllText( projectPath ) );
			var targetGuid = project.GetUnityFrameworkTargetGuid();

			project.AddFrameworkToProject( targetGuid, "CoreHaptics.framework", false );

			File.WriteAllText( projectPath, project.WriteToString() );
		}
	}
}
#endif
