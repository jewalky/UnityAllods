using System.Collections.Generic;
using UnityEngine;
using System;

public partial class Utils
{
    // the following custom code is so that we can finally start using virtual fullscreen mode
    // without suffering from the cursor being out of screen bounds.
    // all code should refer to GetMousePosition instead of using Input.mousePosition directly.

    private static float _MouseX = 0;
    private static float _MouseY = 0;

    public static void SetMousePosition(float deltaX, float deltaY)
    {
        _MouseX = Mathf.Clamp(_MouseX + deltaX, 0, Screen.width);
        _MouseY = Mathf.Clamp(_MouseY + deltaY, 0, Screen.height);
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

    public static T CreateObjectWithScript<T>() where T : Component
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
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            renderer.enabled = enabled;
    }

    public static void PutQuadInMesh(Vector3[] qv, Vector2[] quv, Color[] qc, ref int pp, ref int ppt, ref int ppc, float x, float y, float w, float h, Rect texRect, Color color)
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

    public static void MakeQuad(out GameObject go, int w, int h, Color color)
    {
        go = CreateObject();
        go.name = "Utils$Quad";
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = new Material(MainCamera.MainShader);
        mr.material.color = color;
        Mesh mesh = new Mesh();
        Vector3[] qv = new Vector3[4];
        int[] qt = new int[4];
        for (int i = 0; i < 4; i++)
            qt[i] = i;
        int pp = 0; int ppt = 0; int ppc = 0;
        PutQuadInMesh(qv, null, null, ref pp, ref ppt, ref ppc, 0, 0, w, h, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        mesh.vertices = qv;
        mesh.SetIndices(qt, MeshTopology.Quads, 0);
        mf.mesh = mesh;
    }

    public static void MakeTexturedQuad(out GameObject go, Texture2D tex)
    {
        MakeTexturedQuad(out go, tex, new Rect(0, 0, 1, 1));
    }

    public static void MakeTexturedQuad(out GameObject go, Texture2D tex, Rect texRect)
    {
        go = CreateObject();
        go.name = "Utils$TexturedQuad";
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        float acW = Mathf.Round(tex.width * texRect.width);
        float acH = Mathf.Round(tex.height * texRect.height);
        mr.material = new Material(MainCamera.MainShader);
        mr.material.mainTexture = tex;
        Mesh mesh = new Mesh();
        Vector3[] qv = new Vector3[4];
        Vector2[] quv = new Vector2[4];
        int[] qt = new int[4];
        for (int i = 0; i < 4; i++)
            qt[i] = i;
        int pp = 0; int ppt = 0; int ppc = 0;
        PutQuadInMesh(qv, quv, null, ref pp, ref ppt, ref ppc, 0, 0, acW, acH, texRect, new Color(1, 1, 1, 1));
        mesh.vertices = qv;
        mesh.uv = quv;
        mesh.SetIndices(qt, MeshTopology.Quads, 0);
        mf.mesh = mesh;
    }

    public class PerformanceTimer
    {
        private float _Time = 0;
        public float Time
        {
            get
            {
                if (_Time > 0.0001)
                    return _Time;
                return 0;
            }
        }

        private float _TimeStart = 0;

        public void Clear()
        {
            _Time = 0;
            _TimeStart = 0;
        }

        public void Clock()
        {
            _TimeStart = UnityEngine.Time.realtimeSinceStartup;
        }

        public void Unclock()
        {
            _Time += UnityEngine.Time.realtimeSinceStartup - _TimeStart;
        }
    }

    public class MeshBuilder
    {
        private List<Vector3> Vertices = new List<Vector3>();
        private List<Vector2> UV1 = new List<Vector2>();
        private List<Vector2> UV2 = new List<Vector2>();
        private List<Vector2> UV3 = new List<Vector2>();
        private List<Vector2> UV4 = new List<Vector2>();
        private List<Color> Colors = new List<Color>();
        private List<int> Meshes = new List<int>();
        private int VertexNum;
        private int TopMesh;

        public MeshBuilder()
        {
            Reset();
        }

        private void AllocVertex()
        {
            Vertices.Add(new Vector3());
            UV1.Add(new Vector2());
            UV2.Add(new Vector2());
            UV3.Add(new Vector2());
            UV4.Add(new Vector2());
            Colors.Add(new Color(1, 1, 1, 1));
            if (Meshes.Count > 0)
                Meshes.Add(Meshes[Meshes.Count - 1]);
            else Meshes.Add(TopMesh - 1);
        }

        public void Reset()
        {
            Vertices.Clear();
            UV1.Clear();
            UV2.Clear();
            UV3.Clear();
            UV4.Clear();
            Colors.Clear();
            Meshes.Clear();
            VertexNum = 0;
            TopMesh = 1;
            AllocVertex();
        }

        public void NextVertex()
        {
            AllocVertex();
            VertexNum++;
        }

        public Vector3 CurrentPosition
        {
            get { return Vertices[VertexNum]; }
            set { Vertices[VertexNum] = value; }
        }

        public Vector2 CurrentUV1
        {
            get { return UV1[VertexNum]; }
            set { UV1[VertexNum] = value; }
        }

        public Vector2 CurrentUV2
        {
            get { return UV2[VertexNum]; }
            set { UV2[VertexNum] = value; }
        }

        public Vector2 CurrentUV3
        {
            get { return UV3[VertexNum]; }
            set { UV3[VertexNum] = value; }
        }

        public Vector2 CurrentUV4
        {
            get { return UV4[VertexNum]; }
            set { UV4[VertexNum] = value; }
        }

        public Color CurrentColor
        {
            get { return Colors[VertexNum]; }
            set { Colors[VertexNum] = value; }
        }

        public int CurrentMesh
        {
            get { return Meshes[VertexNum]; }
            set { Meshes[VertexNum] = value; if (value + 1 > TopMesh) TopMesh = value + 1; }
        }

        public Mesh ToMesh(params MeshTopology[] submeshes)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = Vertices.ToArray();
            mesh.uv = UV1.ToArray();
            mesh.uv2 = UV2.ToArray();
            mesh.uv3 = UV3.ToArray();
            mesh.uv4 = UV4.ToArray();
            mesh.colors = Colors.ToArray();
            if (submeshes.Length != TopMesh)
                throw new Exception(string.Format("Builder submesh count ({0}) doesn't correspond to submesh topology count ({1})", TopMesh, submeshes.Length));
            mesh.subMeshCount = TopMesh;
            for (int i = 0; i < TopMesh; i++)
            {
                List<int> indices = new List<int>();
                for (int j = 0; j < Meshes.Count; j++)
                {
                    if (Meshes[j] == i)
                        indices.Add(j);
                }

                mesh.SetIndices(indices.ToArray(), submeshes[i], i);
            }

            return mesh;
        }

        public void AddQuad(int submesh, float x, float y, float w, float h)
        {
            AddQuad(submesh, x, y, w, h, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        }

        public void AddQuad(int submesh, float x, float y, float w, float h, Rect texRect)
        {
            AddQuad(submesh, x, y, w, h, texRect, new Color(1, 1, 1, 1));
        }

        public void AddQuad(int submesh, float x, float y, float w, float h, Color color)
        {
            AddQuad(submesh, x, y, w, h, new Rect(0, 0, 1, 1), color);
        }

        public void AddQuad(int submesh, float x, float y, float w, float h, Rect texRect, Color color)
        {
            CurrentPosition = new Vector3(x, y);
            CurrentColor = color;
            CurrentUV1 = new Vector2(texRect.xMin, texRect.yMin);
            CurrentMesh = submesh;
            NextVertex();

            CurrentPosition = new Vector3(x + w, y);
            CurrentColor = color;
            CurrentUV1 = new Vector2(texRect.xMax, texRect.yMin);
            CurrentMesh = submesh;
            NextVertex();

            CurrentPosition = new Vector3(x + w, y + h);
            CurrentColor = color;
            CurrentUV1 = new Vector2(texRect.xMax, texRect.yMax);
            CurrentMesh = submesh;
            NextVertex();

            CurrentPosition = new Vector3(x, y + h);
            CurrentColor = color;
            CurrentUV1 = new Vector2(texRect.xMin, texRect.yMax);
            CurrentMesh = submesh;
            NextVertex();
        }
    }

    public static Rect DivRect(Rect inRec, Vector2 vec)
    {
        return new Rect(inRec.x / vec.x, inRec.y / vec.y, inRec.width / vec.x, inRec.height / vec.y);
    }
}
