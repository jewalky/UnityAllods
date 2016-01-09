using System.IO;
using UnityEditor;
using UnityEngine;

public class ScriptBatch
{
    [MenuItem("MyTools/Linux dedicated server build")]
    public static void BuildServer()
    {
        BuildPlayer("ServerBuild/AllodsServer.x86", BuildTarget.StandaloneLinux, 
            BuildOptions.EnableHeadlessMode, @"DLLs", @"ServerBuild\AllodsServer_Data\Managed");
    }

    [MenuItem("MyTools/Linux client build")]
    public static void BuildLinuxClient()
    {
        BuildPlayer("LinuxClientBuild/Allods.x86", BuildTarget.StandaloneLinux, BuildOptions.None, @"DLLs", @"LinuxClientBuild\Allods_Data\Managed");
    }

    [MenuItem("MyTools/OSX client build")]
    public static void BuildMacClient()
    {
        BuildPlayer("MacBuild/Allods.app", BuildTarget.StandaloneOSXIntel, BuildOptions.None, @"DLLs", @"MacBuild\Allods.app\Contents\Resources\Data\Managed");
    }


    [MenuItem("MyTools/Windows build")]
    public static void BuildWindowsClient()
    {
        BuildPlayer("ClientBuild/Allods.exe", BuildTarget.StandaloneWindows, BuildOptions.None, @"DLLs", @"ClientBuild\Allods_Data\Managed");
    }


    private static void BuildPlayer(string buildPath, BuildTarget buildTarget, BuildOptions buildOptions, string dllSourceDir, string dllTargetDir)
    {
        var levels = new[] {"Assets/Allods.unity"};
        BuildPipeline.BuildPlayer(levels, buildPath, buildTarget, buildOptions);
        foreach (var file in Directory.GetFiles(dllSourceDir))
            File.Copy(file, Path.Combine(dllTargetDir, Path.GetFileName(file)), true);
    }
}