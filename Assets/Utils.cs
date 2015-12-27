using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;
#endif

public partial class Utils
{
    public static Vector3 Vec3InvertY(Vector3 _in)
    {
       return new Vector3(_in.x / 100, ((float)Screen.height - _in.y) / 100, _in.z / 100);
       //return new Vector3(_in.x, ((float)Screen.height - _in.y), _in.z);
       //return _in;
    }

    public static void DestroyObjectAndMesh(GameObject o)
    {
        var children = new List<GameObject>();
        foreach (Transform child in o.transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyObjectAndMesh(o));

        MeshFilter mf = o.GetComponent<MeshFilter>();
        if (mf != null)
        {
            GameObject.DestroyImmediate(mf.mesh, true);
            GameObject.DestroyImmediate(mf.sharedMesh, true);
        }
        MeshRenderer mr = o.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            GameObject.DestroyImmediate(mr.material, true);
            GameObject.DestroyImmediate(mr.sharedMaterial, true);
        }
        GameObject.DestroyImmediate(o, true);
    }

    public static T CreateObjectWithScript<T>() where T : UnityEngine.Component
    {
        GameObject go = CreateObject();
        go.name = typeof(T).Name+"$Object";
        return go.AddComponent<T>();
    }

    public static GameObject CreateObject()
    {
        GameObject go = new GameObject();
        go.transform.parent = SceneRoot.Instance.transform;
        return go;
    }

    public static GameObject CreatePrimitive(PrimitiveType pt)
    {
        GameObject go = GameObject.CreatePrimitive(pt);
        go.transform.parent = SceneRoot.Instance.transform;
        return go;
    }

    public static void SetRendererEnabledWithChildren(GameObject parent, bool enabled)
    {
        Renderer prenderer = parent.GetComponent<Renderer>();
        if (prenderer != null) prenderer.enabled = enabled;
        Renderer[] renderers;
        renderers = parent.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            renderer.enabled = enabled;
    }
}

#if UNITY_EDITOR
public class ScriptBatch
{
    [MenuItem("MyTools/Linux dedicated server build")]
    public static void BuildGame()
    {
        // Get filename.
        string path = "ServerBuild";
        string[] levels = new string[] { "Assets/Allods.unity" };
        // Build player.
        BuildPipeline.BuildPlayer(levels, path + "/AllodsServer.x64", BuildTarget.StandaloneLinux64, BuildOptions.EnableHeadlessMode);
        // copy libs
        const string sourceDir = @"DLLs";
        const string targetDir = @"ServerBuild\AllodsServer_Data\Managed";
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
    }

    [MenuItem("MyTools/Windows build")]
    public static void BuildGameWindows()
    {
        // Get filename.
        string path = "ClientBuild";
        string[] levels = new string[] { "Assets/Allods.unity" };
        // Build player.
        BuildPipeline.BuildPlayer(levels, path + "/Allods.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
        // Then put some DLLs in it
        const string sourceDir = @"DLLs";
        const string targetDir = @"ClientBuild\Allods_Data\Managed";
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
    }
}
#endif