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
    // the following custom code is so that we can finally start using virtual fullscreen mode
    // without suffering from the cursor being out of screen bounds.
    // all code should refer to GetMousePosition instead of using Input.mousePosition directly.

    private static float MouseX = 0;
    private static float MouseY = 0;

    public static void SetMousePosition(float deltaX, float deltaY)
    {
        MouseX = Mathf.Clamp(MouseX + deltaX, 0, Screen.width);
        MouseY = Mathf.Clamp(MouseY + deltaY, 0, Screen.height);
    }

    public static Vector3 GetMousePosition()
    {
        //Vector3 mPos = new Vector3(MouseX, MouseY, 0);
        //return mPos;
        return new Vector3(Input.mousePosition.x, (Screen.height - Input.mousePosition.y), Input.mousePosition.z);
    }

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

    public static void PutQuadInMesh(Vector3[] qv, Vector2[] quv, Color[] qc, ref int pp, ref int ppt, ref int ppc, int x, int y, int w, int h, Rect texRect, Color color)
    {
        qv[pp++] = new Vector3(x, y, 0);
        qv[pp++] = new Vector3(x + w, y, 0);
        qv[pp++] = new Vector3(x + w, y + h, 0);
        qv[pp++] = new Vector3(x, y + h, 0);
        if (quv != null)
        {
            quv[ppt++] = new Vector2(texRect.xMin, texRect.yMin);
            quv[ppt++] = new Vector2(texRect.xMax, texRect.yMin);
            quv[ppt++] = new Vector2(texRect.xMax, texRect.yMax);
            quv[ppt++] = new Vector2(texRect.xMin, texRect.yMax);
        }
        if (qc != null)
        {
            qc[ppc++] = color;
            qc[ppc++] = color;
            qc[ppc++] = color;
            qc[ppc++] = color;
        }
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
        BuildPipeline.BuildPlayer(levels, path + "/AllodsServer.x64", BuildTarget.StandaloneLinux64, BuildOptions.EnableHeadlessMode|BuildOptions.Il2CPP);
        // copy libs
        const string sourceDir = @"DLLs";
        const string targetDir = @"ServerBuild\AllodsServer_Data\Managed";
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
    }

    [MenuItem("MyTools/Linux client build")]
    public static void BuildLinuxClient()
    {
        // Get filename.
        string path = "LinuxClientBuild";
        string[] levels = new string[] { "Assets/Allods.unity" };
        // Build player.
        BuildPipeline.BuildPlayer(levels, path + "/Allods.x86", BuildTarget.StandaloneLinux, BuildOptions.Il2CPP);
        // copy libs
        const string sourceDir = @"DLLs";
        const string targetDir = @"LinuxClientBuild\Allods_Data\Managed";
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
        BuildPipeline.BuildPlayer(levels, path + "/Allods.exe", BuildTarget.StandaloneWindows, BuildOptions.Il2CPP);
        // Then put some DLLs in it
        const string sourceDir = @"DLLs";
        const string targetDir = @"ClientBuild\Allods_Data\Managed";
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
    }
}
#endif